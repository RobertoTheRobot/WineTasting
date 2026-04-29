# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

Small Blazor Web App for hosting a wine tasting event. Currently scaffolded from the default `dotnet new blazor` template — feature code has not been added yet.

## Stack

- **.NET 10** (`net10.0`) — `Microsoft.NET.Sdk.Web`
- **Blazor Web App** with **Interactive Server** render mode only (no WebAssembly)
- Solution file is `.slnx` format (`WineTasting.slnx`)
- Nullable reference types and implicit usings enabled
- `BlazorDisableThrowNavigationException` is set — navigation does not throw `NavigationException`; treat it as a normal flow-control mechanism

## Common commands

```bash
dotnet build                          # build
dotnet run                            # run app on http://localhost:5999 (http profile)
dotnet watch                          # run with hot reload
dotnet run --launch-profile http      # explicit profile selection
```

There are no tests, linters, or CI workflows configured (`.github/workflows/` is empty).

## Architecture

Standard Blazor Server layout. The render-mode wiring is the part worth knowing:

- `Program.cs` registers `AddRazorComponents().AddInteractiveServerComponents()` and maps `App` with `AddInteractiveServerRenderMode()`. New components default to static SSR; opt into interactivity with `@rendermode InteractiveServer` (or apply it at the route level in `Routes.razor`).
- `Components/App.razor` is the HTML document root. It uses `MapStaticAssets` (.NET 9+ asset pipeline) — reference assets via `@Assets["..."]` rather than raw paths so fingerprinting works.
- `Components/Routes.razor` handles routing; pages live in `Components/Pages/`; layout in `Components/Layout/`.
- `Components/_Imports.razor` provides global `@using` directives — add namespaces here rather than per-component when they're broadly used.
- Error handling: `/Error` for exceptions (production), `/not-found` for 404s, both via re-execution middleware.
