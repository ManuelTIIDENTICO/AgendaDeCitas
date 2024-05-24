//void ConfigureServices(IServiceCollection services)
//{
//    services.AddControllersWithViews();
//   services.AddHttpClient();
//}
var builder = WebApplication.CreateBuilder(args);

// Agregar servicios al contenedor
builder.Services.AddControllersWithViews();

// Registrar HttpClient
builder.Services.AddHttpClient();

var app = builder.Build();

// Configurar el pipeline de HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Ciudades}/{action=Index}/{id?}");

app.Run();
