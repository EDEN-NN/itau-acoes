using System.Threading.Tasks;
using Acoes.Application.Common;
using Acoes.Application.DTOs;
using Acoes.Application.Services;
using Microsoft.AspNetCore.Mvc;

namespace Acoes.Api.Controllers;

[ApiController]
[Route("api/motor")]
[Produces("application/json")]
public class MotorController : ControllerBase
{
    private readonly MotorCompraService _motorCompraService;

    public MotorController(MotorCompraService motorCompraService)
    {
        _motorCompraService = motorCompraService;
    }

    
    
    
    
    
    
    
    
    [HttpPost("executar-compra")]
    [ProducesResponseType(typeof(ApiResult<ExecutarCompraResponse>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 404)]
    [ProducesResponseType(typeof(ApiResult<object>), 409)]
    [ProducesResponseType(typeof(ApiResult<object>), 422)]
    public async Task<IActionResult> ExecutarCompra([FromBody] ExecutarCompraRequest request)
    {
        var resultado = await _motorCompraService.ExecutarCompraAsync(request.DataReferencia);
        return Ok(ApiResult<ExecutarCompraResponse>.Success(resultado));
    }
}
