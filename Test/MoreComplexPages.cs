using System;
using System.Net;
using UnityEngine;

namespace HttpListener.Test
{
    public sealed class MoreComplexPages : MonoBehaviour
    {
        public void HandleLogin(HttpListenerRequest req, HttpListenerResponse res)
        {
            var username = req.QueryString["name"];
            if (username == null || username != "saeed")
            {
                var buff = System.Text.Encoding.UTF8.GetBytes("invalid username").AsSpan();
                res.ContentLength64 = buff.Length;
                res.OutputStream.Write(buff);
                res.Close();
                return;
            }
            var password = req.QueryString["pass"];
            if (password is not "123")
            {
                var buff = System.Text.Encoding.UTF8.GetBytes("invalid password").AsSpan();
                res.ContentLength64 = buff.Length;
                res.OutputStream.Write(buff);
                res.Close();
                return;
            }

            var userToken = Guid.NewGuid().ToString();
            var buffer = System.Text.Encoding.UTF8.GetBytes(userToken).AsSpan();
            res.OutputStream.Write(buffer);
            res.Close();
        }
    }
}