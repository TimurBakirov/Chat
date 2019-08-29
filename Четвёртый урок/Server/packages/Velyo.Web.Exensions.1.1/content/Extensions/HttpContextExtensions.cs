using System.Diagnostics;
using System.Net.Mail;
using System.Text;

namespace System.Web
{
    /// <summary>
    /// 
    /// </summary>
    internal delegate void ProcessRequestCallback(HttpContext context);

    /// <summary>
    /// Extension methods for <see cref="System.Web.HttpContext"/>
    /// </summary>
    [DebuggerStepThrough]
    internal static class HttpContextExtensions
    {
        /// <summary>
        /// Sends a notification email.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <param name="subject">The subject.</param>
        /// <param name="message">The message.</param>
        public static void Notify(this HttpContext context, string from, string to, string subject, string message)
        {
            try
            {
                SmtpClient mailClient = new SmtpClient();
                using (MailMessage mailMessage = new MailMessage(from, to, subject, message))
                {
                    mailMessage.IsBodyHtml = true;
                    mailClient.Send(mailMessage);
                }
            }
            catch { }
        }

        /// <summary>
        /// Sends a notification email for an exception.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        /// <param name="ex">The ex.</param>
        public static void NotifyForException(this HttpContext context, string from, string to, Exception ex)
        {
            var username = (context.User != null && context.User.Identity != null)
                ? context.User.Identity.Name : "guest";
            StringBuilder buff = new StringBuilder("<html><head></head><body>");
            buff.AppendFormat("User: {0}<br/>", username)
                .AppendFormat("DateTime: {0}<br/><br/>", DateTime.Now)
                .Append("StackTrace:<br/>")
                .AppendFormat("<p style='color: Red;'>{0}</p>", ex.ToString())
                .Append("Request Params:<br/><table style='font-size:0.82em;'>");
            for (int i = 0; i < context.Request.Params.Count; i++)
            {
                buff.AppendFormat("<tr><td>{0}</td><td>{1}</td></tr>",
                    context.Request.Params.AllKeys[i], context.Request.Params[i]);
            }
            buff.Append("</table></body></html>");

            Notify(context, buff.ToString(), from, to, "Error");
        }

        /// <summary>
        /// Sends a notification email for last server exception.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="from">From.</param>
        /// <param name="to">To.</param>
        public static void NotifyForException(this HttpContext context, string from, string to)
        {
            NotifyForException(context, from, to, context.Server.GetLastError());
        }

        /// <summary>
        /// Checks the request modified meta and ensures is added to response.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="fileName">Name of the file.</param>
        /// <param name="modifiedDate">The modified date.</param>
        /// <param name="callback">The callback.</param>
        public static void CheckRequestModified(this HttpContext context, string fileName, DateTime modifiedDate, ProcessRequestCallback callback)
        {
            if (!context.Request.IsFileModified(fileName, modifiedDate))
            {
                context.Response.ReturnNotModified(fileName, DateTime.Now);
            }
            else
            {
                context.Response.AppendModifiedMeta(fileName, DateTime.Now);
                callback(context);
            }
        }

        /// <summary>
        /// Returns a site relative HTTP path from a partial path starting out with a ~.
        /// Same syntax that ASP.Net internally supports but this method can be used
        /// outside of the Page framework.
        /// 
        /// Works like Control.ResolveUrl including support for ~ syntax
        /// but returns an absolute URL.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="originalUrl">Any Url including those starting with ~</param>
        /// <returns>relative url</returns>
        public static string ResolveUrl(this HttpContext context, string originalUrl)
        {

            if (originalUrl == null) return null; // *** Absolute path - just return    
            if (originalUrl.IndexOf("://") != -1) return originalUrl; // *** Fix up image path for ~ root app dir directory    
            if (originalUrl.StartsWith("~"))
            {
                string newUrl = "";
                if (context != null)
                    newUrl = context.Request.ApplicationPath + originalUrl.Substring(1).Replace("//", "/");
                else // *** Not context: assume current directory is the base directory            
                    throw new ArgumentException("Invalid URL: Relative URL not allowed.");
                // *** Just to be sure fix up any double slashes        
                return newUrl;
            }
            return originalUrl;
        }

        /// <summary>
        /// This method returns a fully qualified absolute server Url which includes
        /// the protocol, server, port in addition to the server relative Url.
        /// 
        /// Works like Control.ResolveUrl including support for ~ syntax
        /// but returns an absolute URL.
        /// </summary>
        /// <param name="ServerUrl">Any Url, either App relative or fully qualified</param>
        /// <param name="forceHttps">if true forces the url to use https</param>
        /// <returns></returns>
        public static string ResolveServerUrl(this HttpContext context, string serverUrl, bool forceHttps)
        {

            // *** Is it already an absolute Url?    
            if (serverUrl.IndexOf("://") > -1) return serverUrl;// *** Start by fixing up the Url an Application relative Url    
            string newUrl = ResolveUrl(context, serverUrl);
            Uri originalUri = HttpContext.Current.Request.Url;
            newUrl = (forceHttps ? "https" : originalUri.Scheme) + "://" + originalUri.Authority + newUrl;
            return newUrl;
        }

        /// <summary>
        /// This method returns a fully qualified absolute server Url which includes
        /// the protocol, server, port in addition to the server relative Url.
        /// 
        /// It work like Page.ResolveUrl, but adds these to the beginning.
        /// This method is useful for generating Urls for AJAX methods
        /// </summary>
        /// <param name="ServerUrl">Any Url, either App relative or fully qualified</param>
        /// <returns></returns>
        public static string ResolveServerUrl(this HttpContext context, string serverUrl)
        {
            return ResolveServerUrl(context, serverUrl, false);
        }
    }
}
