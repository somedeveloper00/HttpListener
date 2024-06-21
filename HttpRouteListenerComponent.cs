using System;
using System.Net;
using System.Net.Http;
using UnityEngine;
using UnityEngine.Events;

namespace HttpListener
{
    [Serializable]
    public sealed class HttpProcessUnityEvent : UnityEvent<HttpListenerRequest, HttpListenerResponse> { }

    /// <summary>
    /// Represents possible HTTP methods in the <see cref="HttpServer"/> system
    /// </summary>
    public enum HttpMethodEnum
    {
        GET, POST, OPTIONS, DELETE, HEAD, PUT
    }

    public static class HttpMethodEnumExtensions
    {
        /// <summary>
        /// Converts <see cref="HttpMethodEnum"/> to <see cref="HttpMethod"/>
        /// </summary>
        public static HttpMethod ToHttpMethod(this HttpMethodEnum methodEnum)
        {
            return methodEnum switch
            {
                HttpMethodEnum.GET => HttpMethod.Get,
                HttpMethodEnum.POST => HttpMethod.Post,
                HttpMethodEnum.OPTIONS => HttpMethod.Options,
                HttpMethodEnum.DELETE => HttpMethod.Delete,
                HttpMethodEnum.HEAD => HttpMethod.Head,
                HttpMethodEnum.PUT => HttpMethod.Put,
                _ => throw new ArgumentOutOfRangeException(nameof(methodEnum), methodEnum, null)
            };
        }
    }

    /// <summary>
    /// Corresponds to a URI scheme
    /// </summary>
    public enum UriSchemeEnum
    {
        File,
        Ftp,
        Gopher,
        Http,
        Https,
        Mailto,
        NetPipe,
        NetTcp,
        News,
        Nntp
    }

    public static class UriSceheEnumExtensions
    {
        /// <summary>
        /// Convert the enum to the corresponding scheme string in the class <see cref="Uri"/> 
        /// </summary>
        public static string ToSchemeString(this UriSchemeEnum uriSceheEnum)
        {
            return uriSceheEnum switch
            {
                UriSchemeEnum.File => Uri.UriSchemeFile,
                UriSchemeEnum.Ftp => Uri.UriSchemeFtp,
                UriSchemeEnum.Gopher => Uri.UriSchemeGopher,
                UriSchemeEnum.Http => Uri.UriSchemeHttp,
                UriSchemeEnum.Https => Uri.UriSchemeHttps,
                UriSchemeEnum.Mailto => Uri.UriSchemeMailto,
                UriSchemeEnum.NetPipe => Uri.UriSchemeNetPipe,
                UriSchemeEnum.News => Uri.UriSchemeNews,
                UriSchemeEnum.Nntp => Uri.UriSchemeNntp,
                _ => throw new NotSupportedException()
            };
        }
    }

    /// <summary>
    /// Options for handling CORS. CORS is a standard feature in the web, you can read about it online as this documentation 
    /// asumes you know of it.
    /// </summary>
    [Serializable]
    public sealed class CorsOptions
    {
        [Tooltip("Origings to allow")]
        public string[] allowedOrigins = new string[] { "*" };

        [Tooltip("HTTP methods to allow")]
        public HttpMethodEnum[] allowedMethods = new HttpMethodEnum[] { HttpMethodEnum.POST, HttpMethodEnum.GET, HttpMethodEnum.OPTIONS };

        [Tooltip("Whether to allow credentials")]
        public bool allowCredentials = true;

        [Tooltip("Headers to allow")]
        public string[] allowedHeaders = new string[]
        {
            "Content-Type",
            "Content-Encoding",
            "X-Authentication",
            "X-Authorization",
            "X-PlayFabSDK",
            "X-ReportErrorAsSuccess",
            "X-SecretKey",
            "X-EntityToken",
            "Authorization",
            "x-ms-app",
            "x-ms-client-request-id",
            "x-ms-user-id",
            "traceparent",
            "tracestate",
            "Request-Id"
         };
        public HttpProcessUnityEvent onProcess;
    }

    /// <summary>
    /// Listens to a route from a <see cref="HttpServerComponent"/> for a specific HTTP method
    /// </summary>
    public sealed class HttpRouteListenerComponent : MonoBehaviour
    {
        public HttpListenerComponent serverComponent;

        [Tooltip("Route to listen to")]
        public string path = "/";

        [Tooltip("URI scheme.")]
        public UriSchemeEnum uriScheme = UriSchemeEnum.Http;

        [Tooltip("Whether to listen to any route starting with path")]
        public bool listenToSubRoutes = false;

        [Tooltip("Method to listen to")]
        public HttpMethodEnum method;

        [Tooltip("Options for cross origins requests")]
        public CorsOptions corsOptions;

        [Tooltip("Gets called on every new request that matches the configs of this component")]
        public HttpProcessUnityEvent onProcess;

        private HttpListenerComponent.RouteHandler _routeHandler;
        private HttpListenerComponent.RouteHandler _corsRouteHandler;

        private void OnValidate()
        {
            if (path == null || path.Length == 0)
            {
                path = "/";
            }

            if (path[0] != '/')
            {
                path = '/' + path;
            }

            if (path.Length > 1 && path[^1] != '/')
            {
                path += '/';
            }

            if (path.Contains(" "))
            {
                path = path.Replace(" ", "-");
            }
        }

        private void Awake()
        {
            _routeHandler = new HttpListenerComponent.RouteHandler(Handle, method.ToHttpMethod(), path, listenToSubRoutes);
            serverComponent.BindRoute(_routeHandler);

            if (method != HttpMethodEnum.OPTIONS)
            {
                _corsRouteHandler = new HttpListenerComponent.RouteHandler(OnCors, HttpMethod.Options, path, listenToSubRoutes);
                serverComponent.BindRoute(_corsRouteHandler);
            }
        }

        private void Handle(HttpListenerRequest req, HttpListenerResponse res)
        {
            AddCorsHeaders(res);
            onProcess.Invoke(req, res);
        }

        private void OnCors(HttpListenerRequest req, HttpListenerResponse res)
        {
            AddCorsHeaders(res);
            corsOptions.onProcess?.Invoke(req, res);
        }

        private void AddCorsHeaders(HttpListenerResponse res)
        {
            res.AddHeader("Access-Control-Allow-Origin", string.Join(',', corsOptions.allowedOrigins));
            res.AddHeader("Access-Control-Allow-Methods", string.Join(',', corsOptions.allowedMethods));
            res.AddHeader("Access-Control-Allow-Credentials", corsOptions.allowCredentials.ToString());
            res.AddHeader("Access-Control-Allow-Headers", string.Join(',', corsOptions.allowedHeaders));
        }

        private void OnDestroy()
        {
            if (serverComponent && !serverComponent.IsRunning())
            {
                serverComponent.UnbindRoute(_routeHandler);
                serverComponent.UnbindRoute(_corsRouteHandler);
            }
        }
    }
}