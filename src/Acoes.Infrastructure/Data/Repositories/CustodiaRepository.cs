using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acoes.Domain.Entities;
using Acoes.Domain.Interfaces.Repositories;
using Acoes.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Acoes.Infrastructure.Data.Repositories;

public class CustodiaRepository : ICustodiaRepository
{
    private readonly AcoesDbContext _context;

    public CustodiaRepository(AcoesDbContext context)
    {
        _context = context;
    }

    public async Task<Custodia?> ObterPorContaIdETickerAsync(long contaGraficaId, string ticker)
    {
        return await _context.Custodias
            .FirstOrDefaultAsync(c => c.ContaGraficaId == contaGraficaId && c.Ticker == ticker);
    }

    public async Task<IEnumerable<Custodia>> ObterPorContaIdAsync(long contaGraficaId)
    {
        return await _context.Custodias
            .Where(c => c.ContaGraficaId == contaGraficaId)
            .ToListAsync();
    }

    public async Task AdicionarAsync(Custodia custodia)
    {
        await _context.Custodias.AddAsync(custodia);
    }

    public Task AtualizarAsync(Custodia custodia)
    {
        _context.Custodias.Update(custodia);
        return Task.CompletedTask;
    }
}
