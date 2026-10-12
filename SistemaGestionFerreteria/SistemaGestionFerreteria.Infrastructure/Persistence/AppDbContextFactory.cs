using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using System.Text.Json;

namespace SistemaGestionFerreteria.Infrastructure.Persistence;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var folder = Directory.GetCurrentDirectory();
        while (!File.Exists(Path.Combine(folder, "appsettings.json")))
        {
            var appFolder = Path.Combine(folder, "SistemaGestionFerreteria");
            if (File.Exists(Path.Combine(appFolder, "appsettings.json"))) { folder = appFolder; break; }
            folder = Directory.GetParent(folder)?.FullName ?? throw new InvalidOperationException("No se encontró appsettings.json del proyecto.");
        }
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        using var config = JsonDocument.Parse(File.ReadAllText(Path.Combine(folder, "appsettings.json")), new JsonDocumentOptions { CommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true });
        var connection = config.RootElement.GetProperty("ConnectionStrings").GetProperty("DefaultConnection").GetString();
        var overridePath = Path.Combine(folder, "appsettings." + environment + ".json");
        if (File.Exists(overridePath))
        {
            using var overrides = JsonDocument.Parse(File.ReadAllText(overridePath));
            if (overrides.RootElement.TryGetProperty("ConnectionStrings", out var strings) && strings.TryGetProperty("DefaultConnection", out var value))
                connection = value.GetString();
        }
        connection = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection") ?? connection
            ?? throw new InvalidOperationException("Falta la conexión DefaultConnection.");
        return new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlServer(connection).Options);
    }
}
