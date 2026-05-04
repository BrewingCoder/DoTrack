# DoTrack

A self-hosted, OSS issue tracker and project manager designed for small dev teams running T&M engagements with active client/stakeholder involvement.

> Looks like JIRA, weighs like Vikunja, treats clients as first-class participants without per-seat pricing.

**Status:** v0 — scaffolding. Not yet usable.

## Stack

- **Backend:** .NET 10 + EF Core (multi-provider: PostgreSQL / SQL Server / SQLite; MySQL deferred until Pomelo ships EF Core 10)
- **Frontend:** React + shadcn/ui + Tailwind + TipTap + Vaul (PWA)
- **Distribution:** Docker Compose, Helm chart, signed images on GHCR
- **License:** Apache 2.0

## Repository layout

```
src/
  DoTrack.Domain/                       Pure domain model, no deps
  DoTrack.Application/                  Use cases, ports
  DoTrack.Infrastructure/               EF Core, queue, repos (provider-agnostic)
  DoTrack.Migrations.{Postgres,SqlServer,Sqlite}/
  DoTrack.QueryLanguage/                Recursive-descent parser → EF expression trees
  DoTrack.GitProviders.{Abstractions,GitHub,Gitea,Bitbucket}/
  DoTrack.Automation.{Abstractions,N8n}/
  DoTrack.Workers/                      In-process IHostedService runners
  DoTrack.Api/                          ASP.NET Core composition root
tests/
  DoTrack.{Domain,Application,Infrastructure,QueryLanguage,GitProviders,Integration}.Tests/
.dev/
  youtrack-ref/                         Local YouTrack container (UX reference rig)
```

## Build

```sh
dotnet restore
dotnet build
dotnet test
```
