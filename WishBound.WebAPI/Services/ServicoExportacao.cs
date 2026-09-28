using System.Globalization;
using System.Text;
using System.Xml.Linq;

namespace WishBound.WebAPI.Services
{
    // ============================================================
    //  EXPORTAÇÃO DE RELATÓRIOS — PDF e XML, sem bibliotecas externas.
    //
    //  Um relatório é uma lista de SECÇÕES; cada secção pode ter
    //  pares chave/valor, um gráfico de barras e/ou uma tabela. O
    //  mesmo modelo gera os dois formatos:
    //    - XML: System.Xml.Linq (XDocument), um elemento por linha,
    //      com os nomes de coluna como elementos filhos;
    //    - PDF: escrito à mão segundo a especificação PDF 1.4 — só
    //      texto (Helvetica, codificação WinAnsi, que tem os acentos
    //      portugueses), retângulos e linhas. Tabelas com cabeçalho
    //      repetido em cada página, barras horizontais, rodapé
    //      "Página n de N". Assim o projeto não precisa de NuGet
    //      extra nem de licenças.
    // ============================================================

    public class RelatorioExportacao
    {
        public string Titulo { get; set; } = string.Empty;
        public string Conjunto { get; set; } = string.Empty;
        public string GeradoPor { get; set; } = string.Empty;
        public DateTime GeradoEm { get; set; } = DateTime.UtcNow;

        /// <summary>Tabelas largas ficam melhor em A4 horizontal.</summary>
        public bool Horizontal { get; set; }

        public List<SeccaoRelatorio> Seccoes { get; set; } = new List<SeccaoRelatorio>();
    }

    public class SeccaoRelatorio
    {
        public string Titulo { get; set; } = string.Empty;

        /// <summary>Nome do elemento XML da secção (sem espaços nem acentos).</summary>
        public string ElementoXml { get; set; } = "Seccao";

        public List<(string Chave, string Valor)> Valores { get; set; } = new List<(string, string)>();

        public List<BarraRelatorio> Barras { get; set; } = new List<BarraRelatorio>();

        public TabelaRelatorio? Tabela { get; set; }
    }

    public class BarraRelatorio
    {
        public string Etiqueta { get; set; } = string.Empty;
        public decimal Valor { get; set; }
        public string TextoValor { get; set; } = string.Empty;
        public string? CorHex { get; set; }
    }

    public class TabelaRelatorio
    {
        /// <summary>Nome do elemento XML de cada linha ("Utilizador", "Personagem"...).</summary>
        public string ElementoLinha { get; set; } = "Linha";

        public List<ColunaRelatorio> Colunas { get; set; } = new List<ColunaRelatorio>();
        public List<string[]> Linhas { get; set; } = new List<string[]>();
    }

    public class ColunaRelatorio
    {
        public string Titulo { get; set; } = string.Empty;
        public string ElementoXml { get; set; } = string.Empty;
        public bool Numero { get; set; }

        public ColunaRelatorio(string titulo, string elementoXml, bool numero = false)
        {
            Titulo = titulo;
            ElementoXml = elementoXml;
            Numero = numero;
        }
    }

    public static class ServicoExportacao
    {
        public static readonly CultureInfo Pt = CultureInfo.GetCultureInfo("pt-PT");

        // ============================================================
        //  XML
        // ============================================================

        public static byte[] GerarXml(RelatorioExportacao relatorio)
        {
            var raiz = new XElement("RelatorioWishBound",
                new XAttribute("conjunto", relatorio.Conjunto),
                new XAttribute("titulo", relatorio.Titulo),
                new XAttribute("geradoEm", relatorio.GeradoEm.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)),
                new XAttribute("geradoPor", relatorio.GeradoPor));

