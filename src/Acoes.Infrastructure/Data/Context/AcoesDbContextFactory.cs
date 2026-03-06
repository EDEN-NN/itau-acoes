using System.IO;
using Acoes.Infrastructure.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Acoes.Infrastructure.Data.Context;

public class AcoesDbContextFactory : IDesignTimeDbContextFactory<AcoesDbContext>
{
    public AcoesDbContext CreateDbContext(string[] args)
    {
        
        var connectionString = "Server=127.0.0.1;Port=3307;Database=ItauAcoesDb;Uid=root;Pwd=masterkey;";

        var builder = new DbContextOptionsBuilder<AcoesDbContext>();
        builder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));

        return new AcoesDbContext(builder.Options);
    }
}
