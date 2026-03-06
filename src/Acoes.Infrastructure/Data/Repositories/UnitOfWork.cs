using System.Threading.Tasks;
using Acoes.Domain.Interfaces.Repositories;
using Acoes.Infrastructure.Data.Context;

namespace Acoes.Infrastructure.Data.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AcoesDbContext _context;

    public UnitOfWork(AcoesDbContext context)
    {
        _context = context;
    }

    public async Task<bool> CommitAsync()
    {
        return await _context.SaveChangesAsync() > 0;
    }
}
