using Microsoft.AspNetCore.Authentication.Cookies;
using WishBound.ClientAPI.Services;

// ============================================================
//  WishBound.ClientAPI - Aplicação Web (MVC)
//  Site que consome a WishBound.WebAPI através de HttpClient.
//  Não acede diretamente à base de dados: todas as operações
//  CRUD passam pela API.
// ============================================================

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Autenticação por cookie: depois do login (verificado pela WebAPI),
// os dados do utilizador ficam guardados num cookie encriptado.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Conta/Login";           // página de login
        options.AccessDeniedPath = "/Conta/AcessoNegado"; // autenticado mas sem permissões
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.Name = "WishBound.Sessao";
        options.Cookie.HttpOnly = true;               // inacessível ao JavaScript
    });

builder.Services.AddAuthorization();

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

// A ordem importa: primeiro identifica o utilizador (autenticação),
// depois verifica as permissões (autorização).
app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
