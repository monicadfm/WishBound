using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using System.Text.Json.Serialization;
using WishBound.WebAPI.Data;
using WishBound.WebAPI.Middleware;
using WishBound.WebAPI.Services;

// ============================================================
//  WishBound.WebAPI - Ponto de entrada da aplicação
//  API REST que expõe as operações CRUD (SELECT, INSERT,
//  UPDATE, DELETE) sobre a base de dados WishBound.
//
//  A base de dados é gerida diretamente no SQL Server Express
//  ("database first"): a aplicação NUNCA cria nem altera o
//  esquema — apenas o utiliza.
// ============================================================

var builder = WebApplication.CreateBuilder(args);

// Configuração local NÃO versionada (está no .gitignore): guarda a chave
// da API e as credenciais do servidor de email, para nunca irem parar ao
// repositório. Os valores daqui substituem os do appsettings.json.
builder.Configuration.AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: true);

// Ligação à base de dados SQL Server Express através do Entity Framework Core
builder.Services.AddDbContext<WishBoundContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("WishBound")));

// Controllers + JSON (evita ciclos infinitos nas relações entre entidades)
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

// Envio de emails (validação de conta e recuperação de password).
// Sem definições de SMTP em "Email", o serviço fica inativo e a API
// continua a devolver o token para o site mostrar o link no ecrã.
builder.Services.Configure<DefinicoesEmail>(builder.Configuration.GetSection("Email"));
builder.Services.AddSingleton<IServicoEmail, ServicoEmailSmtp>();

// Sistema de amizade: regras de pontos e recompensas por nível, usadas
// pelo AmizadeController (interações) e pelo InvocacoesController
// (cópias repetidas). Scoped porque usa o DbContext do pedido.
builder.Services.AddScoped<ServicoAmizade>();

// Swagger - documentação e teste da API no browser.
// Como a API passou a exigir uma chave, o Swagger ganha o botão "Authorize"
// para a colar (senão todos os pedidos de teste dariam 401).
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition(ChaveApiMiddleware.NomeCabecalho, new OpenApiSecurityScheme
    {
        Name = ChaveApiMiddleware.NomeCabecalho,
        Type = SecuritySchemeType.ApiKey,
        In = ParameterLocation.Header,
        Description = "Chave partilhada entre o site e a API (Seguranca:ChaveApi)."
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = ChaveApiMiddleware.NomeCabecalho
                }
            },
            Array.Empty<string>()
        }
    });
});

// CORS - só o site WishBound (e a futura app mobile, quando existir) pode
// chamar a API a partir de um browser. Antes a política era aberta a todos.
builder.Services.AddCors(options =>
{
    options.AddPolicy("PoliticaSite", policy =>
        policy.WithOrigins(
                  builder.Configuration.GetSection("Seguranca:OrigensPermitidas").Get<string[]>()
                  ?? new[] { "http://localhost:5100", "https://localhost:7100" })
              .AllowAnyMethod()
              .AllowAnyHeader());
});

var app = builder.Build();

// Verificação da ligação à base de dados no arranque.
// Protegido com try-catch: se o SQL Server não estiver disponível,
// a aplicação avisa no terminal em vez de "rebentar".
using (var scope = app.Services.CreateScope())
{
    try
    {
        var contexto = scope.ServiceProvider.GetRequiredService<WishBoundContext>();
        if (contexto.Database.CanConnect())
        {
            Console.WriteLine("[WishBound] Ligação à base de dados 'WishBound' (.\\SQLEXPRESS) estabelecida.");
        }
        else
        {
            Console.WriteLine("[WishBound] AVISO: não foi possível ligar à base de dados 'WishBound' em .\\SQLEXPRESS.");
            Console.WriteLine("[WishBound] Confirme que o serviço SQL Server (SQLEXPRESS) está a correr e que o script da base de dados foi executado.");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine("[WishBound] Erro ao aceder à base de dados: " + ex.Message);
    }
}

// Pipeline HTTP
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// Nota: não usamos redirecionamento HTTP->HTTPS para simplificar a
// comunicação com o ClientAPI em ambiente de desenvolvimento.

app.UseCors("PoliticaSite");

// Chave de API: tudo o que for /api/... tem de trazer o cabeçalho
// X-WishBound-Chave. Fica DEPOIS do CORS e ANTES dos controllers.
app.UseMiddleware<ChaveApiMiddleware>();

app.MapControllers();

app.Run();
