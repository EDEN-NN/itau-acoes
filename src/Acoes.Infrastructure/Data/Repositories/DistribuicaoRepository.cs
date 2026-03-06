using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acoes.Domain.Entities;
using Acoes.Domain.Interfaces.Repositories;
using Acoes.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Acoes.Infrastructure.Data.Repositories;

public class DistribuicaoRepository : IDistribuicaoRepository
{
    private readonly AcoesDbContext _context;

    public DistribuicaoRepository(AcoesDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Distribuicao>> ObterPorCustodiaFilhoteIdAsync(long custodiaFilhoteId)
    {
        return await _context.Distribuicoes
            .Where(d => d.CustodiaFilhoteId == custodiaFilhoteId)
            .ToListAsync();
    }

    public async Task AdicionarAsync(Distribuicao distribuicao)
    {
        await _context.Distribuicoes.AddAsync(distribuicao);
    }

    public async Task AdicionarVariosAsync(IEnumerable<Distribuicao> distribuicoes)
    {
        await _context.Distribuicoes.AddRangeAsync(distribuicoes);
    }
}
