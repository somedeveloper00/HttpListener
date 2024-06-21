using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace HttpListener
{
    public delegate void HandleRoute(HttpListenerRequest req, HttpListenerResponse res);

    /// <summary>
    /// Represents an HTTP server component that listens for incoming requests and handles them using registered route handlers.
    /// </summary>
    public sealed class HttpListenerComponent : MonoBehaviour
    {
        [Tooltip("Port for the server to listen to")]
        [SerializeField] private int port = 8080;

        [Tooltip("host for the server to listen to. Usually 127.0.0.1")]
        [SerializeField] private string host = "127.0.0.1";

        [Tooltip("Determines on which Unity phase to handle requests")]
        [SerializeField] private Phase phase;

        [Header("Debug")]
        [Tooltip("Whether to print public IP address when server starts")]
        [SerializeField] private bool printPublicIpOnStart = true;

        [Tooltip("Whether to print the route and HTTP method when a request comes")]
        [SerializeField] private bool printReceivedRoutes = true;

        [Tooltip("Whether to print information about response")]
        [SerializeField] private bool printResponseInfo = false;

        [Tooltip("Whether to print when server starts listening")]
        [SerializeField] private bool printStartedListening = true;

        [Tooltip("Whether to print when server stops listening")]
        [SerializeField] private bool printClosedListening = true;

        /// <summary>
        /// Actions to be executed in Unity's main thread
        /// </summary>
        private readonly Queue<HttpAction> _httpAction = new(8);

        /// <summary>
        /// background server thread
        /// </summary>
        [NonSerialized] private Thread _serverThread;

        /// <summary>
        /// list of all added routes and their handlers
        /// </summary>
        [NonSerialized] private List<RouteHandler> _routeHandlers = new(8);

        private void Start() => StartServer();
        private void OnDestroy() => StopServer();
        private void Update() => HandlePhase(Phase.Update);
        private void LateUpdate() => HandlePhase(Phase.LateUpdate);
        private void FixedUpdate() => HandlePhase(Phase.FixedUpdate);

        public bool IsRunning() => _serverThread != null && _serverThread.IsAlive;

        public void StopServer()
        {
            if (!IsRunning())
            {
                return;
            }
            _serverThread.Abort();
        }

        public void StartServer()
        {
            if (IsRunning())
            {
                Debug.LogWarning($"server is already running.");
                return;
            }

            _serverThread = new Thread(Listen)
            {
                Name = "http server",
            };
            _serverThread.Start();
        }

        public void BindRoute(RouteHandler handler)
        {
            if (IsRunning())
            {
                Debug.LogWarning($"server is busy, you cannot add new handlers.");
                return;
            }
            _routeHandlers.Add(handler);
        }

        public void UnbindRoute(RouteHandler handler)
        {
            if (IsRunning())
            {
                Debug.LogWarning($"server is busy, you cannot remove handlers.");
                return;
            }

            _routeHandlers.Remove(handler);
        }

        private void Listen()
        {
            using System.Net.HttpListener listener = new(); // initializing it outside so we can close it later on
            try
            {
                var url = new UriBuilder(Uri.UriSchemeHttp, host, port);
                foreach (var routeHandler in _routeHandlers)
                {
                    url.Path = routeHandler.path;
                    listener.Prefixes.Add(url.ToString());
                }

                listener.Start();
                if (printStartedListening)
                {
                    Debug.Log($"server listening on: {string.Join(", ", listener.Prefixes)}");
                }

                if (printPublicIpOnStart)
                {
                    Task.Run(() =>
                    {
                        try
                        {
                            var publicIp = new WebClient().DownloadString("https://api.ipify.org");
                            Debug.Log($"public ip: {publicIp}");
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError("couldnt find public ip (read next log for stacktrace)");
                            Debug.LogException(ex);
                        }
                    });
                }

                while (true)
                {
                    var context = listener.GetContext();
                    if (printReceivedRoutes)
                    {
                        Debug.LogFormat($"[{DateTime.UtcNow:H:mm:ss:f}] request: {context.Request.HttpMethod} {context.Request.Url}\nHeaders\n{context.Request.Headers}");
                    }

                    RouteHandler? routeHandler = null;
                    for (var i = 0; i < _routeHandlers.Count; i++)
                    {
                        string requestRoute = context.Request.Url.LocalPath;

                        if (_routeHandlers[i].method.Method == context.Request.HttpMethod &&
                            (_routeHandlers[i].listenToSubRoutes
                                ? requestRoute.StartsWith(_routeHandlers[i].path, StringComparison.CurrentCultureIgnoreCase)
                                : requestRoute.Equals(_routeHandlers[i].path, StringComparison.CurrentCultureIgnoreCase)))
                        {
                            routeHandler = _routeHandlers[i];
                            break;
                        }
                    }

                    if (!routeHandler.HasValue)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                        context.Response.Close();
                        continue;
                    }

                    // enque for later handling
                    lock (_httpAction)
                    {
                        _httpAction.Enqueue(new(routeHandler.Value, context));
                    }
                }
            }
            finally
            {
                if (listener?.IsListening == true)
                    listener?.Close();
                if (printClosedListening)
                {
                    if (listener == null || !listener.IsListening)
                    {
                        Debug.Log($"Server at port {port} closed successfully.");
                    }
                    else
                    {
                        Debug.LogError($"Server at port {port} failed to close. aborting it now");
                        listener.Abort();
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HandlePhase(Phase phase)
        {
            if (this.phase == phase)
            {
                lock (_httpAction)
                {
                    while (_httpAction.TryDequeue(out var action))
                    {
                        try
                        {
                            action.routeHandler.handle?.Invoke(action.context.Request, action.context.Response);
                            if (printResponseInfo)
                            {
                                Debug.Log($"[{DateTime.UtcNow:H:mm:ss:f}] response: {action.context.Request.HttpMethod} {action.context.Request.RawUrl} status code {action.context.Response.StatusCode}\nHeaders\n{action.context.Response.Headers}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Debug.LogException(ex);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Unity execution phase
        /// </summary>
        public enum Phase
        {
            Update, LateUpdate, FixedUpdate
        }

        public readonly struct HttpAction
        {
            public readonly RouteHandler routeHandler;
            public readonly HttpListenerContext context;

            public HttpAction(RouteHandler routeHandler, HttpListenerContext context)
            {
                this.routeHandler = routeHandler;
                this.context = context;
            }
        }

        public readonly struct RouteHandler
        {
            public readonly string path; // format: '/path'
            public readonly bool listenToSubRoutes;
            public readonly HttpMethod method;
            public readonly HandleRoute handle;

            public RouteHandler(HandleRoute handle, HttpMethod method, string path, bool listenToSubRoutes)
            {
                this.handle = handle;
                this.method = method;
                this.path = path;
                this.listenToSubRoutes = listenToSubRoutes;
            }
        }
    }
}