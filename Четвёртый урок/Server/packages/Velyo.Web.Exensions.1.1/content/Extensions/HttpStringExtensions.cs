using System.Diagnostics;
using System.Text;

namespace System.Web
{
    /// <summary>
    /// Extension methods for <see cref="System.String"/> when used in System.Web
    /// </summary>
    [DebuggerStepThrough]
    internal static class HttpStringExtensions
    {
        /// <summary>
        /// Decodes a string from URL usage.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static string UrlDecode(this string value)
        {
            if (value != null)
            {
                StringBuilder buffer = new StringBuilder(value);
                buffer.Replace("--", "DASH");
                buffer.Replace('-', ' ');
                buffer.Replace("DASH", "-");
                return HttpUtility.UrlDecode(buffer.ToString());
            }
            return value;
        }

        /// <summary>
        /// Encodes a string for URL usage.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns></returns>
        public static string UrlEncode(this string value)
        {
            if (value != null)
            {
                StringBuilder buffer = new StringBuilder(value);
                buffer.Replace("-", "--");
                buffer.Replace(' ', '-');
                return HttpUtility.UrlEncode(buffer.ToString());
            }
            return value;
        }
    }
}
