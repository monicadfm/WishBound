using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using WishBound.WebAPI.Data;

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

// Ligação à base de dados SQL Server Express através do Entity Framework Core
builder.Services.AddDbContext<WishBoundContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("WishBound")));

// Controllers + JSON (evita ciclos infinitos nas relações entre entidades)
builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

// Swagger - documentação e teste da API no browser
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// CORS - permite que outros clientes (ex.: app mobile no futuro) acedam à API
builder.Services.AddCors(options =>
{
    options.AddPolicy("PoliticaAberta", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
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

app.UseCors("PoliticaAberta");

app.MapControllers();

app.Run();
