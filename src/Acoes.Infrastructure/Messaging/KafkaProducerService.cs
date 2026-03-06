using System;
using System.Text.Json;
using System.Threading.Tasks;
using Acoes.Domain.Entities;
using Acoes.Domain.Interfaces.Services;
using Confluent.Kafka;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Acoes.Infrastructure.Messaging;

public class KafkaProducerService : IKafkaProducerService
{
    private readonly IProducer<Null, string> _producer;
    private readonly ILogger<KafkaProducerService> _logger;
    private readonly string _topicName;

    public KafkaProducerService(IConfiguration configuration, ILogger<KafkaProducerService> logger)
    {
        _logger = logger;
        
        var bootstrapServers = configuration["Kafka:BootstrapServers"] ?? "localhost:9092";
        _topicName = configuration["Kafka:Topics:IrEventos"] ?? "ir-eventos";

        var config = new ProducerConfig { BootstrapServers = bootstrapServers };
        _producer = new ProducerBuilder<Null, string>(config).Build();
    }

    public async Task PublicarEventoIRAsync(EventoIR eventoIr)
    {
        try
        {
            var eventPayload = new
            {
                ClienteId = eventoIr.ClienteId,
                TipoEvento = eventoIr.Tipo.ToString(),
                ValorBase = eventoIr.ValorBase,
                ValorIR = eventoIr.ValorIR,
                DataEvento = eventoIr.DataEvento
            };

            var message = JsonSerializer.Serialize(eventPayload);

            var deliveryResult = await _producer.ProduceAsync(_topicName, new Message<Null, string> { Value = message });
            
            _logger.LogInformation($"Evento IR {eventoIr.Id} publicado no topico {deliveryResult.TopicPartitionOffset}");
            
            
            eventoIr.MarcarComoPublicado();
        }
        catch (ProduceException<Null, string> e)
        {
            _logger.LogError($"Erro ao enviar evento para o Kafka: {e.Error.Reason}");
            throw;
        }
    }
}
