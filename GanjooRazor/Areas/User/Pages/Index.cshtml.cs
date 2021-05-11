using GanjooRazor.Models.User;
using GanjooRazor.Utils;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Newtonsoft.Json;
using RSecurityBackend.Models.Auth.ViewModels;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace GanjooRazor.Areas.Panel.Pages
{
    public class IndexModel : PageModel
    {
        /// <summary>
        /// api model
        /// </summary>

        [BindProperty]
        public RegisterRAppUser UserInfo { get; set; }

        [BindProperty]
        public ChangePasswordModel ChangePasswordModel { get; set; }

        /// <summary>
        /// Last Error
        /// </summary>
        public string LastError { get; set; }

        /// <summary>
        /// password changed
        /// </summary>
        public string PasswordChanged { get; set; }

        /// <summary>
        /// get
        /// </summary>
        public async Task OnGetAsync()
        {
            LastError = "";
            PasswordChanged = "";
            await _PreparePage();
        }

        private async Task _PreparePage()
        {
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var userInfoResponse = await secureClient.GetAsync($"{APIRoot.Url}/api/users/{Request.Cookies["UserId"]}");
                    if (userInfoResponse.IsSuccessStatusCode)
                    {
                        PublicRAppUser userInfo = JsonConvert.DeserializeObject<PublicRAppUser>(await userInfoResponse.Content.ReadAsStringAsync());

                        UserInfo = new RegisterRAppUser()
                        {
                            Id = userInfo.Id,
                            Username = userInfo.Username,
                            FirstName = userInfo.FirstName,
                            SureName = userInfo.SureName,
                            NickName = userInfo.NickName,
                            PhoneNumber = userInfo.PhoneNumber,
                            Email = userInfo.Email,
                            Website = userInfo.Website,
                            Bio = userInfo.Bio,
                            RImageId = userInfo.RImageId,
                            Status = userInfo.Status,
                            IsAdmin = false,
                            Password = ""
                        };
                    }
                    else
                    {
                        LastError = await userInfoResponse.Content.ReadAsStringAsync();
                    }
                }
                else
                {
                    LastError = "لطفا از گنجور خارج و مجددا به آن وارد شوید.";
                }
            }
        }

        public async Task<IActionResult> OnPostSetMyInfoAsync(RegisterRAppUser UserInfo)
        {
            LastError = "";
            PasswordChanged = "";
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var isAdminResponse = await secureClient.GetAsync($"{APIRoot.Url}/api/users/isadmin?userId={Request.Cookies["UserId"]}");
                    if (isAdminResponse.IsSuccessStatusCode)
                    {
                        UserInfo.IsAdmin = JsonConvert.DeserializeObject<bool>(await isAdminResponse.Content.ReadAsStringAsync());

                        var putResponse = await secureClient.PutAsync($"{APIRoot.Url}/api/users/{Request.Cookies["UserId"]}", new StringContent(JsonConvert.SerializeObject(UserInfo), Encoding.UTF8, "application/json"));

                        if(!putResponse.IsSuccessStatusCode)
                        {
                            LastError = await putResponse.Content.ReadAsStringAsync();
                        }
                    }
                    else
                    {
                        LastError = await isAdminResponse.Content.ReadAsStringAsync();
                    }
                }
                else
                {
                    LastError = "لطفا از گنجور خارج و مجددا به آن وارد شوید.";
                }
            }
            await _PreparePage();
            return Page();
        }

        public async Task<IActionResult> OnPostSetMyPassAsync(ChangePasswordModel ChangePasswordModel)
        {
            LastError = "";
            PasswordChanged = "";
            if (ChangePasswordModel.NewPasswordRepeat != ChangePasswordModel.NewPassword)
            {
                LastError = "گذرواژهٔ جدید با تکرار آن همسان نیست.";
                return Page();
            }
            if(ChangePasswordModel.NewPassword == ChangePasswordModel.OldPassword)
            {
                LastError = "گذرواژهٔ جدید با گذرواژهٔ کنونی یکسان است.";
                return Page();
            }
            using (HttpClient secureClient = new HttpClient())
            {
                if (await GanjoorSessionChecker.PrepareClient(secureClient, Request, Response))
                {
                    var changePassResp = await secureClient.PostAsync($"{APIRoot.Url}/api/users/setmypassword", 
                        new StringContent(JsonConvert.SerializeObject
                            (
                            new SetPasswordModel()
                            {
                                OldPassword = ChangePasswordModel.OldPassword,
                                NewPassword = ChangePasswordModel.NewPassword
                            }
                            ), 
                        Encoding.UTF8, "application/json"));
                    if (!changePassResp.IsSuccessStatusCode)
                    {
                        LastError = await changePassResp.Content.ReadAsStringAsync();
                    }
                    else
                    {
                        PasswordChanged = "گذرواژهٔ شما به درستی تغییر کرد.";
                    }
                }
                else
                {
                    LastError = "لطفا از گنجور خارج و مجددا به آن وارد شوید.";
                }
            }
            await _PreparePage();
            return Page();
        }
    }
}
