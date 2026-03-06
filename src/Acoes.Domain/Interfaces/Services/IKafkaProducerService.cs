using System.Threading.Tasks;
using Acoes.Domain.Entities;

namespace Acoes.Domain.Interfaces.Services;

public interface IKafkaProducerService
{
    Task PublicarEventoIRAsync(EventoIR eventoIr);
}
