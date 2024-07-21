using System.IO;
using System.Net;
using System.Text;
using System.Web;

namespace RMuseum.Utils
{
    public static class TajikTransilerator
    {
        public static string Transilerate(string input, string formDataForHassan)
        {
            formDataForHassan = formDataForHassan.Replace("%D8%AD%D8%B3%D9%86", HttpUtility.UrlEncode(input));

            HttpWebRequest request = HttpWebRequest.Create("http://persian-tajik.ir/farsitotajiki.aspx") as HttpWebRequest;

            ASCIIEncoding ascii = new ASCIIEncoding();
            byte[] postBytes = ascii.GetBytes(formDataForHassan);

            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            request.ContentLength = postBytes.Length;
            request.UserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.1; SV1; .NET CLR 1.1.4322; .NET CLR 2.0.50727)";

            // add post data to request
            Stream postStream = request.GetRequestStream();
            postStream.Write(postBytes, 0, postBytes.Length);
            postStream.Flush();
            postStream.Close();

            var response = (HttpWebResponse)request.GetResponse();

            var responseString = new StreamReader(response.GetResponseStream()).ReadToEnd();


            int startIndex = responseString.IndexOf("ContentPlaceHolder2_txtCirlic");
            if (startIndex != -1)
            {
                int startDivIndex = responseString.IndexOf(">", startIndex);
                if (startDivIndex != -1)
                {
                    startDivIndex++;
                }

                int endDivIndex = responseString.IndexOf("</div>", startDivIndex);

                return responseString
                    .Substring(startDivIndex, endDivIndex - startDivIndex)
                    .Replace("<span style=\"color: red\">", "")
                    .Replace("</span>", "")
                    .Replace("<br>", "\r\n")
                    .Trim();
            }
            return "";
        }
    }
}
