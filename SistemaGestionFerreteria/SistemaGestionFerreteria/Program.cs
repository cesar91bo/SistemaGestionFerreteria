using Microsoft.EntityFrameworkCore;
using SistemaGestionFerreteria.Application.Interfaces;
using SistemaGestionFerreteria.Components;
using SistemaGestionFerreteria.Infrastructure.Persistence;
using SistemaGestionFerreteria.Infrastructure.Services;
using SistemaGestionFerreteria.Infrastructure.Services.Categorias;
using SistemaGestionFerreteria.Infrastructure.Services.Clientes;
using SistemaGestionFerreteria.Infrastructure.Services.Parametros;
using SistemaGestionFerreteria.Infrastructure.Services.Productos;
using SistemaGestionFerreteria.Infrastructure.Services.Unidades;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContextFactory<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<IProductoService, ProductoService>();
builder.Services.AddScoped<IProductoPrecioService, ProductoPrecioService>();
builder.Services.AddScoped<ICategoriaService, CategoriaService>();
builder.Services.AddScoped<IProveedorService, ProveedorService>();
builder.Services.AddScoped<IUnidadMedidaService, UnidadMedidaService>();
builder.Services.AddScoped<IParametroService, ParametroService>();
builder.Services.AddScoped<IClienteService, ClienteService>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();