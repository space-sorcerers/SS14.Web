# AI Coding Instructions for SS14.Web

## Repository Overview

This monorepo contains Space Station 14's backend web services: three ASP.NET Core microservices and two shared libraries. It provides authentication (OAuth2/OpenID Connect via IdentityServer4), game server listing hub, and the AuthHub website for account management.

## Project Structure

```
SS14.Web/                    Main website (net6.0) - MVC + Razor Pages, IdentityServer4
  Areas/Admin/Pages/         Admin area Razor Pages (user/server/OAuth client management)
  Areas/Identity/Pages/      Identity area Razor Pages (login, register, account)
  Controllers/               MVC Controllers (HomeController)
  Data/                      HubAuditLogManager
  HCaptcha/                  hCaptcha integration
  Helpers/                   PaginatedList, SortState, SearchHelper
  Models/                    ErrorViewModel
  Pages/                     Shared Razor Pages
  Views/                     MVC Views (Home, Shared/_Layout, _LoginPartial)
  wwwroot/                   Static assets (Bootstrap, jQuery, CSS, JS)

SS14.Auth/                   Auth API service (net8.0) - Game launcher authentication
  Controllers/               AuthApiController, PlayerApiController, QueryApiController, etc.
  Jobs/                      Quartz scheduled cleanup jobs
  Responses/                 Response DTOs
  Services/                  EnsureRolesService

SS14.ServerHub/              Server hub listing API service (net8.0)
  Controllers/               ServerListController
  ServerData/                ServerTagInfer, Tags
  Utility/                   HttpClientHelper, JsonHelper, StreamHelper

SS14.Auth.Shared/            Shared auth library (net6.0)
  Auth/                      SS14AuthHandler (custom auth scheme)
  Config/                    AccountConfiguration, PatreonConfiguration, etc.
  Data/                      EF Core DbContext, entities, migrations
  Emails/                    Email sending (SMTP/dummy)
  MutexDb/                   SQLite mutex database
  Sessions/                  SessionManager, SessionToken

SS14.ServerHub.Shared/       Shared hub library (net6.0)
  Data/                      HubDbContext, entities, migrations

SS14.WebEverythingShared/    Catch-all shared utilities (net6.0)
  AuditEntryHelper.cs, JsonConverters.cs, MoreStartupHelpers.cs

SS14.Web.Tests/              NUnit test project (net8.0)
OAuthTest/                   OAuth test client (net7.0)
Tools/                       Python helper scripts
```

## Technology Stack

| Area | Technology |
|------|-----------|
| Runtime | ASP.NET Core 6.0/7.0/8.0 (mixed across projects) |
| Language | C# 10-12 (file-scoped namespaces, primary constructors in newer code) |
| Auth | IdentityServer4 4.1.2, ASP.NET Core Identity |
| Database | PostgreSQL via Npgsql + Entity Framework Core 6.0 |
| ORM | EF Core 6.0 + Dapper 2.0 for raw queries |
| Logging | Serilog (console + Grafana Loki) |
| Metrics | prometheus-net |
| Job Scheduling | Quartz 3.9 (SS14.Auth only) |
| Email | MailKit + MimeKit |
| Configuration | YAML via NetEscapades.Configuration.Yaml |
| Frontend | Bootstrap + jQuery (wwwroot/lib) |
| Testing | NUnit 3.13.2 |
| CI/CD | GitHub Actions + Docker + Podman |

## Coding Conventions

### Naming (enforced by .editorconfig)

| Element | Convention | Example |
|---------|-----------|---------|
| Classes/structs/enums | PascalCase | `HomeController`, `SpaceUser` |
| Interfaces | PascalCase with `I` prefix | `IEmailSender` |
| Methods | PascalCase | `OnGetAsync()`, `ConfigureServices()` |
| Properties | PascalCase | `RequestId`, `Configuration` |
| Private fields | `_camelCase` (underscore prefix) | `_logger`, `_dbContext` |
| Private static readonly | PascalCase (no underscore) | `JsonOptions`, `EnumToType` |
| Constants | PascalCase | `PolicyAnyHubAdmin` |
| Local variables | camelCase | `user`, `result`, `verifyResponse` |
| Method parameters | camelCase | `request`, `nameOrEmail` |
| Type parameters | PascalCase with `T` prefix | `T`, `TQuery` |
| Records | PascalCase, sealed | `sealed record AuthenticateRequest(...)` |

