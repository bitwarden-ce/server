# ByteGarden Server

<!-- Find a logo -->

[![Docker pulls](https://img.shields.io/docker/pulls/bytegarden/api.svg)](https://hub.docker.com/u/bytegarden)
[![Docker pulls](https://img.shields.io/docker/stars/bytegarden/api.svg)](https://hub.docker.com/u/bytegarden)
[![Mattermost](https://img.shields.io/badge/mattermost-join%20char-orange.svg)](https://most.kokakiwi.net/signup_user_complete/?id=1atxn5ydk3g8pe4omy1akmhoaw)

---

The ByteGarden Server project contains the APIs, database, and other core infrastructure items needed for the "backend" of all bytegarden client applications.

The server project is written in C# using .NET Core with ASP.NET Core. The database is written in T-SQL/SQL Server. The codebase can be developed, built, run, and deployed cross-platform on Windows, macOS, and Linux distributions.

## Build/Run

### Requirements

- [.NET Core 2.x SDK](https://www.microsoft.com/net/download/core)
- [SQL Server 2017](https://docs.microsoft.com/en-us/sql/index) (if running directly on host)

*These dependencies are free to use.*

### Recommended Development Tooling

- [Visual Studio](https://www.visualstudio.com/vs/) (Windows and macOS)
- [Visual Studio Code](https://code.visualstudio.com/) (other)

*These tools are free to use.*

### API

```
cd src/Api
dotnet restore
dotnet build
dotnet run
```

visit http://localhost:5000/alive

### Identity

```
cd src/Identity
dotnet restore
dotnet build
dotnet run
```

visit http://localhost:33657/.well-known/openid-configuration

## Deploy

<p align="center">
  <a href="https://hub.docker.com/u/bytegarden/" target="_blank">
    <img src="https://i.imgur.com/SZc8JnH.png" alt="docker" />
  </a>
</p>

You can deploy ByteGarden using Docker containers on Windows, macOS, and Linux distributions. Use the provided PowerShell and Bash scripts to get started quickly. Find all of the ByteGarden images on [Docker Hub](https://hub.docker.com/u/bytegarden/).

### Requirements

- [Docker](https://www.docker.com/community-edition#/download)
- [Docker Compose](https://docs.docker.com/compose/install/) (already included with some Docker installations)

*These dependencies are free to use.*