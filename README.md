[![License: MIT](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)
[![.NET Core](https://github.com/maikebing/SilkierQuartz/workflows/.NET%20Core/badge.svg?branch=master)](https://github.com/IoTSharp/SilkierQuartz/actions/workflows/dotnet-core.yml)
[![Build status](https://ci.appveyor.com/api/projects/status/0ojmooqvycks11kw?svg=true)](https://ci.appveyor.com/project/MaiKeBing/silkierquartz)

SilkierQuartz is a new after merging  [Quartzmin](https://github.com/jlucansky/Quartzmin) and  [QuartzHostedService](https://github.com/mukmyash/QuartzHostedService)!

> [Quartz.NET](https://www.quartz-scheduler.net) is a full-featured, open source job scheduling system that can be used from smallest apps to large scale enterprise systems.


> [Quartzmin](https://github.com/jlucansky/Quartzmin) Quartzmin is powerful, easy to use web management tool for Quartz.NET

>  [QuartzHostedService](https://github.com/mukmyash/QuartzHostedService) QuartzHostedService is easy to host Quartz as service in .Net Core !


So  

SilkierQuartz can be used within your existing application with minimum effort as a Quartz.NET plugin when it automatically creates embedded web server.


![Demo](https://raw.githubusercontent.com/jlucansky/public-assets/master/Quartzmin/demo.gif)

The goal of this project is to provide convenient tool to utilize most of the functionality that Quartz.NET enables. The biggest challenge was to create a simple yet effective editor of job data map which is heart of Quartz.NET. Every job data map item is strongly typed and SilkierQuartz can be easily extended with a custom editor for your specific type beside standard supported types such as String, Integer, DateTime and so on. 

SilkierQuartz was created with **Semantic UI** and **Handlebars.Net** as the template engine.

## Packages

| Package | Description | Version | Downloads |
| --- | --- | --- | --- |
| [SilkierQuartz](https://www.nuget.org/packages/SilkierQuartz/) | Main web UI package for Quartz.NET management, including dashboard, job editing, trigger management, and execution monitoring. | [![NuGet version](https://img.shields.io/nuget/v/SilkierQuartz.svg)](https://www.nuget.org/packages/SilkierQuartz/) | [![NuGet downloads](https://img.shields.io/nuget/dt/SilkierQuartz.svg)](https://www.nuget.org/packages/SilkierQuartz/) |
| [SilkierQuartz.Plugins.RecentHistory](https://www.nuget.org/packages/SilkierQuartz.Plugins.RecentHistory/) | Core recent-history plugin that records execution history and supports centralized relational persistence through the provider-agnostic registration API. | [![NuGet version](https://img.shields.io/nuget/v/SilkierQuartz.Plugins.RecentHistory.svg)](https://www.nuget.org/packages/SilkierQuartz.Plugins.RecentHistory/) | [![NuGet downloads](https://img.shields.io/nuget/dt/SilkierQuartz.Plugins.RecentHistory.svg)](https://www.nuget.org/packages/SilkierQuartz.Plugins.RecentHistory/) |
| [SilkierQuartz.Plugins.RecentHistory.EFCoreSqlServer](https://www.nuget.org/packages/SilkierQuartz.Plugins.RecentHistory.EFCoreSqlServer/) | EF Core SQL Server history store package with ready-to-use `AddEfCoreExecutionHistoryStore(...)` registration for Microsoft SQL Server deployments. | [![NuGet version](https://img.shields.io/nuget/v/SilkierQuartz.Plugins.RecentHistory.EFCoreSqlServer.svg)](https://www.nuget.org/packages/SilkierQuartz.Plugins.RecentHistory.EFCoreSqlServer/) | [![NuGet downloads](https://img.shields.io/nuget/dt/SilkierQuartz.Plugins.RecentHistory.EFCoreSqlServer.svg)](https://www.nuget.org/packages/SilkierQuartz.Plugins.RecentHistory.EFCoreSqlServer/) |
| [SilkierQuartz.Plugins.RecentHistory.EFCoreNpgsql](https://www.nuget.org/packages/SilkierQuartz.Plugins.RecentHistory.EFCoreNpgsql/) | EF Core PostgreSQL history store package for teams using Npgsql and wanting the same execution-history behavior on PostgreSQL. | [![NuGet version](https://img.shields.io/nuget/v/SilkierQuartz.Plugins.RecentHistory.EFCoreNpgsql.svg)](https://www.nuget.org/packages/SilkierQuartz.Plugins.RecentHistory.EFCoreNpgsql/) | [![NuGet downloads](https://img.shields.io/nuget/dt/SilkierQuartz.Plugins.RecentHistory.EFCoreNpgsql.svg)](https://www.nuget.org/packages/SilkierQuartz.Plugins.RecentHistory.EFCoreNpgsql/) |
| [SilkierQuartz.Plugins.RecentHistory.EFCoreSqlite](https://www.nuget.org/packages/SilkierQuartz.Plugins.RecentHistory.EFCoreSqlite/) | EF Core SQLite history store package for lightweight or embedded deployments that want persistent recent job history. | [![NuGet version](https://img.shields.io/nuget/v/SilkierQuartz.Plugins.RecentHistory.EFCoreSqlite.svg)](https://www.nuget.org/packages/SilkierQuartz.Plugins.RecentHistory.EFCoreSqlite/) | [![NuGet downloads](https://img.shields.io/nuget/dt/SilkierQuartz.Plugins.RecentHistory.EFCoreSqlite.svg)](https://www.nuget.org/packages/SilkierQuartz.Plugins.RecentHistory.EFCoreSqlite/) |
| [SilkierQuartz.Plugins.RecentHistory.EFCoreMySql](https://www.nuget.org/packages/SilkierQuartz.Plugins.RecentHistory.EFCoreMySql/) | EF Core MySQL history store package for MySQL-compatible deployments using the Pomelo provider. | [![NuGet version](https://img.shields.io/nuget/v/SilkierQuartz.Plugins.RecentHistory.EFCoreMySql.svg)](https://www.nuget.org/packages/SilkierQuartz.Plugins.RecentHistory.EFCoreMySql/) | [![NuGet downloads](https://img.shields.io/nuget/dt/SilkierQuartz.Plugins.RecentHistory.EFCoreMySql.svg)](https://www.nuget.org/packages/SilkierQuartz.Plugins.RecentHistory.EFCoreMySql/) |

Choose the package that matches your storage model:

- `SilkierQuartz` for the web dashboard.
- `SilkierQuartz.Plugins.RecentHistory` for the core recent-history plugin and provider-agnostic relational registration.
- One EF Core provider package when you want recent history persisted with SQL Server, PostgreSQL, SQLite, or MySQL.

##  SilkierQuartz's Features
  -  automatically discover IJob subclasses with SilkierQuartzAttribute
  -  With QuartzHostedService and more extensions
  -  Authentication feature  , by [khanhna](https://github.com/khanhna)


## Quartzmin's Features
- Add, modify jobs and triggers
- Add, modify calendars (Annual, Cron, Daily, Holiday, Monthly, Weekly)
- Change trigger type to Cron, Simple, Calendar Interval or Daily Time Interval
- Set typed job data map values (bool, DateTime, int, float, long, double, decimal, string, byte[])
- Create custom type editor for complex type in job data map
- Manage scheduler state (standby, shutdown)
- Pause and resume job and trigger groups
- Pause and resume triggers individually
- Pause and resume all triggers for specific job
- Trigger specific job immediately
- Watch currently executing jobs
- Interrupt executing job
- See next scheduled dates for Cron
- See recent job history, state and error messages

## Install
SilkierQuartz is available on [nuget.org](https://www.nuget.org/packages/SilkierQuartz)

To install SilkierQuartz, run the following command in the Package Manager Console
```powershell
PM> Install-Package SilkierQuartz
```

To persist recent execution history with EF Core, install the matching provider package:

```powershell
PM> Install-Package SilkierQuartz.Plugins.RecentHistory.EFCoreSqlServer
PM> Install-Package SilkierQuartz.Plugins.RecentHistory.EFCoreNpgsql
PM> Install-Package SilkierQuartz.Plugins.RecentHistory.EFCoreSqlite
PM> Install-Package SilkierQuartz.Plugins.RecentHistory.EFCoreMySql
```

  
 

### ASP.NET Core middleware
Add to your `Program.cs` file:

```csharp
   public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                })
             .ConfigureSilkierQuartzHost();
     }

```
Add to your `Startup.cs` file:
```csharp
public void ConfigureServices(IServiceCollection services)
{
    services.AddSilkierQuartz();
    services.AddExecutionHistoryStore(setting =>
        setting.UseSqlite("Data Source=silkierquartz-history.db"));
}

public void Configure(IApplicationBuilder app)
{
    /* Optional for authentication
    app.UseAuthentication();
    app.AddSilkierQuartzAuthentication();
    app.UseAuthorization();
    */
    app.UseSilkierQuartz(new SilkierQuartzOptions()
                {
                    Scheduler = scheduler,
                    VirtualPathRoot = "/SilkierQuartz",
                    UseLocalTime = true,
                    DefaultDateFormat = "yyyy-MM-dd",
                    DefaultTimeFormat = "HH:mm:ss"
                    /* Optional for authentication
                    AccountName = "Your User Name",
                    AccountPassword = "Your User Password",
                    IsAuthenticationPersist = false
                    */
                });
}
```

For applications that already manage their own ADO.NET provider factory, you can also configure the centralized execution history store through the generic provider interface:
```csharp
services.AddExecutionHistoryStore(setting =>
    setting.UseAdoProvider(
        providerInvariantName: "Microsoft.Data.SqlClient",
        connectionString: configuration.GetConnectionString("QuartzHistory"),
        providerFactory: SqlClientFactory.Instance));
```

## Notes
In clustered environments, you can now register a centralized execution history store directly with `AddExecutionHistoryStore(...)`. SilkierQuartz will automatically enable `ExecutionHistoryPlugin`, and if no external store is configured the plugin falls back to the existing in-process history store.


## License
This project is made available under the MIT license. See [LICENSE](LICENSE) for details.
