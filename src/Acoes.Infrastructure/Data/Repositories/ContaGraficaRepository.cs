using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acoes.Domain.Entities;
using Acoes.Domain.Enums;
using Acoes.Domain.Interfaces.Repositories;
using Acoes.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Acoes.Infrastructure.Data.Repositories;

public class ContaGraficaRepository : IContaGraficaRepository
{
    private readonly AcoesDbContext _context;

    public ContaGraficaRepository(AcoesDbContext context)
    {
        _context = context;
    }

    public async Task<ContaGrafica?> ObterPorIdAsync(long id)
    {
        return await _context.ContasGraficas
            .Include(c => c.Custodias)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<ContaGrafica?> ObterPorNumeroAsync(string numeroConta)
    {
        return await _context.ContasGraficas
            .Include(c => c.Custodias)
            .FirstOrDefaultAsync(c => c.NumeroConta == numeroConta);
    }

    public async Task<ContaGrafica?> ObterContaMasterAsync()
    {
        return await _context.ContasGraficas
            .Include(c => c.Custodias)
            .FirstOrDefaultAsync(c => c.Tipo == TipoContaGrafica.MASTER);
    }

    public async Task<IEnumerable<ContaGrafica>> ObterFilhotesPorClienteIdAsync(long clienteId)
    {
        return await _context.ContasGraficas
            .Include(c => c.Custodias)
            .Where(c => c.ClienteId == clienteId && c.Tipo == TipoContaGrafica.FILHOTE)
            .ToListAsync();
    }

    public async Task AdicionarAsync(ContaGrafica contaGrafica)
    {
        await _context.ContasGraficas.AddAsync(contaGrafica);
    }
}
