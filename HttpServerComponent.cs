using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace HttpListener
{
    [AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
    public sealed class HttpServerRoute : Attribute
    {
        public HttpMethod method;
        public string path;

        public HttpServerRoute(HttpMethod method, string path)
        {
            this.method = method;
            this.path = path;
        }
    }

    public delegate void HandleRoute(HttpListenerRequest req, HttpListenerResponse res);

    /// <summary>
    /// Represents an HTTP server component that listens for incoming requests and handles them using registered route handlers.
    /// </summary>
    public sealed class HttpServerComponent : MonoBehaviour
    {
        [SerializeField] private int port = 8080;
        [SerializeField] private string host = "localhost";
        [SerializeField] private SafeThreadingComponent safeThreading;
        [SerializeField] private bool printPublicIpOnStart;

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

        public bool IsRunning() => _serverThread != null && _serverThread.IsAlive;

        public void StopServer()
        {
            if (!IsRunning())
            {
                Debug.LogWarning($"server is not running.");
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
                IsBackground = true
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
            System.Net.HttpListener listener = null; // initializing it outside so we can close it later on
            try
            {
                listener = new System.Net.HttpListener();
                var url = new UriBuilder(Uri.UriSchemeHttp, host, port);
                foreach (var routeHandler in _routeHandlers)
                {
                    url.Path = routeHandler.path;
                    listener.Prefixes.Add(url.ToString());
                }

                listener.Start();
                Debug.Log($"server listening on: {string.Join(", ", listener.Prefixes)}");
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
                    Debug.LogFormat($"[{DateTime.UtcNow:H:mm:ss:f}] request: {context.Request.HttpMethod} {context.Request.Url}");

                    RouteHandler routeHandler = null;
                    for (var i = 0; i < _routeHandlers.Count; i++)
                    {
                        if (_routeHandlers[i].path == context.Request.Url.LocalPath &&
                            _routeHandlers[i].method.Method == context.Request.HttpMethod)
                        {
                            routeHandler = _routeHandlers[i];
                            break;
                        }
                    }

                    if (routeHandler == null)
                    {
                        context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                        context.Response.Close();
                        continue;
                    }

                    // execute in main thread
                    safeThreading.ExecuteInMainThread(() => routeHandler.handle(context.Request, context.Response));
                }
            }
            catch (ThreadAbortException)
            {
                listener?.Stop();
            }
            catch (Exception e)
            {
                listener?.Stop();
                Debug.LogException(e);
            }
            finally
            {
                if (listener == null || !listener.IsListening)
                    Debug.Log($"Server closed successfully.");
                else
                {
                    Debug.LogError($"Server failed to close. aborting it now");
                    listener.Abort();
                }
            }
        }

        public sealed class RouteHandler
        {
            public readonly string path; // format: '/path'
            public readonly HttpMethod method;
            public readonly HandleRoute handle;

            public RouteHandler(HandleRoute handle, HttpMethod method, string path)
            {
                this.handle = handle;
                this.method = method;
                this.path = path;
            }
        }
    }
}