# Postgres Dev Rig

Local Postgres instance used as the dev database for running migrations and the Api locally. Bound to `localhost:5433` (port 5433 to avoid conflict with any system Postgres on 5432).

## Run

```sh
docker compose up -d
docker compose ps
```

## Connection string

```
Host=localhost;Port=5433;Database=dotrack_dev;Username=dotrack;Password=dotrack
```

Set as env var `DOTRACK_PG_CONNECTION` for the design-time DbContext factory to pick it up.

## Reset

```sh
docker compose down -v   # wipes data
rm -rf data
```