            foreach (var seccao in relatorio.Seccoes)
            {
                var elemento = new XElement(seccao.ElementoXml, new XAttribute("titulo", seccao.Titulo));

                foreach (var (chave, valor) in seccao.Valores)
                {
                    elemento.Add(new XElement("Valor", new XAttribute("nome", chave), valor));
                }

                foreach (var barra in seccao.Barras)
                {
                    elemento.Add(new XElement("Item",
                        new XAttribute("nome", barra.Etiqueta),
                        barra.Valor.ToString(CultureInfo.InvariantCulture)));
                }

                if (seccao.Tabela != null)
                {
                    foreach (var linha in seccao.Tabela.Linhas)
                    {
                        var elementoLinha = new XElement(seccao.Tabela.ElementoLinha);
                        for (int i = 0; i < seccao.Tabela.Colunas.Count; i++)
                        {
                            elementoLinha.Add(new XElement(seccao.Tabela.Colunas[i].ElementoXml, i < linha.Length ? linha[i] : string.Empty));
                        }
                        elemento.Add(elementoLinha);
                    }
                }

                raiz.Add(elemento);
            }

            var documento = new XDocument(new XDeclaration("1.0", "utf-8", null), raiz);

            using var memoria = new MemoryStream();
            using (var escritor = new StreamWriter(memoria, new UTF8Encoding(false)))
            {
                documento.Save(escritor);
            }

            return memoria.ToArray();
        }

        // ============================================================
        //  PDF
        // ============================================================

        // Medidas em pontos (1/72 polegada). A4 = 595 x 842.
        private const float Margem = 40f;
        private const float AlturaLinhaTabela = 16f;
        private const float TamanhoTexto = 8.5f;

        // Cores (RGB 0..1): as da marca — roxo e dourado — em versão para papel
        private static readonly float[] CorRoxo = { 0.36f, 0.23f, 0.62f };
        private static readonly float[] CorDourado = { 0.80f, 0.60f, 0.12f };
        private static readonly float[] CorTexto = { 0.13f, 0.12f, 0.17f };
        private static readonly float[] CorSuave = { 0.42f, 0.40f, 0.48f };
        private static readonly float[] CorZebra = { 0.95f, 0.94f, 0.97f };
        private static readonly float[] CorLinha = { 0.82f, 0.80f, 0.86f };

        public static byte[] GerarPdf(RelatorioExportacao relatorio)
        {
            var pdf = new EscritorPaginas(relatorio.Horizontal ? 842f : 595f, relatorio.Horizontal ? 595f : 842f);

            pdf.NovaPagina();
            Cabecalho(pdf, relatorio);

            foreach (var seccao in relatorio.Seccoes)
            {
                pdf.GarantirEspaco(60);
                pdf.Y -= 10;
                pdf.Texto(Margem, pdf.Y, seccao.Titulo, 12f, negrito: true, CorRoxo);
                pdf.Y -= 6;
                pdf.Linha(Margem, pdf.Y, pdf.Largura - Margem, pdf.Y, CorDourado, 1.2f);
                pdf.Y -= 14;

                foreach (var (chave, valor) in seccao.Valores)
                {
                    pdf.GarantirEspaco(14);
                    pdf.Texto(Margem, pdf.Y, chave, 9.5f, negrito: false, CorSuave);
                    pdf.Texto(Margem + 210, pdf.Y, valor, 9.5f, negrito: true, CorTexto);
                    pdf.Y -= 14;
                }

                if (seccao.Barras.Count > 0)
                {
                    Barras(pdf, seccao.Barras);
                }

                if (seccao.Tabela != null)
                {
                    Tabela(pdf, seccao.Tabela);
                }

                pdf.Y -= 6;
            }

            return pdf.Construir(relatorio);
        }

        private static void Cabecalho(EscritorPaginas pdf, RelatorioExportacao relatorio)
        {
            pdf.Retangulo(0, pdf.Altura - 70, pdf.Largura, 70, CorRoxo);
            pdf.Texto(Margem, pdf.Altura - 34, "WishBound", 18f, negrito: true, new[] { 1f, 0.84f, 0.40f });
            pdf.Texto(Margem, pdf.Altura - 54, relatorio.Titulo, 11f, negrito: false, new[] { 1f, 1f, 1f });

            string gerado = "Gerado em " + relatorio.GeradoEm.ToLocalTime().ToString("dd/MM/yyyy HH:mm", Pt) + " por " + relatorio.GeradoPor;
            float largura = Medir(gerado, 8.5f, false);
            pdf.Texto(pdf.Largura - Margem - largura, pdf.Altura - 54, gerado, 8.5f, negrito: false, new[] { 0.90f, 0.87f, 0.97f });

            pdf.Y = pdf.Altura - 90;
        }

