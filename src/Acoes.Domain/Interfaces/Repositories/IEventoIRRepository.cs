using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Acoes.Domain.Entities;

namespace Acoes.Domain.Interfaces.Repositories;

public interface IEventoIRRepository
{
    Task AdicionarAsync(EventoIR eventoIr);
    Task AtualizarAsync(EventoIR eventoIr);
    Task<IEnumerable<EventoIR>> ObterVendasDoMesAsync(long clienteId, int ano, int mes);
}
