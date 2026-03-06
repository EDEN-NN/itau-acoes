using System.Collections.Generic;
using System.Threading.Tasks;
using Acoes.Domain.Entities;

namespace Acoes.Domain.Interfaces.Repositories;

public interface ICustodiaRepository
{
    Task<Custodia?> ObterPorContaIdETickerAsync(long contaGraficaId, string ticker);
    Task<IEnumerable<Custodia>> ObterPorContaIdAsync(long contaGraficaId);
    Task AdicionarAsync(Custodia custodia);
    Task AtualizarAsync(Custodia custodia);
}
