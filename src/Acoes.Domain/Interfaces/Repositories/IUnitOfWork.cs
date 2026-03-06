using System.Threading.Tasks;

namespace Acoes.Domain.Interfaces.Repositories;

public interface IUnitOfWork
{
    Task<bool> CommitAsync();
}
