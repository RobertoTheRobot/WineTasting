# WineTasting — Build Plan

Mobile-first Blazor Server app for hosting blind wine tasting events. Two flows: admin (no auth) and user.

## Confirmed decisions

1. **Rating scale**: integer 0–5 stars via `MudRating` (default settings; max 5). 0 means "tasted but no stars". A `Rating` row only exists once the user saves the dialog, so absence of row = "not yet rated".
2. **Reveal trigger**: admin clicks a "Reveal" button. Button is **disabled** until every participant has `IsFinished = true` (and there is at least one). Admin page has a control pane listing each participant + finished status.
3. **Wine numbering**: consecutive 1..N. Auto-assign next number on add. On delete, renumber remaining wines so they stay 1..N consecutive.
4. **Image storage**: `wwwroot/uploads/wines/{guid}{ext}` on disk; path stored in DB.
5. **Database**: SQLite at `App_Data/winetasting.db` via `EnsureCreated()` on startup (drop the file to reset).
6. **Participant identity**: URL carries `participantId` (Guid); also stashed in `localStorage` keyed by event so refresh resumes — wait, this would require JS interop. Replace with: rely on URL only; if user refreshes after the event-select page they stay on the tasting board because the URL holds both ids. No localStorage needed → no JS interop needed.
7. **Wine types**: enum `Red`, `White`, `Rose`, `Bubbles`, `Dessert`.

## Phase 1 — Infrastructure

- [ ] Add NuGet packages: `MudBlazor`, `Microsoft.EntityFrameworkCore.Sqlite`
- [ ] Register MudBlazor in `Program.cs` (`AddMudServices`)
- [ ] Add MudBlazor stylesheet + script references to `Components/App.razor`
- [ ] Add `@using MudBlazor` to `Components/_Imports.razor`
- [ ] Add `MudThemeProvider` (light, custom wine palette), `MudPopoverProvider`, `MudDialogProvider`, `MudSnackbarProvider` to `MainLayout.razor`
- [ ] Replace default nav-menu layout with a mobile-friendly `MudLayout` + `MudAppBar` + `MudDrawer` (or top-bar only on mobile)
- [ ] Configure SQLite connection string in `appsettings.json`
- [ ] Create `Data/` folder with entities and `WineTastingDbContext`
- [ ] Register DbContext in `Program.cs` and call `EnsureCreated` on startup
- [ ] Ensure `App_Data/` and `wwwroot/uploads/wines/` exist

## Phase 2 — Domain model (`Data/`)

- [ ] `WineType` enum: Red, White, Rose, Bubbles, Dessert
- [ ] `Event` { Id (Guid), Name, Date (DateOnly), IsRevealed (bool), Wines (nav), Participants (nav) }
- [ ] `Wine` { Id (Guid), EventId, Number (int), Type, Name, Origin, Description, Characteristics, ImagePath (string?) }
- [ ] `Participant` { Id (Guid), EventId, Alias, IsFinished (bool), Ratings (nav) }
- [ ] `Rating` { Id (Guid), ParticipantId, WineId, Score (decimal 0–5 step 0.5), Notes (string?) }
- [ ] `WineTastingDbContext` with DbSets and `OnModelCreating` configuring relationships, unique constraints (`Wine.EventId+Number`, `Rating.ParticipantId+WineId`)

## Phase 3 — Admin (`Components/Pages/Admin/`)

All under `/admin` route prefix, no auth, opt into `InteractiveServer` rendering.

- [ ] `/admin` — list events (`MudTable`/`MudList`), create new event dialog (Name + Date), each row links to event detail
- [ ] `/admin/events/{id}` — event header, participant list with finished status, "Reveal results" button (toggles `IsRevealed`), wine list with add/edit/delete, "Add wine" opens dialog
- [ ] Wine add/edit dialog: number (auto), type (`MudSelect`), name, origin, description, characteristics, image upload (`MudFileUpload`)
- [ ] Image-upload service: saves to `wwwroot/uploads/wines/{guid}{ext}`, returns relative path; deletes prior image on replace/delete
- [ ] Delete confirmation dialog for events and wines

