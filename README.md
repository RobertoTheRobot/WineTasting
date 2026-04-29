# WineTasting

A small Blazor Web App for hosting a wine tasting party. The host defines an event and its wines; guests join with an alias, rate each wine across five dimensions, and the host reveals the results — at which point everyone can see how their palate compared to the table.

## What it does

- **Host an event** with a list of wines (number, type, name, origin, tasting notes, optional cover image).
- **Guests join** by picking an event and entering an alias — no accounts, no auth.
- **Rate each wine** across five dimensions, each on a 0–5 scale, with optional free-text notes:
  - **Aroma** (the nose) · **Body** (the feel) · **Flavor** (the taste) · **Smoothness** (the finish) · **Overall enjoyability** (would you buy it?)
- **Inline rating tips** — each dimension has a one-line explanation in the rating dialog and in a collapsible *How to rate* guide above the wine grid, aimed at amateur drinkers.
- **Two-stage reveal** — guests rate privately; the host reveals when everyone is finished. Reveal can be toggled off again if you change your mind.
- **Results, designed to start conversations:**
  - **Talking points** strip — *most divisive*, *hot take* (your biggest gap vs the group), *everyone agreed*. Each point only shows when its threshold is met.
  - **Top wine** based on group-average overall enjoyability.
  - **You vs Group** per wine — every dimension shown side-by-side with a coloured delta chip (green if you rated higher, amber if lower, neutral within ½★).
  - **See all votes** drilldown — every participant's alias, full 5-dimension breakdown, and notes per wine. Your row is highlighted.
- **Admin danger zone** per event — reset votes (clears all ratings, unfinishes participants, hides results) or delete the event and everything in it.

## Stack

- **.NET 10** · Blazor Web App with Interactive Server render mode (no WebAssembly).
- **MudBlazor 9** for all UI.
- **EF Core 10 + SQLite** with `EnsureCreated()` (no migrations — see *Schema changes* below).
- **Serilog** with Console + Seq sinks.
- **.NET SDK container publishing** (`<ContainerPublishMethod>Docker</ContainerPublishMethod>`) for image builds.

## Quick start

```bash
git clone <this-repo>
cd WineTasting
dotnet run
```

The app listens on **http://localhost:5999** in Development. The home page is the guest entry; admin lives at `/admin` (the link is intentionally not in the top nav — type the URL).

## Routes

| Route | Who | Purpose |
| --- | --- | --- |
| `/` | Guests | Pick an event, enter an alias, start tasting. |
| `/admin` | Host | Event list. Create new events. |
| `/admin/events/{id}` | Host | Manage wines, view participants, reveal / hide / reset / delete. |
| `/tasting/{eventId}/{participantId}` | Guest | Rating board for the event. |
| `/tasting/{eventId}/{participantId}/results` | Guest | Live waiting room → results page once revealed. |

## Project layout

```
Components/
  App.razor                    HTML document root, MudBlazor stylesheet/script wiring
  Routes.razor                 Router with global @rendermode InteractiveServer
  Layout/MainLayout.razor      MudLayout + theme + dialog/snackbar/popover providers
  Pages/
    Home.razor                 Event picker + alias form
    Admin/
      AdminEvents.razor        Event list + create dialog
      AdminEventDetail.razor   Wines, participants, reveal, danger zone
      EventCreateDialog.razor
      WineEditDialog.razor     Form + image upload
      ConfirmDialog.razor      Reused for any destructive confirm
    Tasting/
      TastingBoard.razor       Wine grid, rating guide, finish flow
      RatingDialog.razor       Five-dimension rating + notes
      RatingDimensions.cs      Dimension metadata + Rating.Get(dim) helper
      TastingResults.razor     Talking points, top wine, per-wine comparison, all-votes drilldown
Data/
  Event.cs · Wine.cs · Participant.cs · Rating.cs · WineType.cs
  WineTastingDbContext.cs      Cascade-delete relationships set up here
Services/
  ImageStorageService.cs       Wine cover upload + cleanup under wwwroot/uploads/wines
Program.cs                     DI, EnsureCreated, static asset wiring
```

