using System;
using System.IO;
using System.Net;
using System.Net.Mime;
using System.Text;
using UnityEngine;

namespace HttpListener.Extensions
{
    public static class HttpResponseExtensions
    {
        /// <summary>
        /// Simply closes the respnose with <see cref="HttpStatusCode.OK"/> status code.
        /// </summary>
        public static void Ok(this HttpListenerResponse res)
        {
            res.StatusCode = (int)HttpStatusCode.OK;
            res.Close();
        }

        /// <summary>
        /// Simply closes the respnose with <see cref="HttpStatusCode.BadRequest"/> status code and <paramref name="message"/> in its body.
        /// </summary>
        public static void BadRequest(this HttpListenerResponse res, string message = null, string mediaType = MediaTypeNames.Text.Plain)
        {
            WriteText(res, message, mediaType, HttpStatusCode.BadRequest, true);
        }

        /// <summary>
        /// Simply closes the respnose with <see cref="HttpStatusCode.InternalServerError"/> status code and <paramref name="message"/> in its body.
        /// </summary>
        public static void InternalError(this HttpListenerResponse res, string message = null, string mediaType = MediaTypeNames.Text.Plain)
        {
            WriteText(res, message, mediaType, HttpStatusCode.InternalServerError, true);
        }

        /// <summary>
        /// Simply closes the respnose with <see cref="HttpStatusCode.NotFound"/> status code and <paramref name="message"/> in its body.
        /// </summary>
        public static void NotFound(this HttpListenerResponse res, string message = null, string mediaType = MediaTypeNames.Text.Plain)
        {
            WriteText(res, message, mediaType, HttpStatusCode.NotFound, true);
        }

        /// <summary>
        /// Writes json to response using default encoding.
        /// </summary>
        public static void WriteHtml(this HttpListenerResponse res, string html, HttpStatusCode statusCode = HttpStatusCode.OK, bool close = true)
        {
            WriteText(res, html, MediaTypeNames.Text.Html, statusCode, close);
        }

        /// <summary>
        /// Writes json to response using default encoding.
        /// </summary>
        public static string WriteJson<T>(this HttpListenerResponse res, T data, HttpStatusCode statusCode = HttpStatusCode.OK, bool close = true)
        {
            var json = JsonUtility.ToJson(data);
            WriteText(res, json, MediaTypeNames.Application.Json, statusCode, close);
            return json;
        }

        /// <summary>
        /// Writes json to response using <paramref name="encoding"/>.
        /// </summary>
        public static string WriteJson<T>(this HttpListenerResponse res, T data, Encoding encoding, HttpStatusCode statusCode = HttpStatusCode.OK, bool close = true)
        {
            var json = JsonUtility.ToJson(data);
            WriteText(res, json, encoding, MediaTypeNames.Application.Json, statusCode, close);
            return json;
        }

        /// <summary>
        /// Writes text to response using the default encoding.
        /// /// </summary>
        public static void WriteText(this HttpListenerResponse res, string text, string mediaType = MediaTypeNames.Text.Plain, HttpStatusCode statusCode = HttpStatusCode.OK, bool close = true)
        {
            WriteText(res, text, Encoding.Default, mediaType, statusCode, close);
        }

        /// <summary>
        /// Writes text to response using the <paramref name="encoding"/>.
        /// </summary>
        public static void WriteText(this HttpListenerResponse res, string text, Encoding encoding, string mediaType = MediaTypeNames.Text.Plain, HttpStatusCode statusCode = HttpStatusCode.OK, bool close = true)
        {
            var bytes = string.IsNullOrEmpty(text) ? Span<byte>.Empty : encoding.GetBytes(text);
            res.Write(bytes, mediaType, statusCode, close);
        }

        /// <summary>
        /// Writes file to response with support for resuming.
        /// </summary>
        public static void WriteFile(this HttpListenerResponse res, HttpListenerRequest req, string filePath, int bufferSize = 1024 * 64)
        {
            try
            {
                using var file = new FileStream(filePath, FileMode.Open, FileAccess.ReadWrite);

                long start = 0, end = file.Length - 1;

                // find requested range
                string range = req.Headers["Range"];
                bool isARangeRequest = !string.IsNullOrEmpty(range);
                if (isARangeRequest)
                {
                    res.StatusCode = (int)HttpStatusCode.PartialContent;
                    res.AddHeader("Content-Range", $"bytes {start}-{end}/{file.Length}");
                    var subs = range["bytes=".Length..].Split('-');
                    if (subs.Length > 0 && string.IsNullOrEmpty(subs[0]))
                    {
                        start = Convert.ToInt64(subs[0]);
                    }
                    if (subs.Length > 1 && string.IsNullOrEmpty(subs[1]))
                    {
                        end = Convert.ToInt64(subs[1]);
                    }
                }
                else
                {
                    res.StatusCode = (int)HttpStatusCode.OK;
                }

                // prepare
                res.AddHeader("Accept-Ranges", "bytes");
                res.ContentLength64 = end - start + 1;
                res.ContentType = MediaTypeNames.Application.Octet;

                // send bytes
                file.Seek(start, SeekOrigin.Begin);
                var buffer = new byte[bufferSize];
                int bytesCount;
                while ((bytesCount = file.Read(buffer, 0, buffer.Length)) > 0)
                {
                    res.OutputStream.Write(buffer, 0, bytesCount);
                }
            }
            catch (Exception ex)
            {
                res.InternalError(ex.Message);
                throw ex;
            }
            finally
            {
                res.Close();
            }
        }

        private static void Write(this HttpListenerResponse res, ReadOnlySpan<byte> bytes, string contentType, HttpStatusCode statusCode, bool close)
        {
            try
            {
                res.StatusCode = (int)statusCode;
                res.ContentType = contentType;
                res.ContentLength64 = bytes.Length;
            }
            catch { }

            if (res.OutputStream.CanWrite)
            {
                if (bytes.Length > 0)
                {
                    res.OutputStream.Write(bytes);
                }

                if (close)
                {
                    res.OutputStream.Close();
                }
            }
        }
    }
}