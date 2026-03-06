using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acoes.Domain.Entities;
using Acoes.Domain.Interfaces.Repositories;
using Acoes.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Acoes.Infrastructure.Data.Repositories;

public class CestaRecomendacaoRepository : ICestaRecomendacaoRepository
{
    private readonly AcoesDbContext _context;

    public CestaRecomendacaoRepository(AcoesDbContext context)
    {
        _context = context;
    }

    public async Task<CestaRecomendacao?> ObterCestaAtivaAsync()
    {
        return await _context.CestasRecomendacao
            .Include(c => c.ItensCesta)
            .FirstOrDefaultAsync(c => c.Ativa);
    }

    public async Task<IEnumerable<CestaRecomendacao>> ObterHistoricoAsync()
    {
        return await _context.CestasRecomendacao
            .Include(c => c.ItensCesta)
            .OrderByDescending(c => c.DataCriacao)
            .ToListAsync();
    }

    public async Task AdicionarAsync(CestaRecomendacao cesta)
    {
        await _context.CestasRecomendacao.AddAsync(cesta);
    }

    public Task AtualizarAsync(CestaRecomendacao cesta)
    {
        _context.CestasRecomendacao.Update(cesta);
        return Task.CompletedTask;
    }
}
