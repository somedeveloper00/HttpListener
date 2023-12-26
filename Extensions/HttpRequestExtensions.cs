using System.IO;
using System.Net;
using System.Text;

public static class HttpRequestExtensions
{
    public static string ReadText(this HttpListenerRequest req)
    {
        using (var reader = new StreamReader(req.InputStream, Encoding.UTF8))
        {
            return reader.ReadToEnd();
        }
    }
}