        private static void Barras(EscritorPaginas pdf, List<BarraRelatorio> barras)
        {
            decimal maximo = Math.Max(1m, barras.Max(b => b.Valor));
            float xBarra = Margem + 130;

            // Espaço à direita para o maior texto de valor (nunca sai da página)
            float maiorTexto = barras.Max(b => Medir(b.TextoValor, TamanhoTexto, true));
            float larguraMax = Math.Max(60f, pdf.Largura - Margem - xBarra - maiorTexto - 10);

            foreach (var barra in barras)
            {
                pdf.GarantirEspaco(16);
                pdf.Texto(Margem, pdf.Y, Ajustar(barra.Etiqueta, 125, TamanhoTexto, false), TamanhoTexto, negrito: false, CorTexto);

                float largura = (float)(barra.Valor / maximo) * larguraMax;
                if (largura > 0)
                {
                    pdf.Retangulo(xBarra, pdf.Y - 2, Math.Max(2f, largura), 9, CorDeHex(barra.CorHex) ?? CorRoxo);
                }

                pdf.Texto(xBarra + largura + 6, pdf.Y, barra.TextoValor, TamanhoTexto, negrito: true, CorTexto);
                pdf.Y -= 15;
            }

            pdf.Y -= 4;
        }

        private static void Tabela(EscritorPaginas pdf, TabelaRelatorio tabela)
        {
            int n = tabela.Colunas.Count;
            if (n == 0)
            {
                return;
            }

            // Larguras proporcionais ao conteúdo (com mínimo e máximo por coluna)
            float disponivel = pdf.Largura - 2 * Margem;
            var naturais = new float[n];
            for (int c = 0; c < n; c++)
            {
                float maior = Medir(tabela.Colunas[c].Titulo, TamanhoTexto, true);
                foreach (var linha in tabela.Linhas.Take(200))
                {
                    if (c < linha.Length)
                    {
                        maior = Math.Max(maior, Medir(linha[c], TamanhoTexto, false));
                    }
                }
                naturais[c] = Math.Clamp(maior + 10, 28, 260);
            }

            // As colunas numéricas ficam com a largura natural (números nunca
            // são cortados); o espaço que sobra reparte-se pelas de texto.
            var larguras = naturais.ToArray();
            float soma = naturais.Sum();
            if (soma > disponivel)
            {
                float numericas = Enumerable.Range(0, n).Where(c => tabela.Colunas[c].Numero).Sum(c => naturais[c]);
                float texto = soma - numericas;
                float sobra = Math.Max(disponivel - numericas, 40f * n);
                for (int c = 0; c < n; c++)
                {
                    if (!tabela.Colunas[c].Numero && texto > 0)
                    {
                        larguras[c] = naturais[c] * sobra / texto;
                    }
                }
            }
            else
            {
                larguras = naturais.Select(l => l * disponivel / soma).ToArray();
            }

            void CabecalhoTabela()
            {
                pdf.Retangulo(Margem, pdf.Y - 4, disponivel, AlturaLinhaTabela, CorRoxo);
                float x = Margem;
                for (int c = 0; c < n; c++)
                {
                    string titulo = Ajustar(tabela.Colunas[c].Titulo, larguras[c] - 8, TamanhoTexto, true);
                    float xt = tabela.Colunas[c].Numero ? x + larguras[c] - 4 - Medir(titulo, TamanhoTexto, true) : x + 4;
                    pdf.Texto(xt, pdf.Y + 1, titulo, TamanhoTexto, negrito: true, new[] { 1f, 1f, 1f });
                    x += larguras[c];
                }
                pdf.Y -= AlturaLinhaTabela;
            }

            pdf.GarantirEspaco(AlturaLinhaTabela * 3);
            CabecalhoTabela();

            if (tabela.Linhas.Count == 0)
            {
                pdf.Texto(Margem + 4, pdf.Y + 1, "(sem registos)", TamanhoTexto, negrito: false, CorSuave);
                pdf.Y -= AlturaLinhaTabela;
                return;
            }

            int indice = 0;
            foreach (var linha in tabela.Linhas)
            {
                if (pdf.Y - AlturaLinhaTabela < Margem + 20)
                {
                    pdf.NovaPagina();
                    CabecalhoTabela();
                }

                if (indice % 2 == 1)
                {
                    pdf.Retangulo(Margem, pdf.Y - 4, disponivel, AlturaLinhaTabela, CorZebra);
                }

                float x = Margem;
                for (int c = 0; c < n; c++)
                {
                    string valor = Ajustar(c < linha.Length ? linha[c] : string.Empty, larguras[c] - 8, TamanhoTexto, false);
                    float xt = tabela.Colunas[c].Numero ? x + larguras[c] - 4 - Medir(valor, TamanhoTexto, false) : x + 4;
                    pdf.Texto(xt, pdf.Y + 1, valor, TamanhoTexto, negrito: false, CorTexto);
                    x += larguras[c];
                }

                pdf.Y -= AlturaLinhaTabela;
                indice++;
            }

            pdf.Linha(Margem, pdf.Y + AlturaLinhaTabela - 4, Margem + disponivel, pdf.Y + AlturaLinhaTabela - 4, CorLinha, 0.6f);
            pdf.Y -= 4;
        }

