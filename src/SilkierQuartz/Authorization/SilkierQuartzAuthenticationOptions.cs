using System;
using Microsoft.AspNetCore.Authentication.Cookies;
using SilkierQuartz.Authorization;

namespace SilkierQuartz
{
    /// <summary>
    /// Configuration for SilkierQuartz authentication
    /// </summary>
    public class SilkierQuartzAuthenticationOptions
    {
        /// <summary>
        /// The simple requirements for authorization when authorization is enabled
        /// </summary>
        public enum SimpleAccessRequirement
        {
            AllowAnonymous,
            AllowOnlyAuthenticated,
            AllowOnlyUsersWithClaim
        }

        public const string AuthorizationPolicyName = "SilkierQuartz";

        public string UserName { get; set; } = "admin";
        public string Password { get; set; } = "password";

        public Func<string, string, string, string, bool> Authenticate = (reqUserName, 
            reqPassword, verifyUserName, verifyPassword) =>
        {
            return
                string.Compare(reqUserName, verifyUserName, StringComparison.InvariantCulture) == 0 &&
                string.Compare(reqPassword, verifyPassword, StringComparison.InvariantCulture) == 0;
        };

        /// <summary>
        /// Sets the authentication scheme for the SilkierQuartz authentication signin.
        /// Defaults to <see cref="CookieAuthenticationDefaults.AuthenticationScheme"/>
        /// </summary>
        public string AuthScheme { get; set; }  = CookieAuthenticationDefaults.AuthenticationScheme;

        /// <summary>
        /// Sets the claim key used for authorization when <see cref="SimpleAccessRequirement"/> is set to <see cref="SimpleAccessRequirement.AllowOnlyUsersWithClaim"/>
        /// </summary>
        public string SilkierQuartzClaim { get; set; } = "SilkierQuartzManage";

        /// <summary>
        /// Sets the claim value used for authorization when <see cref="SimpleAccessRequirement"/> is set to <see cref="SimpleAccessRequirement.AllowOnlyUsersWithClaim"/>
        /// </summary>
        public string SilkierQuartzClaimValue { get; set; } = "Authorized";

        /// <summary>
        /// Used to poplate the Access Requirement for <see cref="SilkierQuartzDefaultAuthorizationRequirement"/>
        /// </summary>
        public SimpleAccessRequirement AccessRequirement { get; set; } = SimpleAccessRequirement.AllowOnlyUsersWithClaim;

        /// <summary>
        /// Set to true to skip all requirement checks in <see cref="SilkierQuartzDefaultAuthorizationHandler"/>
        /// </summary>
        public bool SkipDefaultRequirementHandler { get; set; } = false;
    }
}