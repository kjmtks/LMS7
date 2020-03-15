using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using ALMS.App.Services;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

namespace ALMS.App.ViewModels
{
    public class LoginPageModel : ComponentBase
    {
        [Inject]
        protected NavigationManager NavigationManager { get; set; }
        [Inject]
        protected IAuthService AuthService { get; set; }

        protected EditContext EditContext { get; set; }
        protected LoginData LoginData { get; set; }
        protected string ErrorMessage { get; set; }
        protected bool IsLoading { get; set; } = false;

        public LoginPageModel()
        {
            LoginData = new LoginData();
            EditContext = new EditContext(LoginData);
        }

        public async Task SubmitAsync()
        {
            IsLoading = true;
            var model = new LoginModel() { Account = LoginData.Account, Password = LoginData.Password };
            var result = await AuthService.LoginAsync(model);
            if (result.IsSuccessful)
            {
                NavigationManager.NavigateTo("/");
            }
            else
            {
                ErrorMessage = "Fail to login.";
            }
            IsLoading = false;
        }

    }

    public class LoginData
    {
        [Required, StringLength(32)]
        public string Account { get; set; }
        [Required, StringLength(128)]
        public string Password { get; set; }
    }
}
