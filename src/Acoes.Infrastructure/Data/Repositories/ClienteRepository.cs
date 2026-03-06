using System.Collections.Generic;
using System.Threading.Tasks;
using Acoes.Domain.Entities;
using Acoes.Domain.Interfaces.Repositories;
using Acoes.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;

namespace Acoes.Infrastructure.Data.Repositories;

public class ClienteRepository : IClienteRepository
{
    private readonly AcoesDbContext _context;

    public ClienteRepository(AcoesDbContext context)
    {
        _context = context;
    }

    public async Task<Cliente?> ObterPorIdAsync(long id)
    {
        return await _context.Clientes
            .Include(c => c.ContaGrafica)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<Cliente?> ObterPorCpfAsync(string cpf)
    {
        return await _context.Clientes
            .Include(c => c.ContaGrafica)
            .FirstOrDefaultAsync(c => c.Cpf == cpf);
    }

    public async Task<IEnumerable<Cliente>> ObterAtivosAsync()
    {
        return await _context.Clientes
            .Include(c => c.ContaGrafica)
            .Where(c => c.Ativo)
            .ToListAsync();
    }

    public async Task AdicionarAsync(Cliente cliente)
    {
        await _context.Clientes.AddAsync(cliente);
    }

    public Task AtualizarAsync(Cliente cliente)
    {
        _context.Clientes.Update(cliente);
        return Task.CompletedTask;
    }
}
