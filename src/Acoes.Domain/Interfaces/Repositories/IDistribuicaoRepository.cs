using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Acoes.Domain.Entities;

namespace Acoes.Domain.Interfaces.Repositories;

public interface IDistribuicaoRepository
{
    Task<IEnumerable<Distribuicao>> ObterPorCustodiaFilhoteIdAsync(long custodiaFilhoteId);
    Task AdicionarAsync(Distribuicao distribuicao);
    Task AdicionarVariosAsync(IEnumerable<Distribuicao> distribuicoes);
}
