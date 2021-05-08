using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Newtonsoft.Json;
using RMuseum.Models.Auth.Memory;
using RSecurityBackend.Models.Auth.Memory;
using RSecurityBackend.Models.Auth.ViewModels;
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace GanjooRazor.Utils
{
    /// <summary>
    /// Ganjoor Session Checker
    /// </summary>
    public class GanjoorSessionChecker
    {
        /// <summary>
        /// if user is logged in adds user token to <paramref name="secureClient"/> and then checks user session and if needs renewal, renews it
        /// </summary>
        /// <param name="secureClient"></param>
        /// <param name="request"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        public static async Task<bool> PrepareClient(HttpClient secureClient, HttpRequest request, HttpResponse response)
        {
            if (string.IsNullOrEmpty(request.Cookies["Token"]) || string.IsNullOrEmpty(request.Cookies["SessionId"]))
                return false;
            secureClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", request.Cookies["Token"]);
            var r = await secureClient.GetAsync($"{APIRoot.Url}/api/users/checkmysession/?sessionId={request.Cookies["SessionId"]}");
            if (r.StatusCode == HttpStatusCode.OK)
            {
                return true;
            }
            else
            if(r.StatusCode == HttpStatusCode.Unauthorized)
            {
                var reLoginUrl = $"{APIRoot.Url}/api/users/relogin/{request.Cookies["SessionId"]}";
                var reLoginResponse = await secureClient.PutAsync(reLoginUrl, null);

                if (reLoginResponse.StatusCode != HttpStatusCode.OK)
                {
                    return false;
                }

                LoggedOnUserModel loggedOnUser = JsonConvert.DeserializeObject<LoggedOnUserModel>(await reLoginResponse.Content.ReadAsStringAsync());

                secureClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", loggedOnUser.Token);

                var cookieOption = new CookieOptions()
                {
                    Expires = DateTime.Now.AddDays(365),
                };

                response.Cookies.Append("UserId", loggedOnUser.User.Id.ToString(), cookieOption);
                response.Cookies.Append("SessionId", loggedOnUser.SessionId.ToString(), cookieOption);
                response.Cookies.Append("Token", loggedOnUser.Token, cookieOption);
                response.Cookies.Append("Username", loggedOnUser.User.Username, cookieOption);
                response.Cookies.Append("Name", $"{loggedOnUser.User.FirstName} {loggedOnUser.User.SureName}", cookieOption);
                response.Cookies.Append("NickName", $"{loggedOnUser.User.NickName}", cookieOption);

                bool canEditContent = false;
                var ganjoorEntity = loggedOnUser.SecurableItem.Where(s => s.ShortName == RMuseumSecurableItem.GanjoorEntityShortName).SingleOrDefault();
                if(ganjoorEntity != null)
                {
                    var op = ganjoorEntity.Operations.Where(o => o.ShortName == SecurableItem.ModifyOperationShortName).SingleOrDefault();
                    if(op != null)
                    {
                        canEditContent = op.Status;
                    }
                }

                response.Cookies.Append("CanEdit", canEditContent.ToString(), cookieOption);


                return true;

            }
            return false;
        }

        /// <summary>
        /// has permission
        /// </summary>
        /// <param name="request"></param>
        /// <param name="secuableShortName"></param>
        /// <param name="operationShortName"></param>
        /// <returns></returns>
        public static async Task<bool> IsPermitted(HttpRequest request, HttpResponse response, string secuableShortName, string operationShortName)
        {

            using (HttpClient secureClient = new HttpClient())
            {
                if (await PrepareClient(secureClient, request, response))
                {
                    var res = await secureClient.GetAsync($"{APIRoot.Url}/api/users/securableitems");
                    if (!res.IsSuccessStatusCode)
                    {
                        return false;
                    }

                    SecurableItem[] secuarbleItems = JsonConvert.DeserializeObject<SecurableItem[]>(await res.Content.ReadAsStringAsync());
                    var secuarbleItem = secuarbleItems.Where(s => s.ShortName == secuableShortName).SingleOrDefault();

                    if (secuarbleItem == null)
                        return false;

                    var operation = secuarbleItem.Operations.Where(o => o.ShortName == operationShortName).SingleOrDefault();
                    if (operation == null)
                        return false;

                    return operation.Status;

                }
                else
                {
                    return false;
                }
            }
        }

        /// <summary>
        /// apply permissions to view data
        /// </summary>
        /// <param name="request"></param>
        /// <param name="viewData"></param>
        public static async Task ApplyPermissionsToViewData(HttpRequest request, HttpResponse response, ViewDataDictionary viewData)
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await PrepareClient(secureClient, request, response))
                {
                    var res = await secureClient.GetAsync($"{APIRoot.Url}/api/users/securableitems");
                    if (!res.IsSuccessStatusCode)
                    {
                        return;
                    }

                    SecurableItem[] secuarbleItems = JsonConvert.DeserializeObject<SecurableItem[]>(await res.Content.ReadAsStringAsync());
                   
                    foreach(SecurableItem securableItem in secuarbleItems)
                        foreach(SecurableItemOperation operation in securableItem.Operations)
                            if(operation.Status)
                                viewData[$"{securableItem.ShortName}-{operation.ShortName}"] = true;

                }
                
            }
        }
    }
}
