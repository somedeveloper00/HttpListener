using System.IO;
using System.Net;
using System.Text;
using UnityEngine;

namespace HttpListener.Extensions
{
    public static class HttpRequestExtensions
    {
        public static string ReadText(this HttpListenerRequest req)
        {
            using var reader = new StreamReader(req.InputStream, Encoding.UTF8);
            if (reader.EndOfStream)
            {
                return default;
            }
            return reader.ReadToEnd();
        }

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
    }
}