using System.IO;
using System.Net;
using System.Text;
using System.Web;

namespace RMuseum.Utils
{
    public static class TajikTransilerator
    {
        public static string Transilerate(string input)
        {
            string txt = "ctl00%24ContentPlaceHolder2%24ScriptManager1=ctl00%24ContentPlaceHolder2%24UpdatePanel1%7Cctl00%24ContentPlaceHolder2%24btnConvert&__EVENTTARGET=&__EVENTARGUMENT=&ctl00%24ContentPlaceHolder2%24txtPersian=%D8%AD%D8%B3%D9%86&ctl00%24ContentPlaceHolder2%24txtSuggest=&__VIEWSTATE=FJBM71Nf2Mzx%2BAjVP0LDLRQCvG%2FugjscaiM7gw%2FgpSEoehbsWEMsC7oirmYVtQOUfi54eqgLZgamFYJBb1aYL%2F14dcixVbv1QXAiL9MM6etgOF2vr6c%2FWRXq%2FGJG2cHi2RB8WjgX%2FCUwp2H%2F3DmyVIY4l24XTb8Y0Onu8oNXM5dbfxAs&__VIEWSTATEGENERATOR=82B657B9&__EVENTVALIDATION=y6DlG61vZjyA%2B6t19Hfyomarliwfg8r%2FDo7E%2BXbLHgNT0mxwLpAsjMso%2FrXEsKomZQZRKOwE1s8PwY1aJVUDoiWEfnlLe9HNFndoQBA5UwX0LIHzVZez4spR534XJhre0pFd2PrCeJj35v9clTYrdBfiZ1hvyXAggj5iYlCWl56DVz8JfmNSXlsN2Wirik5DsTGXsw%3D%3D&__ASYNCPOST=true&ctl00%24ContentPlaceHolder2%24btnConvert=%D8%A8%D8%B1%DA%AF%D8%B1%D8%AF%D8%A7%D9%86";

            txt = txt.Replace("%D8%AD%D8%B3%D9%86", HttpUtility.UrlEncode(input));

            HttpWebRequest request = HttpWebRequest.Create("http://persian-tajik.ir/farsitotajiki.aspx") as HttpWebRequest;

            ASCIIEncoding ascii = new ASCIIEncoding();
            byte[] postBytes = ascii.GetBytes(txt);

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

                return responseString.Substring(startDivIndex, endDivIndex - startDivIndex).Replace("<span style=\"color: red\">", "").Replace("</span>", "").Trim();
            }
            return "";
        }
    }
}
