using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using ALMS.App.ViewModels;
using ALMS.App.Models.Entities;
using ALMS.App.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;

// For more information on enabling MVC for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace ALMS.App.Controllers
{

    [AllowAnonymous]
    public class LoginController : Controller
    {
        DatabaseService DB;
        IConfiguration Config;
        public LoginController(DatabaseService db, IConfiguration config)
        {
            DB = db;
            Config = config;
        }

        [HttpGet("/logout", Name = "Logout")]
        public async Task<ActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return LocalRedirect("/Login");
        }

        [HttpGet("/login", Name = "Login")]
        public ActionResult Login()
        {
            return View(new LoginViewModel());
        }


        [HttpPost("/login")]
        public async Task<ActionResult> Login([FromQuery]string ReturnUrl, LoginViewModel m)
        {
            if (!ModelState.IsValid)
            {
                return View(m);
            }

            var user = DB.Context.Users.Include(x => x.Lectures).Include(x => x.Sandboxes).Include(x => x.LectureUsers).ThenInclude(x => x.Lecture).Where(x => x.Account == m.Name).FirstOrDefault();
            if (user == null)
            {
                ViewBag.Notice = "The user is not exist.";
                return LocalRedirect("/login");
            }
            var isSuccess = user.Authenticate(m.Password, Config);
            if (!isSuccess)
            {
                ViewBag.Notice = "Failed.";
                return LocalRedirect("/login");
            }

            var claims = new List<Claim>();
            claims.Add(new Claim(ClaimTypes.Name, user.Account));
            claims.Add(new Claim(ClaimTypes.Email, user.EmailAddress));
            if (user.IsSenior)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Senior"));
            }
            if (user.IsAdmin)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }

            var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);

            await HttpContext.SignInAsync(
              CookieAuthenticationDefaults.AuthenticationScheme,
              new ClaimsPrincipal(claimsIdentity),
              new AuthenticationProperties
              {
                  IsPersistent = m.RememberMe
              });

            return LocalRedirect(ReturnUrl ?? "~/");
        }
    }
}
