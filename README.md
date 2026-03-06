# Acoes — Sistema de Compra Programada de Ações

API REST para compra programada e rebalanceamento automático de carteiras de ações (MVP), desenvolvida em **.NET 10** com arquitetura em camadas (DDD).

---

## Pré-requisitos

| Ferramenta                                                        | Versão mínima              |
| ----------------------------------------------------------------- | -------------------------- |
| [.NET SDK](https://dotnet.microsoft.com/download)                 | 10.0                       |
| [Docker Desktop](https://www.docker.com/products/docker-desktop/) | 4.x                        |
| Docker Compose                                                    | incluído no Docker Desktop |

---

## 1. Subir a infraestrutura

```bash
docker compose up -d
```

Isso inicia 4 containers:

| Container         | Serviço        | Porta   |
| ----------------- | -------------- | ------- |
| `acoes-mysql`     | MySQL 8        | `3307`  |
| `acoes-kafka`     | Kafka 7.4      | `9092`  |
| `acoes-zookeeper` | Zookeeper      | interno |
| `acoes-kafka-ui`  | Kafka UI (web) | `8080`  |

> **Kafka UI:** http://localhost:8080

---

## 2. Executar a API

```bash
dotnet run --project src/Acoes.Api/Acoes.Api.csproj
```

A API sobe em **http://localhost:5075**  
Swagger disponível em **http://localhost:5075/swagger**

---

## 3. Fluxo de uso (primeira vez)

### Passo 0 — Inicializar o sistema _(obrigatório uma vez)_

```http
POST /api/admin/inicializar
```

### Passo 0.5 — Importar cotações da B3 _(obrigatório para o motor de compra)_

1. Baixe o arquivo COTAHIST em: https://www.b3.com.br > Produtos e Serviços > COTAHIST
2. Extraia o `.TXT` e copie para a pasta `data/cotahist/` na raiz do projeto
3. Chame o endpoint:

```http
POST /api/admin/cotacoes/processar?nomeArquivo=COTD050326.TXT
```

> O caminho da pasta é configurável em `src/Acoes.Api/appsettings.json` → `CotaHistDirectory`

### Passo 1 — Criar a Cesta Top Five

```http
POST /api/admin/cesta
Content-Type: application/json

{
  "nome": "Top Five - Março 2026",
  "itens": [
    { "ticker": "PETR4", "percentual": 30 },
    { "ticker": "VALE3", "percentual": 25 },
    { "ticker": "ITUB4", "percentual": 20 },
    { "ticker": "BBDC4", "percentual": 15 },
    { "ticker": "WEGE3", "percentual": 10 }
  ]
}
```

### Passo 2 — Cadastrar cliente

```http
POST /api/clientes/aderir
Content-Type: application/json

{
  "nome": "João Silva",
  "cpf": "12345678901",
  "email": "joao@email.com",
  "valorMensal": 1500.00
}
```

### Passo 3 — Executar compra programada _(dias 5, 15 ou 25)_

```http
POST /api/motor/executar-compra
Content-Type: application/json

{ "dataReferencia": "2026-03-05" }
```

### Passo 4 — Consultar carteira do cliente

```http
GET /api/clientes/{id}/carteira
```

---

## Endpoints disponíveis

| Método   | Rota                               | Descrição                              |
| -------- | ---------------------------------- | -------------------------------------- |
| `POST`   | `/api/admin/inicializar`           | Cria Conta Master (1x por ambiente)    |
| `POST`   | `/api/admin/cotacoes/processar`    | Importa COTAHIST da pasta local        |
| `POST`   | `/api/admin/cotacoes/upload`       | Importa COTAHIST via upload de arquivo |
| `POST`   | `/api/admin/cesta`                 | Cria/atualiza cesta Top Five           |
| `GET`    | `/api/admin/cesta/atual`           | Retorna cesta ativa                    |
| `GET`    | `/api/admin/cesta/historico`       | Histórico de cestas                    |
| `GET`    | `/api/admin/conta-master/custodia` | Resíduos da Conta Master               |
| `POST`   | `/api/clientes/aderir`             | Adesão de novo cliente                 |
| `GET`    | `/api/clientes/{id}/carteira`      | Carteira e P&L do cliente              |
| `PUT`    | `/api/clientes/{id}/valor-mensal`  | Altera valor mensal                    |
| `DELETE` | `/api/clientes/{id}`               | Desativa cliente                       |
| `POST`   | `/api/motor/executar-compra`       | Executa compra programada              |
| `POST`   | `/api/motor/rebalancear`           | Executa rebalanceamento de cesta       |

---

## Executar testes

```bash
dotnet test
```

40 testes unitários cobrindo regras de negócio, motor de compra e rebalanceamento.

---

## Estrutura do projeto

```
itau-acoes/
├── src/
│   ├── Acoes.Api/              # Controllers, Middleware, Program.cs
│   ├── Acoes.Application/      # Services, DTOs, Exceptions
│   ├── Acoes.Domain/           # Entities, Enums, Interfaces
│   └── Acoes.Infrastructure/   # EF Core, Repositories, Kafka, COTAHIST parser
├── tests/
│   └── Acoes.Tests/            # Testes unitários (xUnit + NSubstitute)
├── docs/                       # Especificações de negócio e layout COTAHIST
├── data/
│   └── cotahist/               # ← Coloque os arquivos .TXT da B3 aqui
├── docker-compose.yml
└── Acoes.sln
```

---

## Configuração

| Chave (`appsettings.json`)            | Padrão                 | Descrição                   |
| ------------------------------------- | ---------------------- | --------------------------- |
| `ConnectionStrings:DefaultConnection` | MySQL local porta 3307 | String de conexão           |
| `Kafka:BootstrapServers`              | `localhost:9092`       | Broker Kafka                |
| `Kafka:Topics:IrEventos`              | `ir-eventos`           | Tópico de IR Dedo-Duro      |
| `CotaHistDirectory`                   | `/…/data/cotahist`     | Pasta com arquivos COTAHIST |
