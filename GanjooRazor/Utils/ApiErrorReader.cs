using Newtonsoft.Json;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace GanjooRazor.Utils
{
    /// <summary>
    /// Turns a FAILED API response into a displayable error message, and never returns null/empty.
    ///
    /// Pages used to do <c>JsonConvert.DeserializeObject&lt;string&gt;(await response.Content.ReadAsStringAsync())</c>
    /// directly. That works for the API's normal JSON-encoded error string, but returns null for an
    /// empty body (every bare 401/403 from the authorization layer, 404 without a body, ...) and
    /// throws for a non-JSON body (proxy/IIS error pages, ProblemDetails). A null made pages show
    /// their "nothing here" empty state (or a blank error) instead of an error - e.g. a failed
    /// session renewal on /Admin/ReviewEdits looked like "no edits to review".
    /// </summary>
    public static class ApiErrorReader
    {
        /// <summary>
        /// message used when the failure means the user's session is not usable
        /// </summary>
        public const string NotLoggedInMessage = "لطفاً از گنجور خارج و مجددا به آن وارد شوید.";

        /// <summary>
        /// the API's JSON-encoded error string when there is one; otherwise a message built from the
        /// raw body (plain text) or the HTTP status (empty/HTML/unreadable body)
        /// </summary>
        public static async Task<string> ReadErrorAsync(HttpResponseMessage response)
        {
            string body = "";
            try
            {
                if (response.Content != null)
                    body = await response.Content.ReadAsStringAsync();
            }
            catch
            {
                body = "";
            }

            if (!string.IsNullOrWhiteSpace(body))
            {
                try
                {
                    string message = JsonConvert.DeserializeObject<string>(body);
                    if (!string.IsNullOrWhiteSpace(message))
                        return message;
                }
                catch (JsonException)
                {
                    // not a JSON string: plain text, a JSON object (ProblemDetails) or an HTML error page
                    string trimmed = body.Trim();
                    if (trimmed.Length <= 500 && !trimmed.StartsWith("<") && !trimmed.StartsWith("{") && !trimmed.StartsWith("["))
                        return trimmed;
                }
            }

            switch (response.StatusCode)
            {
                case HttpStatusCode.Unauthorized:
                    return NotLoggedInMessage;
                case HttpStatusCode.Forbidden:
                    return "شما اجازهٔ انجام این کار را ندارید، یا نشست شما نیاز به ورود مجدد دارد. اگر مطمئنید مجاز هستید، از گنجور خارج و مجددا وارد شوید.";
                case HttpStatusCode.NotFound:
                    return "مورد درخواستی یافت نشد.";
                default:
                    return $"خطای ناشناخته از سرور (کد {(int)response.StatusCode}). لطفاً دوباره تلاش کنید.";
            }
        }
    }
}
