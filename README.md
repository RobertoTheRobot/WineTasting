# WineTasting

A small Blazor Web App for hosting a wine tasting party. The host defines an event and its wines; guests join with an alias, rate each wine across five dimensions, and the host reveals the results — at which point everyone can see how their palate compared to the table.

No accounts, no auth, no cloud — a single `dotnet run` and the SQLite file in `App_Data/` is everything.

## Features

- **Host an event** with a list of wines: number, type, name, origin, optional price (€), description, characteristics, and up to **two photos per bottle** (front + back of the label).
- **Guests join** by picking an event from a dropdown and entering an alias — no signup, no login. Everything keys off the participant's URL.
- **Rate each wine** across five dimensions on a 0–5 scale, with optional free-text notes:
  - **Aroma** (the nose) · **Body** (the feel) · **Flavor** (the taste) · **Smoothness** (the finish) · **Overall enjoyability** (would you buy it?)
  - Each dimension has a one-line tip in the rating dialog and in a collapsible *How to rate* guide above the wine grid, aimed at amateur drinkers.
- **Guess the origin** of each wine while you rate. At the reveal, every guess is marked ✓ or ✗ next to the actual origin (lenient match — "France" matches "Bordeaux, France").
- **Two-stage reveal** — guests rate privately; the host reveals when everyone is finished. Reveal can be toggled off again.
- **Results, designed to start conversations:**
  - **Ranked cards** — wines sorted by group **Overall** (the only dimension that drives the final score), with rank medals (🏆 1st, 🎖 2nd/3rd) and the group's average ★ rating.
  - **Talking points strip** — only shows points whose threshold is met:
    - *Most divisive* / *Everyone agreed* (smallest vs largest spread on Overall).
    - *Hot take* — your biggest gap vs the group.
    - *Champagne taste / Budget palate / Price-blind palate* — Pearson correlation between your ratings and price.
    - *Best value* — best stars-per-€.
    - *Price vs. taste* — did the group rate the priciest third higher than the cheapest?
    - *Origin sleuth* — "you got X of Y origins right".
  - **You vs Group** per wine — every dimension shown side-by-side with a coloured delta chip (green if you rated higher, amber if lower, neutral within ½★).
  - **See all votes** drilldown — every participant's alias, full 5-dimension breakdown, origin guess (✓/✗), and notes per wine. Your row is highlighted.
- **Admin: share a lost link** — every participant row has a *Share* button that copies their personal `/tasting/...` URL, in case a guest loses theirs.
- **Admin danger zone** per event — *Reset votes* (clears ratings, unfinishes participants, hides results) or *Delete event* (cascades wines, participants, ratings, and disk-cleans uploaded images). Both gated by a confirmation dialog.

## Stack

- **.NET 10** · Blazor Web App with Interactive Server render mode (no WebAssembly).
- **MudBlazor 9** for all UI. Light theme, mobile-first.
- **EF Core 10 + SQLite**. Schema is created via `EnsureCreated()` and evolves via additive `ALTER TABLE` guards in `Program.cs` — no EF migrations to manage.
- **Serilog** with Console + Seq sinks (Seq is optional).
- **.NET SDK container publishing** for Docker images.

## Prerequisites

