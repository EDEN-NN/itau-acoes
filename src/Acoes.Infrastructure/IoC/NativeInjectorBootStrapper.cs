using Acoes.Application.Services;
using Acoes.Domain.Interfaces.Infrastructure;
using Acoes.Domain.Interfaces.Repositories;
using Acoes.Domain.Interfaces.Services;
using Acoes.Infrastructure.Data.Context;
using Acoes.Infrastructure.Data.Repositories;
using Acoes.Infrastructure.Messaging;
using Acoes.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Acoes.Infrastructure.IoC;

public static class NativeInjectorBootStrapper
{
    public static void RegisterServices(IServiceCollection services, IConfiguration configuration)
    {
        
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? "Server=127.0.0.1;Port=3307;Database=ItauAcoesDb;Uid=root;Pwd=masterkey;";

        services.AddDbContext<AcoesDbContext>(options =>
            options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

        
        services.AddScoped<IClienteRepository, ClienteRepository>();
        services.AddScoped<IContaGraficaRepository, ContaGraficaRepository>();
        services.AddScoped<ICustodiaRepository, CustodiaRepository>();
        services.AddScoped<ICestaRecomendacaoRepository, CestaRecomendacaoRepository>();
        services.AddScoped<IOrdemCompraRepository, OrdemCompraRepository>();
        services.AddScoped<IDistribuicaoRepository, DistribuicaoRepository>();
        services.AddScoped<IEventoIRRepository, EventoIRRepository>();
        services.AddScoped<ICotacaoRepository, CotacaoRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        
        services.AddSingleton<IKafkaProducerService, KafkaProducerService>();

        
        services.AddScoped<ICotaHistService, CotaHistService>();

        
        services.AddScoped<ClienteAppService>();
        services.AddScoped<AdminAppService>();
        services.AddScoped<MotorCompraService>();
        services.AddScoped<RebalanceamentoAppService>();
    }
}
