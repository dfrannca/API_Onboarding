# Account Onboarding API

API REST para criação, consulta, atualização e exclusão lógica de contas de clientes. Desenvolvida com ASP.NET Core e .NET 8, usando PostgreSQL para persistência.

## Tecnologias

- ASP.NET Core / .NET 8
- Entity Framework Core 8
- PostgreSQL 16
- xUnit, Moq e SQLite em memória para testes

## Pré-requisitos

- .NET 8 SDK
- Docker Compose ou uma instância PostgreSQL 16

## Executar localmente

Na raiz do repositório, inicie o banco:

```powershell
docker compose up -d postgres
```

A conexão padrão está configurada em `appsettings.json` para `localhost:5432`, banco `account_onboarding`, usuário `postgres`. Para apontar para outro banco, defina a variável de ambiente `ConnectionStrings__Accounts`:

```powershell
$env:ConnectionStrings__Accounts = "Host=localhost;Port=5432;Database=account_onboarding;Username=postgres;Password=sua-senha"
```

Restaure a ferramenta local do Entity Framework Core e aplique as migrations:

```powershell
dotnet tool restore
dotnet tool run dotnet-ef database update --project src/AccountOnboarding.Api --startup-project src/AccountOnboarding.Api
```

Inicie a API:

```powershell
dotnet run --project src/AccountOnboarding.Api --launch-profile https
```

A API ficará disponível em `https://localhost:7230` e `http://localhost:5227`. No ambiente `Development`, o Swagger estará em `/swagger`.

## Testes

Execute toda a suíte a partir da raiz:

```powershell
dotnet test tests/AccountOnboarding.Tests/AccountOnboarding.Api.UnitTest.csproj
```

Somente os testes de serviço:

```powershell
dotnet test tests/AccountOnboarding.Tests/AccountOnboarding.Api.UnitTest.csproj --filter "FullyQualifiedName~Services"
```

Os testes de integração usam SQLite em memória e fazem requisições HTTP para a API. Não precisam do PostgreSQL nem do Docker.

## Endpoints

Base URL local: `https://localhost:7230`.

| Método | Rota | Descrição |
| --- | --- | --- |
| `POST` | `/api/v1/accounts` | Cria uma conta |
| `GET` | `/api/v1/accounts/{id}` | Consulta uma conta pelo ID |
| `GET` | `/api/v1/accounts?cpf={cpf}&status={status}` | Lista contas, com filtros opcionais |
| `PUT` | `/api/v1/accounts/{id}` | Atualiza uma conta |
| `DELETE` | `/api/v1/accounts/{id}?expectedVersion={version}` | Exclui logicamente uma conta |
| `GET` | `/api/v1/accounts/{id}/history` | Consulta o histórico de auditoria |

### Criar conta

`POST /api/v1/accounts`

Body:

```json
{
  "holderName": "Ana Silva",
  "cpf": "529.982.247-25",
  "status": "Active"
}
```

`status` aceita `Active` ou `Inactive` e assume `Active` quando omitido. O CPF pode ser enviado formatado ou com 11 dígitos; a API o valida e normaliza antes de salvar.

### Consultar conta por ID

`GET /api/v1/accounts/{id}`

Exemplo:

```text
GET https://localhost:7230/api/v1/accounts/c5c4ad28-a0dc-4a9b-8940-28fc8643934d
```

Essa rota recebe o ID como segmento do caminho. `GET /api/v1/accounts?id={id}` não filtra pelo ID; a rota de listagem aceita apenas `cpf` e `status`. Contas excluídas logicamente não aparecem e a consulta pelo ID delas retorna `404 Not Found`.

### Listar e filtrar contas

`GET /api/v1/accounts`

Os filtros são opcionais e podem ser usados juntos ou separados:

```text
GET https://localhost:7230/api/v1/accounts?cpf=529.982.247-25&status=Active
```

Uma busca válida sem correspondências retorna `200 OK` com `data: []`. A listagem não inclui contas excluídas logicamente.

### Atualizar conta

`PUT /api/v1/accounts/{id}`

Body:

```json
{
  "holderName": "Ana Souza",
  "cpf": "52998224725",
  "status": "Inactive",
  "expectedVersion": 1
}
```

Envie a versão atual da conta em `expectedVersion`. A API verifica primeiro se o ID existe e não foi excluído; depois valida a versão e o CPF. Um ID inexistente ou excluído retorna `404 Not Found`; uma versão desatualizada retorna `409 Conflict`.

### Excluir conta

`DELETE /api/v1/accounts/{id}?expectedVersion={version}`

Exemplo:

```text
DELETE https://localhost:7230/api/v1/accounts/c5c4ad28-a0dc-4a9b-8940-28fc8643934d?expectedVersion=2
```

Não há body. A exclusão é lógica: define `DeletedAtUtc`, incrementa a versão e registra a operação. O CPF de uma conta excluída pode ser usado para criar uma nova conta; cada conta mantém seu próprio ID e histórico.

### Consultar histórico

`GET /api/v1/accounts/{id}/history`

Exemplo:

```text
GET https://localhost:7230/api/v1/accounts/c5c4ad28-a0dc-4a9b-8940-28fc8643934d/history
```

O histórico inclui operações `Created`, `Updated` e `Deleted`, com seus horários em UTC. Também é possível consultá-lo para uma conta excluída logicamente.

## Formato das respostas

As respostas de sucesso usam o envelope:

```json
{
  "status": 200,
  "title": "Success",
  "message": "Conta consultada com sucesso.",
  "data": {
    "id": "c5c4ad28-a0dc-4a9b-8940-28fc8643934d",
    "holderName": "Ana Silva",
    "cpf": "***.***.***-25",
    "status": "Active",
    "createdAtUtc": "2026-09-30T07:03:01.197527+00:00",
    "updatedAtUtc": null,
    "deletedAtUtc": null,
    "version": 1
  }
}
```

O CPF é mascarado nas respostas. A listagem retorna um array em `data`; quando não há resultados, o array fica vazio.

Erros de domínio também retornam `status`, `title` e `message`. Os principais códigos são `400` para CPF inválido, `404` para conta não encontrada e `409` para CPF duplicado ou conflito de versão.

## Persistência e regras

- O CPF normalizado tem índice único parcial: não pode se repetir entre contas ativas, mas pode ser reutilizado depois de uma exclusão lógica.
- Conta, auditoria e evento de integração são persistidos em uma única operação de `SaveChangesAsync`.
- As alterações usam concorrência otimista por `Version` e `expectedVersion`.
- Os eventos de integração ficam na tabela `account_integration_outbox`. A publicação atual é simulada; um worker pode publicar os eventos pendentes em um broker e preencher `PublishedAtUtc` após a confirmação.
- Consultas por ID usam cache em memória. Criação preenche o cache; atualização e exclusão invalidam a entrada. O cache atual é local à instância da API.
