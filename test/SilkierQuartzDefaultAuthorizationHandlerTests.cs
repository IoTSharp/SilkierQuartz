using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using SilkierQuartz;
using SilkierQuartz.Authorization;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Xunit;

namespace SilkierQuartz.Test
{
    public class SilkierQuartzDefaultAuthorizationHandlerTests
    {
        private static ClaimsPrincipal AnonymousPrincipal() => new ClaimsPrincipal();

        private static ClaimsPrincipal UnauthenticatedPrincipal() =>
            new ClaimsPrincipal(new ClaimsIdentity());

        private static ClaimsPrincipal AuthenticatedPrincipal(IEnumerable<Claim> claims = null)
        {
            var identity = new ClaimsIdentity(claims, "TestAuthType");
            return new ClaimsPrincipal(identity);
        }

        private static SilkierQuartzAuthenticationOptions OptionsFor(
            SilkierQuartzAuthenticationOptions.SimpleAccessRequirement requirement,
            bool skip = false)
        {
            return new SilkierQuartzAuthenticationOptions
            {
                AccessRequirement = requirement,
                SkipDefaultRequirementHandler = skip,
                SilkierQuartzClaim = "SilkierQuartzManage",
                SilkierQuartzClaimValue = "Authorized"
            };
        }

        private static async Task<AuthorizationHandlerContext> InvokeHandler(
            SilkierQuartzAuthenticationOptions options,
            ClaimsPrincipal user)
        {
            var requirement = new SilkierQuartzDefaultAuthorizationRequirement(options.AccessRequirement);
            var context = new AuthorizationHandlerContext(new[] { requirement }, user, null);
            var handler = new SilkierQuartzDefaultAuthorizationHandler(options);
            await handler.HandleAsync(context);
            return context;
        }

        // ── AllowAnonymous ──────────────────────────────────────────────────────

        [Fact(DisplayName = "AllowAnonymous: anonymous principal succeeds")]
        public async Task AllowAnonymous_AnonymousPrincipal_Succeeds()
        {
            var ctx = await InvokeHandler(
                OptionsFor(SilkierQuartzAuthenticationOptions.SimpleAccessRequirement.AllowAnonymous),
                AnonymousPrincipal());

            ctx.HasSucceeded.Should().BeTrue();
        }

        [Fact(DisplayName = "AllowAnonymous: authenticated user also succeeds")]
        public async Task AllowAnonymous_AuthenticatedUser_Succeeds()
        {
            var ctx = await InvokeHandler(
                OptionsFor(SilkierQuartzAuthenticationOptions.SimpleAccessRequirement.AllowAnonymous),
                AuthenticatedPrincipal());

            ctx.HasSucceeded.Should().BeTrue();
        }

        // ── AllowOnlyAuthenticated ──────────────────────────────────────────────

        [Fact(DisplayName = "AllowOnlyAuthenticated: authenticated user succeeds")]
        public async Task AllowOnlyAuthenticated_AuthenticatedUser_Succeeds()
        {
            var ctx = await InvokeHandler(
                OptionsFor(SilkierQuartzAuthenticationOptions.SimpleAccessRequirement.AllowOnlyAuthenticated),
                AuthenticatedPrincipal());

            ctx.HasSucceeded.Should().BeTrue();
        }

        [Fact(DisplayName = "AllowOnlyAuthenticated: unauthenticated principal fails")]
        public async Task AllowOnlyAuthenticated_UnauthenticatedPrincipal_Fails()
        {
            var ctx = await InvokeHandler(
                OptionsFor(SilkierQuartzAuthenticationOptions.SimpleAccessRequirement.AllowOnlyAuthenticated),
                UnauthenticatedPrincipal());

            ctx.HasFailed.Should().BeTrue();
        }

        [Fact(DisplayName = "AllowOnlyAuthenticated: anonymous principal (null Identity) fails without exception")]
        public async Task AllowOnlyAuthenticated_AnonymousPrincipal_FailsWithoutException()
        {
            var ctx = await InvokeHandler(
                OptionsFor(SilkierQuartzAuthenticationOptions.SimpleAccessRequirement.AllowOnlyAuthenticated),
                AnonymousPrincipal());

            ctx.HasFailed.Should().BeTrue();
        }

