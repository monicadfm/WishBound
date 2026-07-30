using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;
using WishBound.WebAPI.Data;

// ============================================================
//  WishBound.WebAPI - Ponto de entrada da aplicação
//  API REST que expõe as operações CRUD (SELECT, INSERT,
//  UPDATE, DELETE) sobre a base de dados WishBoundDb.
// ============================================================

var builder = WebApplication.CreateBuilder(args);

// Ligação à base de dados SQL Server (LocalDB) através do Entity Framework Core
builder.Services.AddDbContext<WishBoundContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("WishBoundDb")));

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

// Criação automática da base de dados (com dados iniciais) se ainda não existir.
// Protegido com try-catch: se o SQL Server não estiver disponível,
// a aplicação avisa no terminal em vez de "rebentar".
using (var scope = app.Services.CreateScope())
{
    try
    {
        var contexto = scope.ServiceProvider.GetRequiredService<WishBoundContext>();
        contexto.Database.EnsureCreated();
    }
    catch (Exception ex)
    {
        Console.WriteLine("[WishBound] Erro ao criar/aceder à base de dados: " + ex.Message);
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
