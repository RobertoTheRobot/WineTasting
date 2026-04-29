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

var app = builder.Build();

Directory.CreateDirectory(Path.Combine(app.Environment.ContentRootPath, "App_Data"));
Directory.CreateDirectory(Path.Combine(app.Environment.WebRootPath, "uploads", "wines"));

using (var scope = app.Services.CreateScope())
{
    var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<WineTastingDbContext>>();
    using var db = factory.CreateDbContext();
    db.Database.EnsureCreated();
}

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
