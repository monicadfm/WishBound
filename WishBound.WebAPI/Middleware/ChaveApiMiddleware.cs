using System.Security.Cryptography;
using System.Text;

namespace WishBound.WebAPI.Middleware
{
    /// <summary>
    /// Protege a WebAPI de pedidos que não venham do site WishBound.ClientAPI.
    ///
    /// Antes desta camada, qualquer pessoa podia abrir http://localhost:5240
    /// e usar os endpoints diretamente (criar personagens, invocar em nome de
    /// outro utilizador, ler o histórico alheio...). Agora todos os pedidos
    /// para /api/... têm de trazer o cabeçalho X-WishBound-Chave com a chave
    /// combinada entre os dois projetos (secção "Seguranca:ChaveApi" da
    /// configuração); os restantes recebem 401 Unauthorized.
    ///
    /// O Swagger continua acessível para documentação; para EXPERIMENTAR os
    /// endpoints é preciso carregar em "Authorize" e colar a chave.
    /// </summary>
    public class ChaveApiMiddleware
    {
        public const string NomeCabecalho = "X-WishBound-Chave";

        private readonly RequestDelegate _proximo;
        private readonly string _chaveEsperada;
        private readonly ILogger<ChaveApiMiddleware> _registo;

        public ChaveApiMiddleware(RequestDelegate proximo, IConfiguration configuracao, ILogger<ChaveApiMiddleware> registo)
        {
            _proximo = proximo;
            _registo = registo;
            _chaveEsperada = configuracao["Seguranca:ChaveApi"] ?? string.Empty;

            if (string.IsNullOrWhiteSpace(_chaveEsperada))
            {
                _registo.LogWarning(
                    "Não há chave de API configurada (Seguranca:ChaveApi) - todos os pedidos a /api serão recusados.");
            }
        }

        public async Task InvokeAsync(HttpContext contexto)
        {
            // Os pedidos "preflight" do CORS (OPTIONS) nunca trazem cabeçalhos
            // personalizados - são tratados pela política de CORS, não aqui.
            if (HttpMethods.IsOptions(contexto.Request.Method))
            {
                await _proximo(contexto);
                return;
            }

            // Só os endpoints da API são protegidos (o Swagger e a página
            // inicial continuam a abrir normalmente no browser).
            if (!contexto.Request.Path.StartsWithSegments("/api"))
            {
                await _proximo(contexto);
                return;
            }

            if (!ChaveValida(contexto.Request.Headers[NomeCabecalho].ToString()))
            {
                _registo.LogWarning("Pedido recusado (chave de API inválida): {Metodo} {Caminho}",
                    contexto.Request.Method, contexto.Request.Path);

                contexto.Response.StatusCode = StatusCodes.Status401Unauthorized;
                contexto.Response.ContentType = "text/plain; charset=utf-8";
                await contexto.Response.WriteAsync(
                    "Pedido não autorizado: falta o cabeçalho " + NomeCabecalho + " com uma chave válida.");
                return;
            }

            await _proximo(contexto);
        }

        /// <summary>
        /// Compara a chave recebida com a esperada em tempo constante
        /// (não desiste no primeiro caractere diferente), para não dar pistas
        /// a quem tente adivinhá-la.
        /// </summary>
        private bool ChaveValida(string? chaveRecebida)
        {
            if (string.IsNullOrWhiteSpace(_chaveEsperada) || string.IsNullOrWhiteSpace(chaveRecebida))
            {
                return false;
            }

            byte[] esperada = Encoding.UTF8.GetBytes(_chaveEsperada);
            byte[] recebida = Encoding.UTF8.GetBytes(chaveRecebida);

            return esperada.Length == recebida.Length &&
                   CryptographicOperations.FixedTimeEquals(esperada, recebida);
        }
    }
}
