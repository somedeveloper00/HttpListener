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

    public sealed class HttpRouteListenerComponent : MonoBehaviour
    {
        public HttpListenerComponent serverComponent;
        public string path = "/";
        public HttpMethodEnum method;
        public HttpProcessUnityEvent onProcess;
        private HttpListenerComponent.RouteHandler _routeHandler;

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
            _routeHandler = new(onProcess.Invoke, method.ToHttpMethod(), path);
            serverComponent.BindRoute(_routeHandler);
        }

        private void OnDestroy()
        {
            if (serverComponent && !serverComponent.IsRunning())
            {
                serverComponent.UnbindRoute(_routeHandler);
            }
        }
    }
}