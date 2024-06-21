using System;
using System.IO;
using System.Net;
using System.Text;
using UnityEngine;

namespace HttpListener.Extensions
{
    public static class HttpRequestExtensions
    {
        /// <summary>
        /// Read all bytes from the request.
        /// </summary>
        public static byte[] ReadAllBytes(this HttpListenerRequest req)
        {
            using var stream = new MemoryStream();
            req.InputStream.CopyTo(stream);
            return stream.ToArray();
        }

        /// <summary>
        /// Read all text from the request, using default encoding.
        /// </summary>
        public static string ReadText(this HttpListenerRequest req)
        {
            return ReadText(req, Encoding.Default);
        }

        /// <summary>
        /// Read all text from the request, using <paramref name="encoding"/>.
        /// </summary>
        public static string ReadText(this HttpListenerRequest req, Encoding encoding)
        {
            using var reader = new StreamReader(req.InputStream, encoding);
            if (reader.EndOfStream)
            {
                return default;
            }
            return reader.ReadToEnd();
        }

        /// <summary>
        /// Reads json from the request. If successful, returns <c>true</c> otherwise <c>false</c>.
        /// </summary>
        public static bool TryReadJson<T>(this HttpListenerRequest req, out T result, out string json)
        {
            using var reader = new StreamReader(req.InputStream, Encoding.UTF8);
            if (reader.EndOfStream)
            {
                result = default;
                json = default;
                return false;
            }

            json = reader.ReadToEnd();
            try
            {
                result = JsonUtility.FromJson<T>(json);
                return result != null;
            }
            catch
            {
                result = default;
                return false;
            }
        }

        /// <inheritdoc cref="TryReadJson{T}(HttpListenerRequest, out T, out string)"/>
        public static bool TryReadJson(this HttpListenerRequest req, Type type, out object result, out string json)
        {
            using var reader = new StreamReader(req.InputStream, Encoding.UTF8);
            if (reader.EndOfStream)
            {
                result = default;
                json = default;
                return false;
            }

            json = reader.ReadToEnd();
            try
            {
                result = JsonUtility.FromJson(json, type);
                return result != null;
            }
            catch
            {
                result = default;
                return false;
            }
        }
    }
}