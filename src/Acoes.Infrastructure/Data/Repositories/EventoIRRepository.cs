using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Acoes.Domain.Entities;
using Acoes.Domain.Enums;
using Acoes.Domain.Interfaces.Repositories;
using Acoes.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Acoes.Infrastructure.Data.Repositories;

public class EventoIRRepository : IEventoIRRepository
{
    private readonly AcoesDbContext _context;

    public EventoIRRepository(AcoesDbContext context)
    {
        _context = context;
    }

    public async Task AdicionarAsync(EventoIR eventoIr)
    {
        await _context.EventosIR.AddAsync(eventoIr);
    }

    public Task AtualizarAsync(EventoIR eventoIr)
    {
        _context.EventosIR.Update(eventoIr);
        return Task.CompletedTask;
    }

    public async Task<IEnumerable<EventoIR>> ObterVendasDoMesAsync(long clienteId, int ano, int mes)
    {
        return await _context.EventosIR
            .Where(e => e.ClienteId == clienteId
                     && e.Tipo == TipoEventoIR.IR_VENDA
                     && e.DataEvento.Year == ano
                     && e.DataEvento.Month == mes)
            .ToListAsync();
    }
}
