using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Authentication;
using System.Security.Claims;
using ALMS.App.Models;
using Blazored.LocalStorage;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.Extensions.Configuration;

namespace ALMS.App.Services
{

    // referred from: https://qiita.com/nobu17/items/91c96ede1bd043fe1373

    public class AuthStateProvider : AuthenticationStateProvider
    {
        private readonly HttpClient _httpClient;
        private readonly ILocalStorageService _localStorage;

        public AuthStateProvider(HttpClient httpClient, ILocalStorageService localStorage)
        {
            _httpClient = httpClient;
            _localStorage = localStorage;
        }

        public async override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                var savedToken = await _localStorage.GetItemAsync<string>("authToken");
                var userID = await _localStorage.GetItemAsync<string>("account");
                var roles = await _localStorage.GetItemAsync<List<string>>("roles");
                if (string.IsNullOrWhiteSpace(savedToken))
                {
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }

                var claims = new List<Claim>();
                claims.Add(new Claim(ClaimTypes.Name, userID));
                foreach(var role in roles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, role));
                }

                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("bearer", savedToken);
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(claims, "apiauth")));
            }
            catch {
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }
        }

        public async Task MarkUserAsAuthenticated(string account, List<string> roles, string authToken)
        {
            await _localStorage.SetItemAsync("account", account);
            await _localStorage.SetItemAsync("authToken", authToken);
            await _localStorage.SetItemAsync("roles", roles);
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        public async Task MarkUserAsLoggedOut()
        {
            await _localStorage.RemoveItemAsync("account");
            await _localStorage.RemoveItemAsync("roles");
            await _localStorage.RemoveItemAsync("authToken");
            if (_httpClient.DefaultRequestHeaders.Authorization != null)
            {
                _httpClient.DefaultRequestHeaders.Authorization = null;
            }
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }
    }

    public interface IAuthService
    {
        Task<LoginResult> LoginAsync(LoginModel loginModel);
        Task LogoutAsync();
    }

    public class LoginModel
    {
        public string Account { get; set; }
        public string Password { get; set; }
    }

    public class LoginResult
    {
        public bool IsSuccessful { get; set; }
        public Exception Error { get; set; }
        public string IDToken { get; set; }
    }

    public class DatabaseAuthService : IAuthService
    {
        private readonly DatabaseService DB;
        private readonly IConfiguration Config;
        private readonly AuthenticationStateProvider _authenticationStateProvider;


        public DatabaseAuthService(AuthenticationStateProvider authenticationStateProvider, DatabaseService db, IConfiguration config)
        {
            _authenticationStateProvider = authenticationStateProvider;
            DB = db;
            Config = config;
        }

        public async Task<LoginResult> LoginAsync(LoginModel loginModel)
        {
            var user = DB.Context.Users.Where(u => u.Account == loginModel.Account).FirstOrDefault();

            if(user != null && user.Authenticate(loginModel.Password, Config))
            {
                var token = Guid.NewGuid().ToString("N").Substring(0, 16);
                var res = new LoginResult()
                {
                    IsSuccessful = true,
                    IDToken = token
                };
                var roles = new List<string>();
                if (user.IsAdmin) { roles.Add("Admin"); }
                if (user.IsSenior) { roles.Add("Senior"); }

                await ((AuthStateProvider)_authenticationStateProvider).MarkUserAsAuthenticated(loginModel.Account, roles, res.IDToken);
                return res;
            }
            else
            {
                return new LoginResult()
                {
                    IsSuccessful = false,
                    Error = new AuthenticationException("NotAuthrized")
                };
            }
        }

        public async Task LogoutAsync()
        {
            await ((AuthStateProvider)_authenticationStateProvider).MarkUserAsLoggedOut();
        }
    }
}
