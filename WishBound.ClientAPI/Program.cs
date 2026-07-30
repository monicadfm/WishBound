using WishBound.ClientAPI.Services;

// ============================================================
//  WishBound.ClientAPI - Aplicação Web (MVC)
//  Site que consome a WishBound.WebAPI através de HttpClient.
//  Não acede diretamente à base de dados: todas as operações
//  CRUD passam pela API.
// ============================================================

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// HttpClient "tipado" que comunica com a WebAPI.
// O endereço base é lido do appsettings.json.
builder.Services.AddHttpClient<WishBoundApiService>(client =>
{
    var baseUrl = builder.Configuration["WishBoundApi:BaseUrl"] ?? "http://localhost:5240/";
    client.BaseAddress = new Uri(baseUrl);
    client.Timeout = TimeSpan.FromSeconds(15);
});

var app = builder.Build();

// Em produção, os erros não tratados são encaminhados para uma página amigável
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseStaticFiles();

app.UseRouting();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
