using System.Threading.Tasks;
using Acoes.Application.Common;
using Acoes.Application.DTOs;
using Acoes.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Acoes.Api.Controllers;

[ApiController]
[Route("api/motor")]
[Produces("application/json")]
public class RebalanceamentoController : ControllerBase
{
    private readonly RebalanceamentoAppService _rebalanceamentoService;

    public RebalanceamentoController(RebalanceamentoAppService rebalanceamentoService)
    {
        _rebalanceamentoService = rebalanceamentoService;
    }

    
    
    
    
    
    
    [HttpPost("rebalancear")]
    [ProducesResponseType(typeof(ApiResult<RebalancearResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 404)]
    [ProducesResponseType(typeof(ApiResult<object>), 422)]
    public async Task<IActionResult> Rebalancear([FromBody] RebalancearRequest request)
    {
        var resultado = await _rebalanceamentoService.ExecutarRebalanceamentoAsync(request);
        return Ok(ApiResult<RebalancearResponse>.Success(resultado));
    }
}
