using Microsoft.AspNetCore.Authorization;
using SilkierQuartz;
using SilkierQuartz.Authorization;
using SilkierQuartz.Configuration;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Reflection;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class ServiceCollectionExtensions
    {
        [Obsolete("We recommend AddSilkierQuartz")]
        public static IServiceCollection AddQuartzmin(this IServiceCollection services, Action<NameValueCollection> stdSchedulerFactoryOptions = null)
            => services.AddSilkierQuartz(stdSchedulerFactoryOptions: stdSchedulerFactoryOptions);

        public static IServiceCollection AddSilkierQuartz(
            this IServiceCollection services,
            Action<SilkierQuartzOptions> configureOptions = null,
            Action<SilkierQuartzAuthenticationOptions> configureAuthenticationOptions = null,
            Action<NameValueCollection> stdSchedulerFactoryOptions = null,
            Func<List<Assembly>> jobsasmlist = null)
        {
            var options = new SilkierQuartzOptions();
            configureOptions?.Invoke(options);
            services.AddSingleton(options);

            var authenticationOptions = new SilkierQuartzAuthenticationOptions();
            configureAuthenticationOptions?.Invoke(authenticationOptions);

            services.AddSingleton(authenticationOptions);
            if (authenticationOptions.AccessRequirement != SilkierQuartzAuthenticationOptions.SimpleAccessRequirement.AllowAnonymous)
            {
                var normalizedVirtualPath = $"{options.VirtualPathRoot}{(options.VirtualPathRoot.EndsWith('/') ? "" : "/")}";
                var cookiePath = $"{options.BasePath}{(options.BasePath.EndsWith('/') ? "" : "/")}{normalizedVirtualPath.Trim('/')}";
                services
                    .AddAuthentication(authenticationOptions.AuthScheme)
                    .AddCookie(authenticationOptions.AuthScheme, cfg =>
                    {
                        cfg.Cookie.Name = authenticationOptions.CookieName;
                        cfg.Cookie.Path = cookiePath;
                        cfg.LoginPath = $"{normalizedVirtualPath}Authenticate/Login";
                        cfg.AccessDeniedPath = $"{normalizedVirtualPath}Authenticate/Login";
                        cfg.ExpireTimeSpan = TimeSpan.FromDays(7);
                        cfg.SlidingExpiration = true;
                    });
            }
            services.AddAuthorization(opts =>
                {
                    opts.AddPolicy(SilkierQuartzAuthenticationOptions.AuthorizationPolicyName, builder =>
                    {
                        builder.AddRequirements(new SilkierQuartzDefaultAuthorizationRequirement(authenticationOptions.AccessRequirement));
                    });
                });
            services.AddScoped<IAuthorizationHandler, SilkierQuartzDefaultAuthorizationHandler>();


            services.UseQuartzHostedService(stdSchedulerFactoryOptions);

            var types = JobsListHelper.GetSilkierQuartzJobs(jobsasmlist?.Invoke());
            types.ForEach(t =>
            {
                var so = t.GetCustomAttribute<SilkierQuartzAttribute>();
                services.AddQuartzJob(t, so.Identity ?? t.Name, so.Manual ? true : false, so.Group, so.Description ?? t.FullName);
            });
            return services;
        }
    }
}
