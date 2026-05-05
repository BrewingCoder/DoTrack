# YouTrack UX Reference Rig

Local YouTrack instance used as the side-by-side UX comparison rig for DoTrack UI work. **YouTrack is the IA gold standard.** Don't start a UI feature without this running alongside the DoTrack dev server.

This is **not** a DoTrack dependency. It's a developer tool. Persistent volumes are gitignored.

## Run

```sh
docker compose up -d
docker compose logs -f   # first boot takes 30-90s
```

Then open <http://localhost:8888> and walk through the configuration wizard.

## Setup wizard notes

- Use **bundled** database (HSQLDB) — this instance is a UX reference, not a real tracker.
- License: Free 10-User license is fine; this instance only has one user (you).
- Create at least one project named `DOT` (DoTrack reference) and populate with sample epics/features/work items/time entries that mirror DoTrack's data shape.

## Listen-port wizard dance (read before first launch)

YouTrack's Configuration Wizard rewrites the container's internal listen-port
to match the **Base URL** you set. The compose file maps `127.0.0.1:8888` on
the host straight to `8888` in the container, so:

1. On a fresh volume, the container initially listens on `8080`. The wizard
   loads at `http://localhost:8888/?wizard_token=...` (the token URL is in
   `docker logs dotrack-yt-ref`) — but **only after** Docker port-forwarding
   maps host 8888 onto whatever the container is listening on.
2. On the wizard's HTTP page, set **Base URL** to `http://localhost:8888`
   and leave **Application Listen Port** at `8080`. Click Next.
3. The wizard validates the Base URL by dialling it from inside the
   container; that fails (different port from internal listener), so a "Base
   URL is Not Available" warning appears. Click **Continue** — the warning
   is expected behind a port mapping or reverse proxy.
4. After the wizard completes, YouTrack rewrites the internal listener to
   `8888` to match the Base URL. The host:8888 → container:8888 mapping in
   `docker-compose.yml` is correct from this point on.

If you bring up a fresh volume (`docker compose down -v`) and the container
is suddenly unreachable at `:8888`, the wizard hasn't run yet — listen-port
is still 8080 inside the container. Either re-walk the wizard, or
temporarily flip the compose mapping back to `8888:8080` to reach it.

## Stop / clean

```sh
docker compose down            # stop, keep data
docker compose down -v         # stop, wipe volumes (full reset)
rm -rf data conf logs backups  # nuclear
```

## Pinning

Image is pinned to `jetbrains/youtrack:2026.1.13162` for reproducibility. Bump when JetBrains ships a UX update worth comparing against.
