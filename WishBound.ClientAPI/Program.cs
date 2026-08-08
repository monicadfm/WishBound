using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using WishBound.ClientAPI.Services;

// ============================================================
//  WishBound.ClientAPI - Aplicação Web (MVC)
//  Site que consome a WishBound.WebAPI através de HttpClient.
//  Não acede diretamente à base de dados: todas as operações
//  CRUD passam pela API.
// ============================================================

var builder = WebApplication.CreateBuilder(args);

// Configuração local NÃO versionada (está no .gitignore): guarda segredos
// como as credenciais Google, para nunca irem parar ao repositório público.
// Os valores daqui substituem os do appsettings.json.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

builder.Services.AddControllersWithViews();

// Autenticação por cookie: depois do login (verificado pela WebAPI),
// os dados do utilizador ficam guardados num cookie encriptado.
var autenticacao = builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Conta/Login";           // página de login
        options.AccessDeniedPath = "/Conta/AcessoNegado"; // autenticado mas sem permissões
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = true;
        options.Cookie.Name = "WishBound.Sessao";
        options.Cookie.HttpOnly = true;               // inacessível ao JavaScript
    })
    // Cookie TEMPORÁRIO usado apenas durante o "salto" ao Google:
    // guarda o resultado do OAuth até o ContaController.GoogleCallback
    // o trocar pela sessão normal (WishBound.Sessao) e o apagar.
    .AddCookie("Externo", options =>
    {
        options.Cookie.Name = "WishBound.Externo";
        options.ExpireTimeSpan = TimeSpan.FromMinutes(10);
    });

// LOGIN COM GOOGLE (OAuth 2.0) - funcionalidade opcional do enunciado.
// Só é ativado se houver credenciais no appsettings.json; sem elas o
// site funciona normalmente e o botão "Entrar com Google" fica escondido.
var googleClientId = builder.Configuration["Autenticacao:Google:ClientId"];
var googleClientSecret = builder.Configuration["Autenticacao:Google:ClientSecret"];

if (!string.IsNullOrWhiteSpace(googleClientId) && !string.IsNullOrWhiteSpace(googleClientSecret))
{
    autenticacao.AddGoogle(options =>
    {
        options.ClientId = googleClientId;
        options.ClientSecret = googleClientSecret;

        // O resultado do Google é guardado no cookie temporário "Externo"
        options.SignInScheme = "Externo";

        // Além dos claims padrão (id, nome, email), queremos a fotografia
        options.ClaimActions.MapJsonKey("urn:google:foto", "picture");

        // Permite que o cookie de correlação (anti-falsificação do OAuth)
        // funcione em http://localhost durante o desenvolvimento
        options.CorrelationCookie.SameSite = SameSiteMode.Lax;
        options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;

        // Se o utilizador cancelar no ecrã da Google (ou algo falhar do
        // lado dela), volta calmamente à página de login em vez de
        // rebentar com uma exceção não tratada.
        options.AccessDeniedPath = "/Conta/Login";
        options.Events.OnRemoteFailure = contexto =>
        {
            contexto.Response.Redirect("/Conta/Login");
            contexto.HandleResponse();
            return Task.CompletedTask;
        };
    });
}

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
