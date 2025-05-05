using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SilkierQuartz.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SilkierQuartz.Controllers
{
    [AllowAnonymous]
    public class AuthenticateController : PageControllerBase
    {
        private readonly SilkierQuartzAuthenticationOptions authenticationOptions;

        public AuthenticateController(SilkierQuartzAuthenticationOptions authenticationOptions)
        {
            this.authenticationOptions = authenticationOptions ?? throw new ArgumentNullException(nameof(authenticationOptions));
        }

        [HttpGet]
        public async Task<IActionResult> Login([FromServices] IAuthenticationSchemeProvider schemes)
        {
            if (authenticationOptions.AccessRequirement == SilkierQuartzAuthenticationOptions.SimpleAccessRequirement.AllowAnonymous)
            {
                return RedirectToAction(nameof(SchedulerController.Index), nameof(Scheduler));
            }

            var silkierScheme = await schemes.GetSchemeAsync(authenticationOptions.AuthScheme);

            if (authenticationOptions.Authenticate == null)
            {
                foreach (var userClaim in HttpContext.User.Claims)
                {
                    Debug.WriteLine($"{userClaim.Type} - {userClaim.Value}");
                }

                if (HttpContext.User == null || !HttpContext.User.Identity.IsAuthenticated ||
                    !HttpContext.User.HasClaim(authenticationOptions.SilkierQuartzClaim,
                        authenticationOptions.SilkierQuartzClaimValue))
                {
                    await SignIn(false, SilkierQuartzAuthenticationOptions.DefaultUserName, SilkierQuartzAuthenticationOptions.DefaultPassword);
                    return RedirectToAction(nameof(SchedulerController.Index), nameof(Scheduler));
                }
                else
                {
                    return RedirectToAction(nameof(SchedulerController.Index), nameof(Scheduler));
                }
            }
            else
            {
                if (HttpContext.User == null || !HttpContext.User.Identity.IsAuthenticated ||
                    !HttpContext.User.HasClaim(authenticationOptions.SilkierQuartzClaim, authenticationOptions.SilkierQuartzClaimValue))
                {
                    ViewBag.IsLoginError = false;
                    return View(new AuthenticateViewModel());
                }
                else
                {
                    return RedirectToAction(nameof(SchedulerController.Index), nameof(Scheduler));
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> Login([FromForm] AuthenticateViewModel request)
        {
            var form = HttpContext.Request.Form;
            if (!authenticationOptions.Authenticate(request.UserName, request.Password))
            {
                request.IsLoginError = true;
                return View(request);
            }
            else
            {
                await SignIn(request.IsPersist, request.UserName, request.Password);
                return RedirectToAction(nameof(SchedulerController.Index), nameof(Scheduler));
            }
        }

        [HttpGet]
        [Authorize(Policy = SilkierQuartzAuthenticationOptions.AuthorizationPolicyName)]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(authenticationOptions.AuthScheme);
            return RedirectToAction(nameof(Login));
        }

        private async Task SignIn(bool isPersistentSignIn, string userName, string password)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, string.IsNullOrEmpty(userName)
                    ? "SilkierQuartzAdmin"
                    : SilkierQuartzAuthenticationOptions.DefaultUserName),

                new Claim(ClaimTypes.Name, string.IsNullOrEmpty(password)
                    ? "SilkierQuartzPassword"
                    : SilkierQuartzAuthenticationOptions.DefaultPassword),

                new Claim(authenticationOptions.SilkierQuartzClaim, authenticationOptions.SilkierQuartzClaimValue)
            };

            var authProperties = new AuthenticationProperties()
            {
                IsPersistent = isPersistentSignIn
            };

            var userIdentity = new ClaimsIdentity(claims, authenticationOptions.AuthScheme);
            await HttpContext.SignInAsync(authenticationOptions.AuthScheme, new ClaimsPrincipal(userIdentity),
                authProperties);
        }
    }
}
