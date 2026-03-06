using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Acoes.Application.Common;
using Acoes.Application.DTOs;
using Acoes.Application.Services;
using Acoes.Domain.Interfaces.Infrastructure;
using Acoes.Domain.Interfaces.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Acoes.Api.Controllers;

[ApiController]
[Route("api/admin")]
[Produces("application/json")]
public class AdminController : ControllerBase
{
    private readonly AdminAppService _adminAppService;
    private readonly ICotaHistService _cotaHistService;
    private readonly ICotacaoRepository _cotacaoRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly string _cotaHistDirectory;

    public AdminController(
        AdminAppService adminAppService,
        ICotaHistService cotaHistService,
        ICotacaoRepository cotacaoRepository,
        IUnitOfWork unitOfWork,
        IConfiguration configuration)
    {
        _adminAppService = adminAppService;
        _cotaHistService = cotaHistService;
        _cotacaoRepository = cotacaoRepository;
        _unitOfWork = unitOfWork;
        _cotaHistDirectory = configuration["CotaHistDirectory"]
            ?? Path.Combine(Directory.GetCurrentDirectory(), "data", "cotahist");
    }

    

    
    
    
    
    [HttpPost("cotacoes/upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResult<InserirCotacoesResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    public async Task<IActionResult> UploadCotacoes(IFormFile arquivo)
    {
        if (arquivo is null || arquivo.Length == 0)
            return BadRequest(ApiResult<object>.Failure("Arquivo inválido ou vazio.", "ARQUIVO_INVALIDO"));

        
        var tempPath = Path.GetTempFileName();
        try
        {
            await using (var stream = System.IO.File.Create(tempPath))
                await arquivo.CopyToAsync(stream);

            var cotacoes = (await _cotaHistService.ProcessarArquivoAsync(tempPath)).ToList();

            if (cotacoes.Count == 0)
                return Ok(ApiResult<InserirCotacoesResponse>.Success(new InserirCotacoesResponse
                {
                    TotalInserido = 0,
                    Tickers = new(),
                    Mensagem = "Arquivo processado, mas nenhuma cotação válida foi encontrada (verifique filtros BDI/TPMERC)."
                }));

            await _cotacaoRepository.AdicionarVariosAsync(cotacoes);
            await _unitOfWork.CommitAsync();

            var tickers = cotacoes.Select(c => c.Ticker).Distinct().OrderBy(t => t).ToList();
            return Ok(ApiResult<InserirCotacoesResponse>.Success(new InserirCotacoesResponse
            {
                TotalInserido = cotacoes.Count,
                DataPregao = cotacoes[0].DataPregao,
                Tickers = tickers,
                Mensagem = $"{cotacoes.Count} cotações importadas com sucesso para {tickers.Count} tickers."
            }));
        }
        finally
        {
            if (System.IO.File.Exists(tempPath))
                System.IO.File.Delete(tempPath);
        }
    }

    
    
    
    
    
    [HttpPost("cotacoes/processar")]
    [ProducesResponseType(typeof(ApiResult<InserirCotacoesResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 404)]
    public async Task<IActionResult> ProcessarCotacoesLocal([FromQuery] string nomeArquivo)
    {
        if (string.IsNullOrWhiteSpace(nomeArquivo))
            return BadRequest(ApiResult<object>.Failure("Informe o nome do arquivo.", "ARQUIVO_INVALIDO"));

        
        var caminhoArquivo = Path.Combine(_cotaHistDirectory, nomeArquivo);

        if (!System.IO.File.Exists(caminhoArquivo))
            return NotFound(ApiResult<object>.Failure(
                $"Arquivo '{nomeArquivo}' não encontrado em data/cotahist/. Caminho buscado: {caminhoArquivo}",
                "ARQUIVO_NAO_ENCONTRADO"));

        var cotacoes = (await _cotaHistService.ProcessarArquivoAsync(caminhoArquivo)).ToList();

        if (cotacoes.Count == 0)
            return Ok(ApiResult<InserirCotacoesResponse>.Success(new InserirCotacoesResponse
            {
                TotalInserido = 0,
                Tickers = new(),
                Mensagem = "Arquivo processado, mas nenhuma cotação válida foi encontrada."
            }));

        await _cotacaoRepository.AdicionarVariosAsync(cotacoes);
        await _unitOfWork.CommitAsync();

        var tickers = cotacoes.Select(c => c.Ticker).Distinct().OrderBy(t => t).ToList();
        return Ok(ApiResult<InserirCotacoesResponse>.Success(new InserirCotacoesResponse
        {
            TotalInserido = cotacoes.Count,
            DataPregao = cotacoes[0].DataPregao,
            Tickers = tickers,
            Mensagem = $"{cotacoes.Count} cotações importadas com sucesso para {tickers.Count} tickers."
        }));
    }

    

    
    
    
    
    [HttpPost("inicializar")]
    [ProducesResponseType(typeof(ApiResult<InicializarSistemaResponse>), 200)]
    public async Task<IActionResult> Inicializar()
    {
        var resultado = await _adminAppService.InicializarSistemaAsync();
        return Ok(ApiResult<InicializarSistemaResponse>.Success(resultado));
    }

    

    
    
    
    
    
    [HttpPost("cesta")]
    [ProducesResponseType(typeof(ApiResult<CestaResponse>), 201)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    public async Task<IActionResult> CriarCesta([FromBody] CriarCestaRequest request)
    {
        var resultado = await _adminAppService.CriarCestaAsync(request);
        return CreatedAtAction(nameof(ObterCestaAtual), null, ApiResult<CestaResponse>.Success(resultado));
    }

    
    [HttpGet("cesta/atual")]
    [ProducesResponseType(typeof(ApiResult<CestaResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 404)]
    public async Task<IActionResult> ObterCestaAtual()
    {
        var cesta = await _adminAppService.ObterCestaAtivaAsync();
        return Ok(ApiResult<CestaResponse>.Success(cesta));
    }

    
    [HttpGet("cesta/historico")]
    [ProducesResponseType(typeof(ApiResult<List<CestaResponse>>), 200)]
    public async Task<IActionResult> ObterHistoricoCestas()
    {
        var historico = await _adminAppService.ObterHistoricoCestasAsync();
        return Ok(ApiResult<List<CestaResponse>>.Success(historico));
    }

    

    
    [HttpGet("conta-master/custodia")]
    [ProducesResponseType(typeof(ApiResult<ContaMasterResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 404)]
    public async Task<IActionResult> ObterContaMaster()
    {
        var master = await _adminAppService.ObterContaMasterAsync();
        return Ok(ApiResult<ContaMasterResponse>.Success(master));
    }
}
