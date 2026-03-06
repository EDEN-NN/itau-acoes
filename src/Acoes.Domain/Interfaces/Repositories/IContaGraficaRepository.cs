using System.Collections.Generic;
using System.Threading.Tasks;
using Acoes.Domain.Entities;
using Acoes.Domain.Enums;

namespace Acoes.Domain.Interfaces.Repositories;

public interface IContaGraficaRepository
{
    Task<ContaGrafica?> ObterPorIdAsync(long id);
    Task<ContaGrafica?> ObterPorNumeroAsync(string numeroConta);
    Task<ContaGrafica?> ObterContaMasterAsync();
    Task<IEnumerable<ContaGrafica>> ObterFilhotesPorClienteIdAsync(long clienteId);
    Task AdicionarAsync(ContaGrafica contaGrafica);
}
