using System;
using System.Net;
using UnityEngine;

namespace HttpListener.Test
{
    public sealed class HelloWorldPage : MonoBehaviour
    {
        public void Handle(HttpListenerRequest request, HttpListenerResponse response)
        {
            var str = $@"
{{
    ""message"": ""Hello World"",
    ""time"": ""{DateTime.Now:yyyy-MM-dd HH:mm:ss}""
}}
    ";
            var buffer = System.Text.Encoding.UTF8.GetBytes(str).AsSpan();
            response.ContentLength64 = buffer.Length;
            response.OutputStream.Write(buffer);
            response.OutputStream.Close();
        }
    }
}