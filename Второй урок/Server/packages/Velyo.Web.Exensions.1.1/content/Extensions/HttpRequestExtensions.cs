using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.Globalization;

namespace System.Web
{
    /// <summary>
    /// Extension methods for <see cref="System.Web.HttpRequest" />
    /// </summary>
    [DebuggerStepThrough]
    internal static class HttpRequestExtensions
    {
        /// <summary>
        /// Gets the root path.
        /// </summary>
        /// <param name="r">The r.</param>
        /// <returns></returns>
        public static string GetRootPath(this HttpRequest r)
        {
            string hostPort = r.Url.Authority;
            string scheme = r.Url.Scheme;
            string path = string.Concat(scheme, @"://", hostPort);
            return path;
        }

        /// <summary>
        /// Determines whether the specified request is AJAX.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        public static bool IsAjax(this HttpRequest request)
        {
            return (request.Headers.Get("x-microsoftajax") ?? "").Equals("delta=true", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Determines whether the specified file is file modified.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="modifiedDate">The modified date.</param>
        /// <returns></returns>
        public static bool IsFileModified(this HttpRequest request, string fileName, DateTime modifiedDate)
        {
            bool modified = true;
            DateTime modifiedSince;

            if (!string.IsNullOrEmpty(request.Headers["If-Modified-Since"])
                && DateTime.TryParse(request.Headers["If-Modified-Since"], out modifiedSince))
            {

                modified = (modifiedDate > modifiedSince);
            }
            else if (!string.IsNullOrEmpty(request.Headers["If-None-Match"]))
            {
                string etag = HttpResponseExtensions.GenerateETag(fileName, modifiedDate);
                modified = request.Headers["If-None-Match"] != etag;
            }

            return modified;
        }

        /// <summary>
        /// Determines whether the request is JSON.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <returns></returns>
        public static bool IsJson(this HttpRequest request)
        {
            return request.ContentType.StartsWith("application/json", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Processes if request is modified.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="fileName">The name.</param>
        /// <param name="modifiedDate">The modifie date.</param>
        /// <returns></returns>
        static public bool ProcessModified(this HttpRequest request, string fileName, DateTime modifiedDate)
        {
            return ProcessModified(request, HttpContext.Current.Response, fileName, modifiedDate);
        }

        /// <summary>
        /// Processes if request is modified.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="response">The response.</param>
        /// <param name="fileName">The name.</param>
        /// <param name="modifiedDate">The modifie date.</param>
        /// <returns></returns>
        static public bool ProcessModified(this HttpRequest request, HttpResponse response, string fileName, DateTime modifiedDate)
        {
            if (request.IsFileModified(fileName, modifiedDate))
            {
                response.AppendModifiedMeta(fileName, DateTime.Now);
                return true;
            }
            else
            {
                response.ReturnNotModified(fileName, DateTime.Now);
                return false;
            }
        }

        /// <summary>
        /// Removes items from query string.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="itemsToRemove">The items to remove.</param>
        /// <returns></returns>
        public static string RemoveItemsFromQueryString(this HttpRequest request, params string[] itemsToRemove)
        {
            NameValueCollection queryString = request.QueryString;
            List<string> items = new List<string>();
            foreach (string item in itemsToRemove)
            {
                items.Add(item.ToLower(CultureInfo.InvariantCulture));
            }
            string s = string.Empty;
            for (int i = 0; i < queryString.Count; i++)
            {
                if ((queryString.GetKey(i) != null) && !items.Contains(queryString.GetKey(i).ToLower(CultureInfo.InvariantCulture)))
                {
                    s += queryString.GetKey(i) + '=' + queryString[i] + '&';
                }
            }
            return s;
        }
    }
}
