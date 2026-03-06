using System.Collections.Generic;
using System.Threading.Tasks;
using Acoes.Domain.Entities;

namespace Acoes.Domain.Interfaces.Repositories;

public interface ICestaRecomendacaoRepository
{
    Task<CestaRecomendacao?> ObterCestaAtivaAsync();
    Task<IEnumerable<CestaRecomendacao>> ObterHistoricoAsync();
    Task AdicionarAsync(CestaRecomendacao cesta);
    Task AtualizarAsync(CestaRecomendacao cesta);
}