### Code Style

- **File-scoped namespaces**: `namespace SS14.Web;` (C# 10+)
- **Allman braces** (opening brace on new line)
- **4-space indentation** (no tabs)
- **`var` everywhere**: used for all local variable declarations
- **Sealed classes** by default on non-inheritance classes
- **Primary constructors** used in newer code (C# 12)
- **Records** for DTOs, request/response models
- **Nullable reference types**: enabled in some projects (`<Nullable>enable</Nullable>`)
- **Expression-bodied members**: accessors/properties use `=>`, methods use block body
- **No `this.` qualification** for fields/properties/methods/events
- **Predefined types**: use `string` not `String`, `int` not `Int32`
- **`using` directives**: outside namespace, System namespaces first
- **Trailing commas** in multiline lists/initializers
- **`@formatter:off` / `@formatter:on`** comments around formatted enums

### File Organization

- One class per file (exceptions: tightly coupled records/enums in same file as controller)
- Razor Pages: `PageName.cshtml` + `PageName.cshtml.cs` (code-behind)
- MVC Views in `Views/{Controller}/` with `Views/Shared/` for layouts/partials
- Areas follow pattern: `Areas/{AreaName}/Pages/`

## Architecture Patterns

### Dependency Injection

- **Constructor injection** is the standard pattern
- Services registered in `Startup.cs` via `ConfigureServices()`
- Shared services registered via `StartupHelpers.AddShared()` in `SS14.Auth.Shared`
- Common lifetimes:
  - DbContexts → **Scoped**
  - UserManager, SignInManager, SessionManager → **Scoped**
  - IEmailSender → **Transient**
  - IRawEmailSender → **Singleton**
  - Background services → `AddHostedService` (Singleton)

### Adding a New Service

```csharp
// 1. Register in Startup.cs (project-specific) or StartupHelpers.cs (shared)
services.AddScoped<MyNewService>();

// 2. Inject via constructor
public class MyController
{
    private readonly MyNewService _myService;

    public MyController(MyNewService myService)
    {
        _myService = myService;
    }
}
```

### MVC Controllers (SS14.Web)

- Inherit from `Controller`
- Use `IActionResult` return type
- Standard route: `{controller=Home}/{action=Index}/{id?}`
- Views are under `Views/{ControllerName}/`

### API Controllers (SS14.Auth, SS14.ServerHub)

- Decorated with `[ApiController]` and `[Route("/api/...")]`
- Inherit from `ControllerBase`
- Return `IActionResult` with HTTP status codes
- Records at bottom of file for request/response DTOs
- Attribute routing for actions: `[HttpGet("ping")]`, `[HttpPost("authenticate")]`

### Razor Pages (SS14.Web)

- Pages in `Areas/{Area}/Pages/` or root `Pages/`
- Code-behind inherits from `PageModel`
- Handler methods: `OnGetAsync()`, `OnPostSaveAsync()`, `OnPostDeleteAsync()`
- Bind properties with `[BindProperty]`, temp messages with `[TempData]`
- Nested `InputModel` class for form binding
- `Page()` to return the page, `RedirectToPage()` for redirects
- `NotFound()` for missing entities

### Configuration

- YAML files: `appsettings.yml`, `appsettings.{Environment}.yml`, `appsettings.Secret.yml`
- Loaded via `builder.AddYamlFile()` in `Program.cs`
- Sections bound via `services.Configure<TOptions>(config.GetSection("SectionName"))`
- Connection strings in `appsettings.Secret.yml` (gitignored)

### Authorization

- Role-based policies defined in `SS14.Web/Startup.cs`:
  - `AuthConstants.PolicyAnyHubAdmin` → SysAdmin OR ServerHubAdmin
  - `AuthConstants.PolicySysAdmin` → SysAdmin only
  - `AuthConstants.PolicyServerHubAdmin` → ServerHubAdmin only
- Area folder authorization via conventions:
  ```csharp
  options.Conventions.AuthorizeAreaFolder("Admin", "/", AuthConstants.PolicyAnyHubAdmin);
  options.Conventions.AuthorizeAreaFolder("Admin", "/Clients", AuthConstants.PolicySysAdmin);
  ```
- Razor Page level: `@inject IAuthorizationService AuthorizationService`

### Entity Framework Core

- PostgreSQL via Npgsql
- DbContexts: `ApplicationDbContext` (auth), `HubDbContext` (server hub)
- Migrations in each DbContext project
- `UseNpgsql(connectionString)` to configure

### Audit Logging

- `AccountLogManager` for logging user account actions
- `AccountLogEntry` records for each log type (see `SpaceUser.cs`)
- Retention configured via `AccountLogRetentionConfiguration`

## Common Patterns

### Returning Success/Error Responses (API)
```csharp
return Ok(response);
return BadRequest();
return NotFound();
return Unauthorized(new ErrorResponse(...));
return UnprocessableEntity("Some error message");
```

### Returning Page Results (Razor Pages)
```csharp
return Page();
return RedirectToPage(new { id });
return NotFound("User does not exist!");
```

### Session Management
```csharp
var (token, expireTime) = await _sessionManager.RegisterNewSession(user, SessionManager.DefaultExpireTime);
await _sessionManager.InvalidateSessions(user);
await _sessionManager.RefreshToken(token);
```

### Transactions
```csharp
await using var tx = await _dbContext.Database.BeginTransactionAsync();
// ... operations ...
await tx.CommitAsync();
```

### Status Messages (TempData)
```csharp
[TempData] public string StatusMessage { get; set; }
StatusMessage = "Changes saved";
// In .cshtml: @if (TempData.ContainsKey("StatusMessage")) { <partial name="_StatusMessage" /> }
```

## Testing

- Framework: NUnit 3.13.2
- Test project: `SS14.Web.Tests` (net8.0)
- Run tests: `dotnet test`
- Tests have `InternalsVisibleTo` access to `SS14.Web`
- Test file example structure:
  ```csharp
  namespace SS14.Web.Tests;
  public class TestSomething
  {
      [Test]
      public void TestFeature()
      {
          // Arrange
          // Act
          // Assert
          Assert.That(result, Is.EqualTo(expected));
      }
  }
  ```

## Build & Run

```bash
# Build all projects
dotnet build

# Run tests
dotnet test

# Build in Release
dotnet build --configuration Release

# Requirements for local development:
# 1. Local PostgreSQL database
# 2. Create appsettings.Secret.yml with connection strings
# 3. Use `dotnet ef migrations script` for SQL schema
# 4. Create SQLite mutex DB and run init_mutex.sql
```

## Docker

Three Dockerfiles:
- `web.Dockerfile` → SS14.Web (base: `aspnet:6.0`, build: `sdk:8.0`)
- `auth.Dockerfile` → SS14.Auth (base: `aspnet:8.0`, build: `sdk:8.0`)
- `hub.Dockerfile` → SS14.ServerHub (base: `aspnet:8.0`, build: `sdk:8.0`)

Build script: `build.sh` (uses docker build)

## Git Workflow

- Branch: `master`
- CI: GitHub Actions on push/PR to master
- CI steps: `dotnet restore` → `dotnet build --configuration Release` → `dotnet test`
- Deployment via `deploy.sh` (Docker save → gzip → SSH → Podman)

## Important Notes

- **Do NOT enable launcher registration endpoints** (commented out in AuthApiController due to spam)
- **Do NOT commit secrets** (connection strings, API keys, signing keys)
- **Do NOT modify README.md** directly (separate from AI instructions)
- **Patreon integration** is optional (disabled if no credentials configured)
- **Security stamp validation interval** is 5 seconds (configured in StartupHelpers.cs)
- **Email is required for account confirmation** (`RequireConfirmedEmail = true`)
- **Passwords have no complexity requirements** (all Require* flags are false)
- Follow existing patterns for new features (check similar files first)
