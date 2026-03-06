using System.Threading.Tasks;
using Acoes.Application.Common;
using Acoes.Application.DTOs;
using Acoes.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Acoes.Api.Controllers;

[ApiController]
[Route("api/clientes")]
[Produces("application/json")]
public class ClientesController : ControllerBase
{
    private readonly ClienteAppService _clienteAppService;

    public ClientesController(ClienteAppService clienteAppService)
    {
        _clienteAppService = clienteAppService;
    }

    
    
    
    
    
    [HttpPost("adesao")]
    [ProducesResponseType(typeof(ApiResult<AdesaoResponse>), 201)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 409)]
    public async Task<IActionResult> Aderir([FromBody] AdesaoRequest request)
    {
        var resultado = await _clienteAppService.AderirAsync(request);
        return CreatedAtAction(
            nameof(ObterCarteira),
            new { clienteId = resultado.ClienteId },
            ApiResult<AdesaoResponse>.Success(resultado));
    }

    
    [HttpPut("{clienteId:long}/valor-mensal")]
    [ProducesResponseType(typeof(ApiResult<object>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 404)]
    public async Task<IActionResult> AlterarValorMensal(long clienteId, [FromBody] AlterarValorMensalRequest request)
    {
        await _clienteAppService.AlterarValorMensalAsync(clienteId, request.NovoValor);
        return Ok(ApiResult<object>.Success(new
        {
            clienteId,
            mensagem = "Valor mensal atualizado com sucesso."
        }));
    }

    
    [HttpPost("{clienteId:long}/saida")]
    [ProducesResponseType(typeof(ApiResult<object>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 404)]
    public async Task<IActionResult> Sair(long clienteId)
    {
        await _clienteAppService.SairDoProdutoAsync(clienteId);
        return Ok(ApiResult<object>.Success(new
        {
            clienteId,
            ativo = false,
            mensagem = "Adesão encerrada. Sua posição em custódia foi mantida."
        }));
    }

    
    [HttpGet("{clienteId:long}/carteira")]
    [ProducesResponseType(typeof(ApiResult<CarteiraResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 404)]
    public async Task<IActionResult> ObterCarteira(long clienteId)
    {
        var carteira = await _clienteAppService.ObterCarteiraAsync(clienteId);
        return Ok(ApiResult<CarteiraResponse>.Success(carteira));
    }
}