### Data model (one event)

```
Event ──┬─< Wine ──< Rating
        └─< Participant ──< Rating
```

`Rating` carries five `int` scores (`Aroma`, `Body`, `Flavor`, `Smoothness`, `Overall`) plus optional `Notes`. A unique index on `(ParticipantId, WineId)` ensures one rating per guest per wine. Deleting an `Event` cascades to wines, participants, and ratings.

## Configuration

`appsettings.json`:

- `ConnectionStrings:WineTastingDb` — defaults to `Data Source=App_Data/winetasting.db` (relative to the working directory).
- `Serilog:WriteTo` — console at index 0, Seq at index 1. Override the Seq sink at runtime via `Serilog__WriteTo__1__Args__serverUrl` and `…apiKey` env vars.

`Properties/launchSettings.json` pins the Development port to `5999`.

### Uploads

Wine cover images are saved to `wwwroot/uploads/wines/<guid>.<ext>` by `ImageStorageService`. Allowed: `.jpg .jpeg .png .webp .gif`, max 5 MB. Files are served as static files at `/uploads/wines/...`.

### Schema changes

The app uses `db.Database.EnsureCreated()` — no EF migrations. If the entity model changes, **delete the SQLite file** (`App_Data/winetasting.db` plus the `-shm` and `-wal` sidecars) so the schema is recreated on next start. There is no production data flow that survives schema changes.

## Deployment

Container publish + Docker Compose, mirroring the HomeYarp deployment pattern.

### Publish the image

From Visual Studio: use the `http___registry.perezfaulks.com.pubxml` profile.

From the CLI:

```bash
dotnet publish /t:PublishContainer \
  -p:ContainerRegistry=registry.perezfaulks.com \
  -p:ContainerRepository=winetasting \
  -p:ContainerImageTag=latest
```

The pubxml pins `<ContainerImageTag>latest</ContainerImageTag>` so `compose.yaml`'s default tag resolves. The csproj's `<ContainerPublishMethod>Docker</ContainerPublishMethod>` makes publish go through the local Docker daemon (`docker push`) rather than the SDK's pure-managed registry client — this is what works against the homelab's plain-HTTP registry.

### compose.yaml + .env

`compose.yaml` (committed) pulls `registry.perezfaulks.com/winetasting:${WINETASTING_TAG:-latest}` and runs as root so the bind mounts are writable.

| Bind mount | Persists |
| --- | --- |
| `./data` → `/app/App_Data` | SQLite database (+ WAL/shm sidecars). |
| `./uploads` → `/app/wwwroot/uploads/wines` | Uploaded wine cover images. |

Host port `5999` → container `8080`.

`.env` (gitignored) holds host-specific values:

```env
WINETASTING_TAG=latest
SEQ_URL=http://192.168.1.2:5341
SEQ_API_KEY=…
```

### Bring it up

```bash
docker compose pull
docker compose up -d
```

Then point your DNS or a reverse proxy at the host on port `5999`.

## Hosting a tasting (guide)

1. **Host:** `/admin` → *New event* → name + date.
2. **Host:** open the event → *Add wine* per bottle. Number is auto-incremented; type, origin, description, characteristics, and a cover image are optional.
3. **Share** the home page URL with guests.
4. **Guests:** pick the event from the dropdown, enter an alias, rate each wine across the five dimensions, leave notes if they want, tap *I'm done*.
5. **Host:** when the participants panel shows everyone finished, hit *Reveal*.
6. Everyone sees their results, the talking points strip, and can drill into per-wine *See all votes*. Use it as conversation fuel.

If something went wrong (test data, mid-tasting do-over) the host's *Danger zone* on the event page exposes:

- **Reset votes** — keeps wines and aliases, deletes all ratings, unfinishes participants, hides results.
- **Delete event** — removes the event and everything attached to it (cascades wines, participants, ratings; cleans up uploaded images from disk).

Both are gated by a confirmation dialog and are irreversible.
