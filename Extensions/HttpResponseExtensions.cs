using System.IO;
using System.Net;
using System.Net.Mime;
using System.Text;

public static class HttpResponseExtensions
{
    public static void WriteText(this HttpListenerResponse res, string text)
    {
        res.StatusCode = (int)HttpStatusCode.OK;
        res.ContentType = MediaTypeNames.Text.Plain;
        var bytes = Encoding.UTF8.GetBytes(text);
        res.ContentLength64 = bytes.Length;
        res.OutputStream.Write(bytes);
        res.Close();
    }

    public static void WriteJson(this HttpListenerResponse res, string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        res.StatusCode = (int)HttpStatusCode.OK;
        res.ContentLength64 = bytes.Length;
        res.ContentType = MediaTypeNames.Application.Json;
        res.OutputStream.Write(bytes);
        res.Close();
    }

    public static void Ok(this HttpListenerResponse res)
    {
        res.StatusCode = (int)HttpStatusCode.OK;
        res.Close();
    }

    public static void BadRequest(this HttpListenerResponse res, string message = null)
    {
        res.StatusCode = (int)HttpStatusCode.BadRequest;
        if (message != null)
        {
            using var writer = new StreamWriter(res.OutputStream);
            writer.Write(message);
        }
        res.Close();
    }

    public static void InternalError(this HttpListenerResponse res, string message)
    {
        res.StatusCode = (int)HttpStatusCode.InternalServerError;
        using (var writer = new StreamWriter(res.OutputStream, Encoding.UTF8))
        {
            writer.Write(message);
        }
        res.Close();
    }
}