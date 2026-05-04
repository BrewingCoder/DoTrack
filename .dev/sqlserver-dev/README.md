# SQL Server Dev Rig

Local SQL Server 2022 Developer Edition for multi-provider migrations and integration tests. AIRM5 is ARM64; this image is amd64 and runs under Rosetta — slower first boot but real SQL Server semantics. Bound to `localhost:1433`.

## Run

```sh
docker compose up -d
docker compose ps
```

First boot takes ~30-60s under Rosetta.

## Connection string

```
Server=localhost,1433;Database=dotrack_dev;User Id=sa;Password=D0Track-Dev!;TrustServerCertificate=true
```

Set as env var `DOTRACK_MSSQL_CONNECTION` for the design-time DbContext factory to pick it up.

## Reset

```sh
docker compose down -v
rm -rf data
```
