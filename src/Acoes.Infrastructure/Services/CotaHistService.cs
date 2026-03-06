using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Acoes.Domain.Entities;
using Acoes.Domain.Interfaces.Infrastructure;
using Microsoft.Extensions.Logging;

namespace Acoes.Infrastructure.Services;

public class CotaHistService : ICotaHistService
{
    private readonly ILogger<CotaHistService> _logger;

    
    private static readonly HashSet<string> CodigosBdiPermitidos = new() { "02", "96" };

    
    private static readonly HashSet<string> TiposMercadoPermitidos = new() { "010", "020" };

    public CotaHistService(ILogger<CotaHistService> logger)
    {
        _logger = logger;
    }

    public async Task<IEnumerable<Cotacao>> ProcessarArquivoAsync(string caminhoArquivo)
    {
        var cotacoes = new List<Cotacao>();

        if (!File.Exists(caminhoArquivo))
        {
            _logger.LogError("Arquivo COTAHIST não encontrado em: {Caminho}", caminhoArquivo);
            throw new FileNotFoundException("Arquivo de cotações B3 não encontrado.");
        }

        
        
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        var encoding = Encoding.GetEncoding("ISO-8859-1");

        using var streamReader = new StreamReader(caminhoArquivo, encoding);
        string? linha;

        while ((linha = await streamReader.ReadLineAsync()) != null)
        {
            
            if (linha.Length < 245 || !linha.StartsWith("01")) continue;

            
            
            var codigoBdi = linha.Substring(10, 2);
            if (!CodigosBdiPermitidos.Contains(codigoBdi)) continue;

            
            
            
            var tipoMercado = linha.Substring(24, 3);
            if (!TiposMercadoPermitidos.Contains(tipoMercado)) continue;

            try
            {
                var dataStr = linha.Substring(2, 8);  
                var ticker = linha.Substring(12, 12).Trim(); 

                
                var precoAbertura = ObterDecimal(linha.Substring(56, 13));  
                var precoMaximo = ObterDecimal(linha.Substring(69, 13));  
                var precoMinimo = ObterDecimal(linha.Substring(82, 13));  
                var precoFechamento = ObterDecimal(linha.Substring(108, 13)); 

                var dataPregao = DateTime.ParseExact(dataStr, "yyyyMMdd", CultureInfo.InvariantCulture);

                cotacoes.Add(new Cotacao(dataPregao, ticker, precoAbertura, precoFechamento, precoMaximo, precoMinimo));
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Erro ao processar linha do COTAHIST: {Linha} | Erro: {Erro}", linha, ex.Message);
            }
        }

        _logger.LogInformation("COTAHIST processado: {Total} cotações carregadas de '{Arquivo}'", cotacoes.Count, caminhoArquivo);
        return cotacoes;
    }

    private static decimal ObterDecimal(string valorTexto)
    {
        if (long.TryParse(valorTexto.Trim(), out long valorLongo))
            return valorLongo / 100m;

        return 0m;
    }
}
