using System.Diagnostics;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

namespace System.Web
{
    /// <summary>
    /// Extension methods for <see cref="System.Web.HttpResponse" />
    /// </summary>
    [DebuggerStepThrough]
    internal static class HttpResponseExtensions
    {
        /// <summary>
        /// Appends file client modified meta.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <param name="fileName">The name.</param>
        /// <param name="modifiedDate">The modified date.</param>
        public static void AppendModifiedMeta(this HttpResponse response, string fileName, DateTime modifiedDate)
        {
            response.Cache.SetCacheability(HttpCacheability.Public);
            response.Cache.SetLastModified(modifiedDate);
            response.Cache.SetETag(GenerateETag(fileName, modifiedDate));
        }

        /// <summary>
        /// Compresses the response output, if accepted.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <param name="request">The request.</param>
        public static void CompressIfAccepted(this HttpResponse response, HttpRequest request)
        {
            if (!(request.UserAgent != null && request.UserAgent.Contains("MSIE 6")))
            {
                string accept = request.Headers["Accept-Encoding"];
                if (!string.IsNullOrEmpty(accept))
                {
                    if (accept.Contains("gzip"))
                    {
                        response.AppendHeader("Content-Encoding", "gzip");
                        response.Filter = new GZipStream(response.Filter, CompressionMode.Compress);
                        HttpContext.Current.Trace.Warn("GZip compression is on");
                    }
                    else if (accept.Contains("deflate"))
                    {
                        response.AppendHeader("Content-Encoding", "deflate");
                        response.Filter = new DeflateStream(response.Filter, CompressionMode.Compress);
                        HttpContext.Current.Trace.Warn("Deflate copression is on");
                    }
                }
            }
        }

        /// <summary>
        /// Disables the response cache.
        /// </summary>
        /// <param name="httpResponse">The HTTP response.</param>
        public static void DisableCache(this HttpResponse httpResponse)
        {
            httpResponse.Cache.SetCacheability(HttpCacheability.NoCache);
            httpResponse.Expires = -1;
            httpResponse.ClearHeaders();
            httpResponse.AppendHeader(@"Cache-Control", @"no-cache"); // HTTP 1.1            
            httpResponse.AppendHeader(@"Cache-Control", @"private"); // HTTP 1.1            
            httpResponse.AppendHeader(@"Cache-Control", @"no-store"); // HTTP 1.1            
            httpResponse.AppendHeader(@"Cache-Control", @"must-revalidate"); // HTTP 1.1            
            httpResponse.AppendHeader(@"Cache-Control", @"max-stale=0"); // HTTP 1.1            
            httpResponse.AppendHeader(@"Cache-Control", @"post-check=0"); // HTTP 1.1
            httpResponse.AppendHeader(@"Cache-Control", @"pre-check=0"); // HTTP 1.1
            httpResponse.AppendHeader(@"Pragma", @"no-cache"); // HTTP 1.1
            httpResponse.AppendHeader(@"Keep-Alive", @"timeout=3, max=993"); // HTTP 1.1
            httpResponse.AppendHeader(@"Expires", @"Mon, 26 Jul 1997 05:00:00 GMT"); // HTTP 1.1
        }

        /// <summary>
        /// Generates an ETag.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="modified">The modified.</param>
        /// <returns></returns>
        public static string GenerateETag(string name, DateTime modified)
        {
            string tag = name + modified.ToString();
            byte[] data = new byte[Encoding.UTF8.GetByteCount(tag)];
            Encoding.UTF8.GetBytes(tag, 0, tag.Length, data, 0);
            MD5 md5 = MD5CryptoServiceProvider.Create();
            return "\"" + BitConverter.ToString(md5.ComputeHash(data)).Replace("-", "") + "\"";
        }

        /// <summary>
        /// Returns response permanent redirect to specified URL.
        /// </summary>
        /// <param name="response">The response.</param>
        public static void RedirectPermanent(this HttpResponse response, string url)
        {
            response.Status = "301 Moved Permanently";
            response.StatusCode = 301;
            response.AppendHeader("Location", url);
            response.End();
        }

        /// <summary>
        /// Returns not modified response.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <param name="fileName">The name.</param>
        /// <param name="modifiedDate">The modified date.</param>
        public static void ReturnNotModified(this HttpResponse response, string fileName, DateTime modifiedDate)
        {
            //File hasn't changed, so return HTTP 304 without retrieving the data }
            response.StatusCode = 304;
            response.StatusDescription = "Not Modified";
            //Explicitly set the Content-Length header so the client doesn't wait for
            //  content but keeps the connection open for other requests 
            response.AppendHeader("Content-Length", "0");
            response.Cache.SetCacheability(HttpCacheability.Public);
            response.Cache.SetLastModified(modifiedDate);
            response.Cache.SetETag(GenerateETag(fileName, modifiedDate));
        }

        /// <summary>
        /// Transmits a static file with modified meta check.
        /// </summary>
        /// <param name="response">The response.</param>
        /// <param name="fileName">Name of the file.</param>
        public static void TransmitStaticFile(this HttpResponse response, string fileName)
        {
            if (!System.IO.File.Exists(fileName))
            {
                response.StatusCode = 404;
                response.StatusDescription = "Not found";
                return;
                //response.End();
            }
            DateTime modified = System.IO.File.GetLastWriteTimeUtc(fileName);
            bool isModified = System.Web.HttpContext.Current.Request.IsFileModified(fileName, modified);
            if (isModified)
            {
                response.AppendModifiedMeta(fileName, modified);
                response.TransmitFile(fileName);
            }
            else
            {
                response.ReturnNotModified(fileName, modified);
            }
        }
    }
}