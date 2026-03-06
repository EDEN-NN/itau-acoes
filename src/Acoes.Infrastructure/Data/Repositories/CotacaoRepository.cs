using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acoes.Domain.Entities;
using Acoes.Domain.Interfaces.Repositories;
using Acoes.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Acoes.Infrastructure.Data.Repositories;

public class CotacaoRepository : ICotacaoRepository
{
    private readonly AcoesDbContext _context;

    public CotacaoRepository(AcoesDbContext context)
    {
        _context = context;
    }

    public async Task<Cotacao?> ObterPorTickerEDataAsync(string ticker, DateTime dataPregao)
    {
        return await _context.Cotacoes
            .FirstOrDefaultAsync(c => c.Ticker == ticker && c.DataPregao == dataPregao.Date);
    }

    public async Task<Cotacao?> ObterMaisRecentePorTickerAsync(string ticker)
    {
        return await _context.Cotacoes
            .Where(c => c.Ticker == ticker)
            .OrderByDescending(c => c.DataPregao)
            .FirstOrDefaultAsync();
    }

    public async Task AdicionarVariosAsync(IEnumerable<Cotacao> cotacoes)
    {
        await _context.Cotacoes.AddRangeAsync(cotacoes);
    }
}
