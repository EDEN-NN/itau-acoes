namespace Acoes.Application.Exceptions;

public class ClienteCpfDuplicadoException : DomainException
{
    public ClienteCpfDuplicadoException(string cpf)
        : base($"Já existe um cliente cadastrado com o CPF {cpf}.", "CLIENTE_CPF_DUPLICADO", 400) { }
}

public class ValorMensalInvalidoException : DomainException
{
    public ValorMensalInvalidoException()
        : base("O valor mensal mínimo é de R$ 100,00.", "VALOR_MENSAL_INVALIDO", 400) { }
}

public class ClienteNaoEncontradoException : DomainException
{
    public ClienteNaoEncontradoException(long clienteId)
        : base($"Cliente com ID {clienteId} não encontrado.", "CLIENTE_NAO_ENCONTRADO", 404) { }
}

public class ClienteJaInativoException : DomainException
{
    public ClienteJaInativoException(long clienteId)
        : base($"O cliente {clienteId} já está inativo.", "CLIENTE_JA_INATIVO", 400) { }
}

public class CestaNaoEncontradaException : DomainException
{
    public CestaNaoEncontradaException()
        : base("Nenhuma cesta ativa encontrada.", "CESTA_NAO_ENCONTRADA", 404) { }
}

public class PercentuaisInvalidosException : DomainException
{
    public PercentuaisInvalidosException(decimal somaAtual)
        : base($"A soma dos percentuais deve ser exatamente 100%. Soma atual: {somaAtual}%.", "PERCENTUAIS_INVALIDOS", 400) { }
}

public class QuantidadeAtivosInvalidaException : DomainException
{
    public QuantidadeAtivosInvalidaException(int quantidadeInformada)
        : base($"A cesta deve conter exatamente 5 ativos. Quantidade informada: {quantidadeInformada}.", "QUANTIDADE_ATIVOS_INVALIDA", 400) { }
}

public class CompraJaExecutadaException : DomainException
{
    public CompraJaExecutadaException(string data)
        : base($"A compra programada já foi executada para a data {data}.", "COMPRA_JA_EXECUTADA", 409) { }
}

public class KafkaIndisponivelException : DomainException
{
    public KafkaIndisponivelException(string detalhe)
        : base($"Erro ao publicar no tópico Kafka: {detalhe}", "KAFKA_INDISPONIVEL", 500) { }
}
