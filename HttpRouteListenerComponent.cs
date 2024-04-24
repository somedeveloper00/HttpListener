using System;
using System.Net;
using System.Net.Http;
using UnityEngine;
using UnityEngine.Events;

namespace HttpListener
{
    [Serializable]
    public sealed class HttpProcessUnityEvent : UnityEvent<HttpListenerRequest, HttpListenerResponse> { }

    public enum HttpMethodEnum
    {
        GET, POST, OPTIONS, DELETE, HEAD, PUT
    }

    public static class HttpMethodEnumExtensions
    {
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

    [Serializable]
    public sealed class CorsOptions
    {
        public string[] allowedOrigins = new string[] { "*" };
        public HttpMethodEnum[] allowedMethods = new HttpMethodEnum[] { HttpMethodEnum.POST, HttpMethodEnum.GET, HttpMethodEnum.OPTIONS };
        public bool allowCredentials = true;
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

    public sealed class HttpRouteListenerComponent : MonoBehaviour
    {
        public HttpListenerComponent serverComponent;
        public string path = "/";
        public HttpMethodEnum method;
        public CorsOptions corsOptions;
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
            _routeHandler = new HttpListenerComponent.RouteHandler(OnMethod, method.ToHttpMethod(), path);
            serverComponent.BindRoute(_routeHandler);

            if (method != HttpMethodEnum.OPTIONS)
            {
                _corsRouteHandler = new HttpListenerComponent.RouteHandler(OnCors, HttpMethod.Options, path);
                serverComponent.BindRoute(_corsRouteHandler);
            }
        }

        private void OnMethod(HttpListenerRequest req, HttpListenerResponse res)
        {
            AddCorsHeaders(res);
            onProcess.Invoke(req, res);
        }

        private void OnCors(HttpListenerRequest req, HttpListenerResponse res)
        {
            AddCorsHeaders(res);
            corsOptions.onProcess?.Invoke(req, res);

            if (res.OutputStream.CanWrite)
            {
                res.OutputStream.Close();
            }
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