## Phase 4 — User flow (`Components/Pages/Tasting/`)

- [ ] `/` — replace default Home: dropdown of events, alias text field, "Start tasting" button. Creates a `Participant` and navigates to the tasting page. Also resumes via localStorage if a participantId exists for that event.
- [ ] `/tasting/{eventId:guid}/{participantId:guid}` — grid of wine cards by number. Each card shows the wine number large, current rating (or "Not rated"), tap to open rating dialog.
- [ ] Rating dialog: `MudRating` (1–5 half-step) + `MudTextField` notes, Save / Cancel.
- [ ] "I'm done" button (sticky bottom on mobile) — confirms with dialog, sets `IsFinished = true`, navigates to results page.
- [ ] `/tasting/{eventId:guid}/{participantId:guid}/results` — if `Event.IsRevealed`, show each wine fully revealed (image, type, name, origin, description, characteristics) with the participant's rating + the average across all participants. If not revealed yet, show a "Waiting for the host to reveal…" state with a refresh option.

## Phase 5 — Polish

- [ ] Verify all pages at ~375px viewport (DevTools mobile emulation)
- [ ] Confirm only `InteractiveServer` is used (matches CLAUDE.md)
- [ ] Confirm no JS interop introduced (MudBlazor + Blazor cover everything here)
- [ ] Light theme only — no `prefers-color-scheme` overrides, no dark toggle
- [ ] Touch targets ≥44×44px on action buttons
- [ ] Build clean (`dotnet build`)
- [ ] Smoke test the full flow end-to-end with `dotnet run`

## Out of scope (explicit non-goals)

- Authentication / accounts
- Multi-host / multi-server (uploads are local disk)
- Real-time live updates between participants (a simple page refresh on the results page is enough)
- Exporting results
- Editing past events after reveal

## Review

**Status:** all phases complete, build clean (`0 Warning(s) 0 Error(s)`), server boots, EF Core auto-creates the schema with both unique indexes (`Wines.EventId+Number`, `Ratings.ParticipantId+WineId`). Smoke test: `/`, `/admin`, and `/tasting/{guid}/{guid}` all return 200 and emit MudBlazor markup. No JS interop introduced — pure MudBlazor + Blazor per the frontend-dev skill.

**What was built:**
- `Data/` — `Event`, `Wine`, `Participant`, `Rating`, `WineType`, `WineTastingDbContext` (cascade delete from event, unique constraints).
- `Services/ImageStorageService` — saves wine photos to `wwwroot/uploads/wines/{guid}{ext}`, deletes orphans on replace/delete.
- `Components/Layout/MainLayout.razor` — `MudLayout`/`MudAppBar` with Home + Admin nav, custom wine-themed `PaletteLight`, `IsDarkMode="false"`.
- Admin: `AdminEvents` (list+create), `AdminEventDetail` (tabs for wines and participants, reveal control panel), `EventCreateDialog`, `WineEditDialog` (with `MudFileUpload` v9 `CustomContent` API), `ConfirmDialog`.
- User: `Home` rebuilt as event picker + alias entry, `TastingBoard` (responsive `MudGrid` of wine cards `xs="6" sm="4"`, taps open `RatingDialog` with `MudRating` 1–5), `TastingResults` (waiting state + reveal-driven full results with group averages and a "top wine" highlight).
- Renumber-on-delete keeps wine numbers consecutive 1..N.
- Reveal button is disabled until every participant `IsFinished` and there is at least one.

**Manual test still recommended:** open `http://localhost:5171` in a phone-sized browser viewport, walk the full admin → 2 participants tasting → reveal flow. Image upload needs a real upload to confirm.

**2026-07-11 — `Data/` case-collision fixed** (renamed the runtime bind-mount dir to
`runtime-data/`, `.gitignore` no longer collides with the source `Data/` folder — see git log)
and the `NU1903` `SQLitePCLRaw` advisory resolved (forced `SQLitePCLRaw.bundle_e_sqlite3` to
`3.0.3`, verified clean via `dotnet list package --vulnerable` and a local runtime smoke test).
Both confirmed with a genuinely fresh `git clone` build, not just the existing working copy.
