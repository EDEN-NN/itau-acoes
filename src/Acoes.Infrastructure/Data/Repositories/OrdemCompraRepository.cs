using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acoes.Domain.Entities;
using Acoes.Domain.Interfaces.Repositories;
using Acoes.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Acoes.Infrastructure.Data.Repositories;

public class OrdemCompraRepository : IOrdemCompraRepository
{
    private readonly AcoesDbContext _context;

    public OrdemCompraRepository(AcoesDbContext context)
    {
        _context = context;
    }

    public async Task<OrdemCompra?> ObterPorIdAsync(long id)
    {
        return await _context.OrdensCompra
            .Include(o => o.Distribuicoes)
            .FirstOrDefaultAsync(o => o.Id == id);
    }

    public async Task<IEnumerable<OrdemCompra>> ObterPorDataAsync(DateTime dataExecucao)
    {
        return await _context.OrdensCompra
            .Where(o => o.DataExecucao.Date == dataExecucao.Date)
            .ToListAsync();
    }

    public async Task<bool> ExisteParaDataAsync(DateTime dataExecucao)
    {
        return await _context.OrdensCompra
            .AnyAsync(o => o.DataExecucao.Date == dataExecucao.Date);
    }

    public async Task AdicionarAsync(OrdemCompra ordem)
    {
        await _context.OrdensCompra.AddAsync(ordem);
    }

    public async Task AdicionarVariosAsync(IEnumerable<OrdemCompra> ordens)
    {
        await _context.OrdensCompra.AddRangeAsync(ordens);
    }
}
