# Space Station 14 Web Services

**These are backend services hosted by Space Wizards for all of Space Station 14 and Robust. You do not need need to host these yourself in any case (except if you feel like contributing, I guess).**

This repo contains various frontend and backend web services used by **Space Station 14**.

List of the projects in question:

* `SS14.Auth`: Auth API server used by launcher and game servers/clients.
* `SS14.ServerHub`: Public game server listing hub used by launcher.
* `SS14.Web`: Main website for SS14 including account management and SSO authentication.

## Development

### Prerequisites

- [.NET 6.0 SDK](https://dotnet.microsoft.com/download/dotnet/6.0) (for SS14.Web)
- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (for SS14.Auth and SS14.ServerHub)
- [PostgreSQL 12+](https://www.postgresql.org/download/)
- [DB Browser for SQLite](https://sqlitebrowser.org/) (for mutex database)
- A code editor (Visual Studio, Rider, or VS Code)

### Database Setup

#### 1. PostgreSQL Setup

Create a local PostgreSQL database:

```sql
CREATE DATABASE "ss14-web-dev";
CREATE USER "ss14-web-dev" WITH PASSWORD 'dev-password';
GRANT ALL PRIVILEGES ON DATABASE "ss14-web-dev" TO "ss14-web-dev";
```

#### 2. Apply Schema Migrations

Generate and apply the database schema:

```bash
# Navigate to project root
cd D:\.sorcerers\SS14.Web

# Generate migration SQL (if you have dotnet-ef installed)
dotnet ef migrations script --project SS14.Auth.Shared --startup-project SS14.Web --context ApplicationDbContext --output schema.sql

# OR manually apply migrations from SS14.Auth.Shared/Data/Migrations/
```

Run the generated SQL on your PostgreSQL database using pgAdmin or psql.

**Important:** Don't forget to apply the Birthday field migration:
```sql
ALTER TABLE "AspNetUsers" ADD COLUMN "Birthday" timestamp with time zone NOT NULL DEFAULT '1000-01-01 00:00:00+00';
```

#### 3. Mutex Database Setup

Create a SQLite database for the mutex system:

```bash
# Create empty database file
touch mutex.db

# Open with DB Browser for SQLite and run Tools/init_mutex.sql
```

Or use command line:
```bash
sqlite3 mutex.db < Tools/init_mutex.sql
```

### Configuration Files

Create `appsettings.Secret.yml` in each project directory.

#### SS14.Web/appsettings.Secret.yml

```yaml
ConnectionStrings:
  DefaultConnection: "Server=127.0.0.1;Port=5432;Database=ss14-web-dev;User Id=ss14-web-dev;Password=dev-password"
  HubConnection: "Server=127.0.0.1;Port=5432;Database=ss14-web-dev;User Id=ss14-web-dev;Password=dev-password"

Mutex:
  DbPath: 'D:\.sorcerers\SS14.Web\mutex.db'

# VK OAuth (optional - for testing SSO)
Vkontakte:
  ClientId: "your_vk_app_id"
  ClientSecret: "your_vk_app_secret"

# Yandex OAuth (optional - for testing SSO)
Yandex:
  ClientId: "your_yandex_client_id"
  ClientSecret: "your_yandex_client_secret"

# Patreon OAuth (optional - for testing Patreon integration)
Patreon:
  ClientId: "your_patreon_client_id"
  ClientSecret: "your_patreon_client_secret"
  TierMap: {}
  TierNames: {}
```

#### SS14.Auth/appsettings.Secret.yml

```yaml
ConnectionStrings:
  DefaultConnection: "Server=127.0.0.1;Port=5432;Database=ss14-web-dev;User Id=ss14-web-dev;Password=dev-password"

Mutex:
  DbPath: 'D:\.sorcerers\SS14.Web\mutex.db'
```

#### SS14.ServerHub/appsettings.Secret.yml

```yaml
ConnectionStrings:
  HubConnection: "Server=127.0.0.1;Port=5432;Database=ss14-web-dev;User Id=ss14-web-dev;Password=dev-password"
```

### OAuth Provider Setup (Optional)

For full SSO testing, you'll need to create OAuth applications:

#### VK OAuth Setup

1. Go to https://dev.vk.com/
2. Create a new "Standalone application"
3. In Settings → Authorized redirect URI, add: `https://localhost:5001/signin-vkontakte`
4. Copy App ID and Secure key to your config
5. For local development, VK may require HTTPS - use `dotnet dev-certs https --trust`

#### Yandex OAuth Setup

1. Go to https://oauth.yandex.ru/
2. Register a new application
3. Add platform "Web services" with callback: `https://localhost:5001/signin-yandex`
4. Enable "login:email" and "login:birthday" in Data access
5. Copy Client ID and Client secret to your config

### Running the Services

#### Option 1: Run All Services Together

```bash
# Terminal 1 - Main Website (SS14.Web)
cd SS14.Web
dotnet run

# Terminal 2 - Auth API (SS14.Auth)
cd SS14.Auth
dotnet run

# Terminal 3 - Server Hub (SS14.ServerHub)
cd SS14.ServerHub
dotnet run
```

#### Option 2: Visual Studio / Rider

1. Open `SS14.Web.sln`
2. Set multiple startup projects:
   - Right-click solution → Properties → Multiple startup projects
   - Set `SS14.Web`, `SS14.Auth`, and `SS14.ServerHub` to "Start"
3. Press F5 to run all services

### Default URLs

- **SS14.Web**: https://localhost:5001 (main website + account management)
- **SS14.Auth**: https://localhost:5002 (auth API for launcher/game)
- **SS14.ServerHub**: https://localhost:5003 (server listing API)

### Testing the SSO Flow

1. Navigate to `https://localhost:5001`
2. Click "Log in"
3. You should see buttons for VK and Yandex (if configured)
4. Click a provider and authorize
5. On first login, you'll be asked to choose a username (ckey)
6. After registration, you're logged in!

### Testing Without OAuth Providers

If you don't want to set up VK/Yandex OAuth for local testing, you can:

1. Manually create a user in the database:
```sql
INSERT INTO "AspNetUsers" ("Id", "UserName", "NormalizedUserName", "Email", "NormalizedEmail", "EmailConfirmed", "PasswordHash", "SecurityStamp", "CreatedTime", "AdminNotes", "Birthday") 
VALUES (gen_random_uuid(), 'testuser', 'TESTUSER', 'test@example.com', 'TEST@EXAMPLE.COM', true, NULL, gen_random_uuid()::text, now(), '', '1990-01-01');
```

2. Manually create an external login:
```sql
INSERT INTO "AspNetUserLogins" ("LoginProvider", "ProviderKey", "ProviderDisplayName", "UserId")
VALUES ('TestProvider', 'test123', 'Test Provider', (SELECT "Id" FROM "AspNetUsers" WHERE "UserName" = 'testuser'));
```

But this won't let you test the actual OAuth flow.

### Common Issues

**"No encryption key configured"**
- Make sure `Is4SigningKeyPath` is not set in your config (it will use development credentials automatically)

**"Could not connect to database"**
- Check PostgreSQL is running: `pg_isready`
- Verify credentials in `appsettings.Secret.yml`
- Ensure database and user exist

**"Mutex database locked"**
- Delete `mutex.db-shm` and `mutex.db-wal` files
- Make sure only one instance is running

**SSL/HTTPS errors with OAuth**
- Run `dotnet dev-certs https --trust` to trust the development certificate
- VK and Yandex require HTTPS for OAuth callbacks

### Debugging

- Set `ASPNETCORE_ENVIRONMENT=Development` for detailed error pages
- Check logs in console output
- Use browser dev tools to inspect OAuth redirects
- Database queries are logged at `Debug` level in development mode

## Production Deployment

For production deployment instructions, see `MIGRATION.md` and `SSO_CONFIGURATION.md`.
