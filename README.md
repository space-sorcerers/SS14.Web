# Space Station 14 Web Services

Backend web services used by **Space Station 14**.

## Projects

* `SS14.Auth` — Authentication API used by launchers, game servers and clients.
* `SS14.ServerHub` — Public server listing hub.
* `SS14.Web` — Main website, account management and SSO authentication.

## Development

### Requirements

* .NET 6 SDK (`SS14.Web`)
* .NET 8 SDK (`SS14.Auth`, `SS14.ServerHub`)
* PostgreSQL
* SQLite (mutex database)

### Setup

1. Create a PostgreSQL database.
2. Apply database migrations.
3. Create the required `appsettings.Secret.yml` files.
4. Create the mutex database and run `Tools/init_mutex.sql`.

Most configuration options are documented directly in the configuration files and source code.

## Quick Start

The repository includes helper scripts for building and running services.

### Windows

Build everything:

```bat
Scripts\bat\buildAll.bat
```

Run all services:

```bat
Scripts\bat\runQuickAll.bat
```

Run individual services:

```bat
Scripts\bat\runQuickAuth.bat
Scripts\bat\runQuickHub.bat
Scripts\bat\runQuickWeb.bat
```

Run the fake OAuth/testing server:

```bat
Scripts\bat\runQuickFakeServer.bat
```

### Linux

Build everything:

```bash
Scripts/sh/buildAll.sh
```

Run all services:

```bash
Scripts/sh/runQuickAll.sh
```

Run individual services:

```bash
Scripts/sh/runQuickAuth.sh
Scripts/sh/runQuickHub.sh
Scripts/sh/runQuickWeb.sh
```

Run the fake OAuth/testing server:

```bash
Scripts/sh/runQuickFakeServer.sh
```

## Contributing

Pull requests are welcome.

## License

See `LICENSE` and repository legal documentation for details.