        [Fact(DisplayName = "AllowOnlyAuthenticated: authenticated user without claim still succeeds (no claim required)")]
        public async Task AllowOnlyAuthenticated_AuthenticatedUserWithoutClaim_Succeeds()
        {
            var ctx = await InvokeHandler(
                OptionsFor(SilkierQuartzAuthenticationOptions.SimpleAccessRequirement.AllowOnlyAuthenticated),
                AuthenticatedPrincipal()); // no SilkierQuartz claim

            ctx.HasSucceeded.Should().BeTrue();
        }

        // ── AllowOnlyUsersWithClaim ─────────────────────────────────────────────

        [Fact(DisplayName = "AllowOnlyUsersWithClaim: authenticated user with correct claim succeeds")]
        public async Task AllowOnlyUsersWithClaim_WithClaim_Succeeds()
        {
            var claims = new[] { new Claim("SilkierQuartzManage", "Authorized") };
            var ctx = await InvokeHandler(
                OptionsFor(SilkierQuartzAuthenticationOptions.SimpleAccessRequirement.AllowOnlyUsersWithClaim),
                AuthenticatedPrincipal(claims));

            ctx.HasSucceeded.Should().BeTrue();
        }

        [Fact(DisplayName = "AllowOnlyUsersWithClaim: authenticated user without claim fails")]
        public async Task AllowOnlyUsersWithClaim_WithoutClaim_Fails()
        {
            var ctx = await InvokeHandler(
                OptionsFor(SilkierQuartzAuthenticationOptions.SimpleAccessRequirement.AllowOnlyUsersWithClaim),
                AuthenticatedPrincipal()); // no claim

            ctx.HasFailed.Should().BeTrue();
        }

        [Fact(DisplayName = "AllowOnlyUsersWithClaim: authenticated user with wrong claim value fails")]
        public async Task AllowOnlyUsersWithClaim_WrongClaimValue_Fails()
        {
            var claims = new[] { new Claim("SilkierQuartzManage", "WrongValue") };
            var ctx = await InvokeHandler(
                OptionsFor(SilkierQuartzAuthenticationOptions.SimpleAccessRequirement.AllowOnlyUsersWithClaim),
                AuthenticatedPrincipal(claims));

            ctx.HasFailed.Should().BeTrue();
        }

        [Fact(DisplayName = "AllowOnlyUsersWithClaim: unauthenticated principal fails")]
        public async Task AllowOnlyUsersWithClaim_UnauthenticatedPrincipal_Fails()
        {
            var ctx = await InvokeHandler(
                OptionsFor(SilkierQuartzAuthenticationOptions.SimpleAccessRequirement.AllowOnlyUsersWithClaim),
                UnauthenticatedPrincipal());

            ctx.HasFailed.Should().BeTrue();
        }

        [Fact(DisplayName = "AllowOnlyUsersWithClaim: anonymous principal (null Identity) fails without exception")]
        public async Task AllowOnlyUsersWithClaim_AnonymousPrincipal_FailsWithoutException()
        {
            var ctx = await InvokeHandler(
                OptionsFor(SilkierQuartzAuthenticationOptions.SimpleAccessRequirement.AllowOnlyUsersWithClaim),
                AnonymousPrincipal());

            ctx.HasFailed.Should().BeTrue();
        }

        // ── Unknown requirement (deny-by-default) ───────────────────────────────

        [Fact(DisplayName = "Unknown AccessRequirement: authenticated user is denied (deny-by-default)")]
        public async Task UnknownRequirement_AuthenticatedUser_Fails()
        {
            var options = new SilkierQuartzAuthenticationOptions
            {
                AccessRequirement = (SilkierQuartzAuthenticationOptions.SimpleAccessRequirement)99,
                SkipDefaultRequirementHandler = false
            };
            var ctx = await InvokeHandler(options, AuthenticatedPrincipal());

            ctx.HasFailed.Should().BeTrue();
        }

        // ── SkipDefaultRequirementHandler ───────────────────────────────────────

        [Fact(DisplayName = "SkipDefaultRequirementHandler: handler is skipped, context remains pending")]
        public async Task SkipDefaultRequirementHandler_DoesNothing()
        {
            var options = OptionsFor(
                SilkierQuartzAuthenticationOptions.SimpleAccessRequirement.AllowOnlyAuthenticated,
                skip: true);
            var ctx = await InvokeHandler(options, UnauthenticatedPrincipal());

            ctx.HasSucceeded.Should().BeFalse();
            ctx.HasFailed.Should().BeFalse();
        }
    }
}
