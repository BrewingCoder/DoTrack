# Dev rig

Local-machine infrastructure for DoTrack development. Not a runtime dependency
of DoTrack itself; this is a developer tool. Persistent volumes are gitignored.

## Layouts

Two ways to run the dev rig — pick one per machine, don't mix.

### Umbrella (recommended for new machines)

One Docker project named `dotrack` containing Postgres + YouTrack. Single
stack in Docker Desktop, single command.

```sh
docker compose -f .dev/docker-compose.yml up -d
docker compose -f .dev/docker-compose.yml down
```

### Fragmented (already in use on existing machines)

Each service is its own compose project. Kept around because dev machines that
already run things this way have data in the bind-mount dirs and shouldn't be
forced to migrate.

```sh
docker compose -f .dev/postgres-dev/docker-compose.yml up -d
docker compose -f .dev/youtrack-ref/docker-compose.yml  up -d
```

The umbrella is just a thin `include:` over these same files — same container
names (`dotrack-pg-dev`, `dotrack-yt-ref`), same bind-mount paths, same data.
Don't run both layouts at once on the same machine: container names collide.

## Services

| Service     | Port              | Purpose                                          | Persists  |
|-------------|-------------------|--------------------------------------------------|-----------|
| Postgres    | `127.0.0.1:5433`  | Dev runtime DB for the API                       | yes       |
| YouTrack    | `127.0.0.1:8888`  | UX reference rig + UI feature-parity comparison  | yes       |
| SQL Server  | `127.0.0.1:1433`  | Ad-hoc SQL Server provider verification only     | yes       |

SQL Server is **not** in the umbrella. Integration tests spin SQL Server up
per-run via Testcontainers; the `.dev/sqlserver-dev/` compose exists for
manual poking when you want a long-lived instance. Run it directly:

```sh
docker compose -f .dev/sqlserver-dev/docker-compose.yml up -d
```

## YouTrack

The YouTrack instance gates UI work — see `youtrack-ref/README.md` for the
setup-wizard walkthrough and the `DOT` reference project expectations.
