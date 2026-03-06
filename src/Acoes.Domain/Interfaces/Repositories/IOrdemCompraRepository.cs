using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Acoes.Domain.Entities;

namespace Acoes.Domain.Interfaces.Repositories;

public interface IOrdemCompraRepository
{
    Task<OrdemCompra?> ObterPorIdAsync(long id);
    Task<IEnumerable<OrdemCompra>> ObterPorDataAsync(DateTime dataExecucao);
    Task<bool> ExisteParaDataAsync(DateTime dataExecucao);
    Task AdicionarAsync(OrdemCompra ordem);
    Task AdicionarVariosAsync(IEnumerable<OrdemCompra> ordens);
}