- **.NET 10 SDK** — [download](https://dotnet.microsoft.com/download/dotnet/10.0). Verify with `dotnet --info` (look for "10.0.x").
- A modern browser (Chrome, Edge, Firefox, Safari).
- *Optional, only for containerized deployment:* Docker Desktop or Docker Engine + Compose v2.

That's it — SQLite is bundled, no DB server to install, no Node, no npm.

## Quick start (local dev)

```bash
git clone <this-repo>
cd WineTasting
dotnet run
```

On first run the SQLite database (`App_Data/winetasting.db`) and the upload directory (`wwwroot/uploads/wines/`) are created automatically.

The app listens on **http://localhost:5999** in Development.

- `/` — guest entry (event picker + alias).
- `/admin` — host's event list (the link is intentionally not in the top nav; type the URL).

For hot-reload while you edit Razor/CSS:

```bash
dotnet watch
```

Other useful commands:

```bash
dotnet build                          # build
dotnet build -c Release               # release build
dotnet run --launch-profile http      # explicit profile selection
```

There are no tests, linters, or CI workflows configured.

## Hosting a tasting (the actual flow)

1. **Host:** open `/admin` → *New event* → name + date.
2. **Host:** open the event → *Add wine* per bottle. Number is auto-incremented; type, origin, price, description, characteristics, and up to two photos are optional.
3. **Share** the home-page URL (just `/`) with guests — anyone on the same network can reach it.
4. **Guests:** pick the event from the dropdown, enter an alias, *Start tasting*. Tap each wine card to rate the five dimensions and (optionally) guess the origin. Tap *I'm done* when finished.
5. **Host:** when the participants panel shows everyone finished, hit *Reveal*.
6. Everyone sees the ranked results, the talking-points strip, and can drill into per-wine *See all votes*.

If a guest loses their tasting URL, the host can recover it: open the event → *Participants* tab → tap the link icon next to the guest's name → *Copy link*.

If something went wrong (test data, mid-tasting do-over) the host's *Danger zone* on the event page exposes:

- **Reset votes** — keeps wines and aliases, deletes all ratings, unfinishes participants, hides results.
- **Delete event** — removes the event and everything attached to it.

Both are irreversible.

## Routes

| Route | Who | Purpose |
| --- | --- | --- |
| `/` | Guests | Pick an event, enter an alias, start tasting. |
| `/admin` | Host | Event list. Create new events. |
| `/admin/events/{id}` | Host | Manage wines, view participants, reveal / hide / reset / delete, copy participant share-links. |
| `/tasting/{eventId}/{participantId}` | Guest | Rating board for the event. |
| `/tasting/{eventId}/{participantId}/results` | Guest | Live waiting room → results page once revealed. |

## Identity model

The `participantId` GUID embedded in the tasting URL **is** the guest's identity — there's no cookie, password, or session. Anyone with a participant URL can rate as that participant. Guests refreshing or reconnecting keep the same identity as long as they have the URL. Use the admin *Share link* feature if a guest loses theirs.

## Configuration

### `appsettings.json`

- `ConnectionStrings:WineTastingDb` — defaults to `Data Source=App_Data/winetasting.db` (relative to the working directory). Change if you want the SQLite file somewhere else.
- `Serilog:WriteTo` — Console at index 0, Seq at index 1. The committed Seq config has placeholder values (`http://localhost:5341/`, empty `apiKey`).

### Environment variables

- `ASPNETCORE_ENVIRONMENT` — `Development` (default for `dotnet run`) or `Production`. Production turns on the `/Error` exception handler.
- `ASPNETCORE_HTTP_PORTS` — port the Kestrel server listens on (the container image defaults to `8080`).
- `ConnectionStrings__WineTastingDb` — overrides the appsettings entry. Useful for pointing at a mounted volume in containers.
- `Serilog__WriteTo__1__Args__serverUrl` and `Serilog__WriteTo__1__Args__apiKey` — override the Seq sink at runtime. Leave blank to disable Seq (Console keeps logging).

### Uploads

Wine cover images are saved to `wwwroot/uploads/wines/<guid>.<ext>` by `ImageStorageService`. Allowed: `.jpg .jpeg .png .webp .gif`, max 5 MB each. Up to two per wine. Files are served as static files at `/uploads/wines/...`.

In containerized deployments, mount a host directory at `/app/wwwroot/uploads/wines` so uploaded photos survive container rebuilds (see `compose.yaml`).

### Schema changes

The app calls `db.Database.EnsureCreated()` on startup, then runs additive `ALTER TABLE … ADD COLUMN` statements (guarded against the `duplicate column` error) for any columns added since v1. See `Program.cs` for the current list (`Wines.ImagePath2`, `Wines.Price`, `Ratings.GuessedOrigin`).

This means **upgrading an existing database is automatic** for additive changes. For non-additive schema changes (renaming or dropping columns, changing types), delete the SQLite file (`App_Data/winetasting.db` plus the `-shm` and `-wal` sidecars) and the schema is recreated on next start. There's no migration tooling.

## Project layout

```
Components/
  App.razor                      HTML document root, MudBlazor stylesheet/script wiring
  Routes.razor                   Router with global @rendermode InteractiveServer
  _Imports.razor                 Global @using directives
  Layout/
    MainLayout.razor (.css)      MudLayout, theme (light), dialog/snackbar/popover providers
    ReconnectModal.razor (+ .css, .js)  SignalR reconnect UI
  Pages/
    Home.razor                   Event picker + alias form
    Error.razor / NotFound.razor
    Admin/
      AdminEvents.razor          Event list + create dialog
      AdminEventDetail.razor     Wines, participants, share-link, reveal, danger zone
      EventCreateDialog.razor
      WineEditDialog.razor       Form + image upload (two slots)
      ShareLinkDialog.razor      Copy a participant URL to clipboard
      ConfirmDialog.razor        Reused for any destructive confirm
    Tasting/
      TastingBoard.razor         Wine grid, rating guide, finish flow
      RatingDialog.razor         Five-dimension rating + notes + origin guess
      RatingDimensions.cs        Dimension metadata + Rating.Get(dim) helper
      TastingResults.razor       Ranked cards, talking points, per-wine comparison, all-votes drilldown
      WineDetailDialog.razor     Photos + full description (opened from results)
Data/
  Event.cs · Wine.cs · Participant.cs · Rating.cs · WineType.cs
  WineTastingDbContext.cs        Cascade-delete relationships set up here
Services/
  ImageStorageService.cs         Wine cover upload + cleanup under wwwroot/uploads/wines
Program.cs                       DI, EnsureCreated + additive ALTER TABLE migrations, static asset wiring
Properties/
  launchSettings.json            Dev port 5999 (gitignored: PublishProfiles/*.pubxml)
appsettings.json                 Connection string + Serilog config
compose.yaml                     Docker Compose for self-hosted deployment
```

### Data model (one event)

```
Event ──┬─< Wine ──< Rating
        └─< Participant ──< Rating
```

`Rating` carries five `int` scores (`Aroma`, `Body`, `Flavor`, `Smoothness`, `Overall`), optional `Notes`, and an optional `GuessedOrigin`. Unique index on `(ParticipantId, WineId)` ensures one rating per guest per wine. Deleting an `Event` cascades to wines, participants, and ratings.

## Deployment

The committed `compose.yaml` runs the container from a **locally-loaded image tarball** so the repo works without a container registry. If you have one, see *Optional: publish to a registry* below.

### Self-hosted via Docker Compose (no registry)

On your dev machine:

```bash
# 1. Build the container image via the .NET SDK
dotnet publish /t:PublishContainer -c Release

# 2. Export it as a tarball
docker save winetastingevent:latest -o winetasting.tar.gz
```

Copy `winetasting.tar.gz` and `compose.yaml` to the deployment host, then:

```bash
# 3. Load the image into the host's Docker daemon
docker load -i winetasting.tar.gz

# 4. Start the container
docker compose up -d
```

The container exposes port `8080` internally; `compose.yaml` maps host port `5999` to it.

### Bind mounts

| Host path | Container path | Persists |
| --- | --- | --- |
| `./data` | `/app/App_Data` | SQLite database + WAL/shm sidecars. |
| `./uploads` | `/app/wwwroot/uploads/wines` | Uploaded wine cover images. |

The `app` user inside the SDK-built container can't write to root-owned bind mounts, so the container runs as `user: "0:0"` — fine for a single-process homelab app.

### `.env` (gitignored)

For host-specific config:

```env
WINETASTING_TAG=latest
SEQ_URL=http://your-seq-host:5341
SEQ_API_KEY=...
```

`compose.yaml` reads these to set the image tag and to override the Seq sink via `Serilog__WriteTo__1__Args__*` env vars. All three are optional.

### Reverse proxy

Once the container is up, point your DNS or a reverse proxy (Caddy, nginx, Traefik, YARP) at the host on port `5999`.

### Optional: publish to a registry

If you want to push instead of carrying tarballs around:

```bash
dotnet publish /t:PublishContainer \
  -p:ContainerRegistry=your-registry.example.com \
  -p:ContainerRepository=winetasting \
  -p:ContainerImageTag=latest
```

The csproj sets `<ContainerPublishMethod>Docker</ContainerPublishMethod>`, which makes publish go through the local Docker daemon (`docker push`) — this works against plain-HTTP registries that the SDK's pure-managed registry client rejects.

Then in `compose.yaml`, replace the `image:` line with `your-registry.example.com/winetasting:${WINETASTING_TAG:-latest}` and remove `pull_policy: never`.

`Properties/PublishProfiles/` is gitignored (publish profiles are user- and host-specific). Create one locally if you use Visual Studio's *Publish* dialog.

## License

[MIT](./LICENSE) — fork it freely, host your own tastings.
