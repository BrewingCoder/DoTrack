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

## Stop / clean

```sh
docker compose down            # stop, keep data
docker compose down -v         # stop, wipe volumes (full reset)
rm -rf data conf logs backups  # nuclear
```

## Pinning

Image is pinned to `jetbrains/youtrack:2026.1.13162` for reproducibility. Bump when JetBrains ships a UX update worth comparing against.