        // ------------------------------------------------------------
        //  Medição de texto (larguras Helvetica, em milésimos do tamanho)
        // ------------------------------------------------------------

        // Larguras AFM da Helvetica para os caracteres 32..126
        private static readonly int[] LargurasHelvetica =
        {
            278,278,355,556,556,889,667,191,333,333,389,584,278,333,278,278,
            556,556,556,556,556,556,556,556,556,556,278,278,584,584,584,556,
            1015,667,667,722,722,667,611,778,722,278,500,667,556,833,722,778,
            667,778,722,667,611,722,667,944,667,667,611,278,278,278,469,556,
            333,556,556,500,556,556,278,556,556,222,222,500,222,833,556,556,
            556,556,333,500,278,556,500,722,500,500,500,334,260,334,584
        };

        public static float Medir(string texto, float tamanho, bool negrito)
        {
            float total = 0;
            foreach (char ch in Normalizar(texto))
            {
                char baseChar = LetraBase(ch);
                int largura = baseChar >= 32 && baseChar <= 126 ? LargurasHelvetica[baseChar - 32] : 556;
                total += largura;
            }

            return total / 1000f * tamanho * (negrito ? 1.06f : 1f);
        }

        /// <summary>Corta o texto com "…" para caber na largura.</summary>
        private static string Ajustar(string texto, float largura, float tamanho, bool negrito)
        {
            texto = Normalizar(texto ?? string.Empty);
            if (Medir(texto, tamanho, negrito) <= largura)
            {
                return texto;
            }

            while (texto.Length > 1 && Medir(texto + "…", tamanho, negrito) > largura)
            {
                texto = texto.Substring(0, texto.Length - 1);
            }

            return texto.TrimEnd() + "…";
        }

        /// <summary>Troca caracteres que a WinAnsi não tem por equivalentes.</summary>
        private static string Normalizar(string texto) => texto
            .Replace("→", "->")
            .Replace("←", "<-")
            .Replace("♥", "<3")
            .Replace("★", "*")
            .Replace("\r", " ")
            .Replace("\n", " ")
            .Replace("\t", " ");

        private static char LetraBase(char ch)
        {
            string decomposto = ch.ToString().Normalize(NormalizationForm.FormD);
            return decomposto.Length > 0 ? decomposto[0] : ch;
        }

