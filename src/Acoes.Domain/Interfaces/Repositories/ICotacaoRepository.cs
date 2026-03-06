using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Acoes.Domain.Entities;

namespace Acoes.Domain.Interfaces.Repositories;

public interface ICotacaoRepository
{
    Task<Cotacao?> ObterPorTickerEDataAsync(string ticker, DateTime dataPregao);
    Task<Cotacao?> ObterMaisRecentePorTickerAsync(string ticker);
    Task AdicionarVariosAsync(IEnumerable<Cotacao> cotacoes);
}
