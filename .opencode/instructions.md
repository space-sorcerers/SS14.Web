# Opencode Instructions for SS14.Web

## About This Repo

Monorepo for Space Station 14's web backend: three ASP.NET Core microservices (SS14.Web, SS14.Auth, SS14.ServerHub) plus shared libraries.

## Per-Project File Organization

| Project | Target | Type | Key Dir |
|---------|--------|------|---------|
| SS14.Web | net6.0 | MVC + Razor Pages | `Areas/Admin/Pages/`, `Areas/Identity/Pages/`, `Controllers/`, `Views/` |
| SS14.Auth | net8.0 | Web API | `Controllers/`, `Jobs/` (Quartz) |
| SS14.ServerHub | net8.0 | Web API | `Controllers/`, `ServerData/`, `Utility/` |
| SS14.Auth.Shared | net6.0 | Class Lib | `Data/` (entities, DbContext, migrations), `Config/`, `Emails/`, `Sessions/` |
| SS14.ServerHub.Shared | net6.0 | Class Lib | `Data/` (entities, DbContext, migrations) |
| SS14.WebEverythingShared | net6.0 | Class Lib | Root-level utility classes |

## Naming Rules

- **`_camelCase`** for private instance fields
- **PascalCase** for private static readonly, constants, methods, properties, classes
- **`I` prefix** for interfaces
- **`T` prefix** for generic type params
- **`var`** for all local variables

## Code Style

- `namespace SS14.Web;` (file-scoped)
- Allman braces, 4-space indent
- `sealed` classes by default
- Records for DTOs (`sealed record Foo(...)`)
- Primary constructors in new code
- No `this.` qualification
- `@formatter:off` / `@formatter:on` around enum value lists
- Trailing commas in multiline initializers

## Key Patterns

### API Controller
```csharp
[ApiController]
[Route("/api/thing")]
public class ThingController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Get() { ... }
}
```

### Razor Page
```csharp
public class Index : PageModel
{
    [TempData] public string StatusMessage { get; set; }
    [BindProperty] public InputModel Input { get; set; }

    public class InputModel { ... }

    public async Task<IActionResult> OnGetAsync(Guid id) { ... }
    public async Task<IActionResult> OnPostSaveAsync(Guid id) { ... }
}
```

### DI Registration
- Shared services → `StartupHelpers.AddShared()` in `SS14.Auth.Shared`
- Per-project services → respective `Startup.cs`

### DbContexts
- `ApplicationDbContext` in `SS14.Auth.Shared/Data/` (auth)
- `HubDbContext` in `SS14.ServerHub.Shared/Data/` (hub)

### Auth Policies (defined in SS14.Web/Startup.cs)
- `AuthConstants.PolicyAnyHubAdmin` → SysAdmin OR ServerHubAdmin
- `AuthConstants.PolicySysAdmin` → SysAdmin only
- `AuthConstants.PolicyServerHubAdmin` → ServerHubAdmin only

## C# Version Notes

- Projects targeting net6.0 use C# 10 (file-scoped namespaces, but no primary constructors or collection expressions)
- Projects targeting net8.0 (SS14.Auth, SS14.ServerHub) use C# 12
- SS14.Web.Tests targets net8.0 (C# 12)
- Do NOT use C# 12 features in net6.0 projects

## Testing

```bash
dotnet test
```
- NUnit framework
- Tests in `SS14.Web.Tests/`
- `InternalsVisibleTo` from SS14.Web to test project

## Configuration

- YAML config files loaded in `Program.cs`
- Pattern: `builder.AddYamlFile("appsettings.yml", false, true)`
- Secrets in `appsettings.Secret.yml` (gitignored)
- Bind sections: `services.Configure<TOptions>(config.GetSection("SectionName"))`

## What NOT To Do

- Enable launcher registration (spam risk — commented out in AuthApiController)
- Commit secrets/connection strings/keys
- Modify README.md
- Use emojis in code
- Add XML doc comments unless truly necessary
- Change .editorconfig settings
