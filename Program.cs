using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using Serilog;
using WineTasting.Components;
using WineTasting.Data;
using WineTasting.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddSerilog((services, config) => config
    .ReadFrom.Configuration(builder.Configuration)
    .ReadFrom.Services(services));

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

builder.Services.AddDbContextFactory<WineTastingDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("WineTastingDb")
        ?? "Data Source=App_Data/winetasting.db"));

builder.Services.AddScoped<ImageStorageService>();

// Reverse-proxy (HomeYarp) forwarded headers — preserve original scheme/host/IP.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor
        | ForwardedHeaders.XForwardedProto
        | ForwardedHeaders.XForwardedHost;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});

// Antiforgery cookie carries Secure whenever the request is HTTPS — which is what
// UseForwardedHeaders reports for every public request behind HomeYarp. SameAsRequest, not
// Always: Always throws on plain-HTTP requests (local dev, the deploy smoke check on :5999).
builder.Services.AddAntiforgery(options => options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest);

var app = builder.Build();

Directory.CreateDirectory(Path.Combine(app.Environment.ContentRootPath, "App_Data"));
Directory.CreateDirectory(Path.Combine(app.Environment.WebRootPath, "uploads", "wines"));

using (var scope = app.Services.CreateScope())
{
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<WineTastingDbContext>>();
    using var db = factory.CreateDbContext();
    db.Database.EnsureCreated();

    // EnsureCreated does not add new columns to existing databases, so guard the
    // ImagePath2 column for users upgrading from the single-image schema. SQLite
    // does not support IF NOT EXISTS on ADD COLUMN; swallow the duplicate-column error.
    try
    {
        db.Database.ExecuteSqlRaw("ALTER TABLE Wines ADD COLUMN ImagePath2 TEXT NULL;");
    }
    catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase))
    {
        // Column already present — nothing to do.
    }

    try
    {
        db.Database.ExecuteSqlRaw("ALTER TABLE Wines ADD COLUMN Price TEXT NULL;");
    }
    catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase))
    {
        // Column already present — nothing to do.
    }

    try
    {
        db.Database.ExecuteSqlRaw("ALTER TABLE Ratings ADD COLUMN GuessedOrigin TEXT NULL;");
    }
    catch (Microsoft.Data.Sqlite.SqliteException ex) when (ex.Message.Contains("duplicate column", StringComparison.OrdinalIgnoreCase))
    {
        // Column already present — nothing to do.
    }
}

// First in the pipeline so everything after it sees the original scheme/host/IP.
app.UseForwardedHeaders();

app.UseSerilogRequestLogging();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.UseStaticFiles();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
