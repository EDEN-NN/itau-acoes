using System.Collections.Generic;
using System.Threading.Tasks;
using Acoes.Domain.Entities;

namespace Acoes.Domain.Interfaces.Infrastructure;

public interface ICotaHistService
{
    Task<IEnumerable<Cotacao>> ProcessarArquivoAsync(string caminhoArquivo);
}