        private static float[]? CorDeHex(string? hex)
        {
            if (string.IsNullOrWhiteSpace(hex) || hex.Length != 7 || hex[0] != '#')
            {
                return null;
            }

            try
            {
                int r = Convert.ToInt32(hex.Substring(1, 2), 16);
                int g = Convert.ToInt32(hex.Substring(3, 2), 16);
                int b = Convert.ToInt32(hex.Substring(5, 2), 16);
                return new[] { r / 255f, g / 255f, b / 255f };
            }
            catch (FormatException)
            {
                return null;
            }
        }

        // ------------------------------------------------------------
        //  Escritor de páginas + montagem do ficheiro PDF
        // ------------------------------------------------------------

        private class EscritorPaginas
        {
            public float Largura { get; }
            public float Altura { get; }
            public float Y { get; set; }

            private readonly List<StringBuilder> _paginas = new List<StringBuilder>();
            private StringBuilder Atual => _paginas[^1];

            public EscritorPaginas(float largura, float altura)
            {
                Largura = largura;
                Altura = altura;
            }

            public void NovaPagina()
            {
                _paginas.Add(new StringBuilder());
                Y = Altura - Margem;
            }

            public void GarantirEspaco(float altura)
            {
                if (Y - altura < Margem + 20)
                {
                    NovaPagina();
                }
            }

            public void Texto(float x, float y, string texto, float tamanho, bool negrito, float[] cor)
            {
                Atual.Append(Cor(cor, preencher: true))
                     .Append("BT /").Append(negrito ? "F2 " : "F1 ").Append(Num(tamanho)).Append(" Tf ")
                     .Append(Num(x)).Append(' ').Append(Num(y)).Append(" Td (")
                     .Append(Escapar(Normalizar(texto))).Append(") Tj ET\n");
            }

            public void Retangulo(float x, float y, float largura, float altura, float[] cor)
            {
                Atual.Append(Cor(cor, preencher: true))
                     .Append(Num(x)).Append(' ').Append(Num(y)).Append(' ')
                     .Append(Num(largura)).Append(' ').Append(Num(altura)).Append(" re f\n");
            }

            public void Linha(float x1, float y1, float x2, float y2, float[] cor, float espessura)
            {
                Atual.Append(Cor(cor, preencher: false))
                     .Append(Num(espessura)).Append(" w ")
                     .Append(Num(x1)).Append(' ').Append(Num(y1)).Append(" m ")
                     .Append(Num(x2)).Append(' ').Append(Num(y2)).Append(" l S\n");
            }

            /// <summary>Monta o ficheiro: catálogo, páginas, fontes, conteúdos e a tabela xref.</summary>
            public byte[] Construir(RelatorioExportacao relatorio)
            {
                // Rodapé em todas as páginas (agora já se sabe o total)
                for (int i = 0; i < _paginas.Count; i++)
                {
                    string rodape = "WishBound · " + relatorio.Titulo + " · Página " + (i + 1) + " de " + _paginas.Count;
                    _paginas[i].Append(Cor(CorSuave, preencher: true))
                        .Append("BT /F1 7.5 Tf ").Append(Num(Margem)).Append(' ').Append(Num(Margem - 16))
                        .Append(" Td (").Append(Escapar(rodape)).Append(") Tj ET\n");
                }

                var objetos = new List<string>();
                int nPaginas = _paginas.Count;

                // 1 catálogo · 2 árvore de páginas · 3/4 fontes · 5 info · depois (página, conteúdo) por página
                string kids = string.Join(" ", Enumerable.Range(0, nPaginas).Select(i => (6 + i * 2) + " 0 R"));

                objetos.Add("<< /Type /Catalog /Pages 2 0 R >>");
                objetos.Add("<< /Type /Pages /Kids [" + kids + "] /Count " + nPaginas + " >>");
                objetos.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica /Encoding /WinAnsiEncoding >>");
                objetos.Add("<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica-Bold /Encoding /WinAnsiEncoding >>");
                objetos.Add("<< /Title (" + Escapar("WishBound - " + relatorio.Titulo) + ") /Producer (WishBound.WebAPI) /CreationDate (D:" +
                            relatorio.GeradoEm.ToString("yyyyMMddHHmmss", CultureInfo.InvariantCulture) + "Z) >>");

                for (int i = 0; i < nPaginas; i++)
                {
                    int conteudo = 7 + i * 2;
                    objetos.Add("<< /Type /Page /Parent 2 0 R /MediaBox [0 0 " + Num(Largura) + " " + Num(Altura) + "] " +
                                "/Resources << /Font << /F1 3 0 R /F2 4 0 R >> >> /Contents " + conteudo + " 0 R >>");

                    byte[] fluxo = Latin1(_paginas[i].ToString());
                    objetos.Add("<< /Length " + fluxo.Length + " >>\nstream\n" + _paginas[i] + "endstream");
                }

                using var memoria = new MemoryStream();
                void Escrever(string s)
                {
                    var bytes = Latin1(s);
                    memoria.Write(bytes, 0, bytes.Length);
                }

                Escrever("%PDF-1.4\n%âãÏÓ\n");

                var posicoes = new List<long>();
                for (int i = 0; i < objetos.Count; i++)
                {
                    posicoes.Add(memoria.Position);
                    Escrever((i + 1) + " 0 obj\n" + objetos[i] + "\nendobj\n");
                }

                long inicioXref = memoria.Position;
                var xref = new StringBuilder();
                xref.Append("xref\n0 ").Append(objetos.Count + 1).Append('\n');
                xref.Append("0000000000 65535 f \n");
                foreach (var posicao in posicoes)
                {
                    xref.Append(posicao.ToString("D10", CultureInfo.InvariantCulture)).Append(" 00000 n \n");
                }
                xref.Append("trailer\n<< /Size ").Append(objetos.Count + 1).Append(" /Root 1 0 R /Info 5 0 R >>\n");
                xref.Append("startxref\n").Append(inicioXref).Append("\n%%EOF\n");
                Escrever(xref.ToString());

                return memoria.ToArray();
            }

            private static string Cor(float[] cor, bool preencher) =>
                Num(cor[0]) + " " + Num(cor[1]) + " " + Num(cor[2]) + (preencher ? " rg " : " RG ");

            private static string Num(float valor) =>
                Math.Round(valor, 2).ToString("0.##", CultureInfo.InvariantCulture);

            /// <summary>
            /// Texto para dentro de ( ) num fluxo PDF: escapa \ ( ) e escreve
            /// os caracteres não-ASCII como octal na codificação WinAnsi.
            /// </summary>
            private static string Escapar(string texto)
            {
                var sb = new StringBuilder(texto.Length + 8);
                foreach (char ch in texto)
                {
                    int codigo = WinAnsi(ch);

                    if (codigo == '\\' || codigo == '(' || codigo == ')')
                    {
                        sb.Append('\\').Append((char)codigo);
                    }
                    else if (codigo >= 32 && codigo <= 126)
                    {
                        sb.Append((char)codigo);
                    }
                    else
                    {
                        sb.Append('\\').Append(Convert.ToString(codigo, 8).PadLeft(3, '0'));
                    }
                }
                return sb.ToString();
            }

            /// <summary>Código WinAnsi (Windows-1252) de um carácter; '?' se não existir.</summary>
            private static int WinAnsi(char ch)
            {
                if (ch < 128) return ch;
                if (ch >= 0xA0 && ch <= 0xFF) return ch; // Latin-1 = WinAnsi nesta zona (á, ç, ã, é, º...)

                return ch switch
                {
                    '€' => 0x80, '‚' => 0x82, '„' => 0x84, '…' => 0x85, '•' => 0x95,
                    '‘' => 0x91, '’' => 0x92, '“' => 0x93, '”' => 0x94,
                    '–' => 0x96, '—' => 0x97, '™' => 0x99, '·' => 0xB7,
                    _ => '?'
                };
            }

            private static byte[] Latin1(string s) => Encoding.Latin1.GetBytes(s);
        }
    }
}
