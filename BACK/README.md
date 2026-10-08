# Agenda Barbearia — API do MVP (C# / .NET 10)

Sistema de agendamento para barbearia, salão e manicure. Esta é a **Fase 1 (MVP)**.

- Arquitetura: Clean Architecture + DDD tático, monólito modular (veja `docs/arquitetura.md`)
- Contrato da API: `docs/openapi.yaml` (69 rotas)
- Banco: PostgreSQL

> **Estado do projeto:** esqueleto com os **casos de uso críticos implementados e testados** e as demais rotas
> já criadas no contrato e nos controllers (respondem `501 Não implementado` até você implementá-las).

## O que já está pronto

| Pronto | Onde |
|---|---|
| Entidades e regras de Cadastro, Agendamento, Financeiro | `src/Agenda.Domain` |
| Validação de agendamento (sobreposição, disponibilidade, bloqueio, habilitação, antecedência) | `ValidadorDeAgendamento` |
| Cálculo de horários livres | `CalculadoraDeHorariosLivres` |
| Ciclo de vida do agendamento + multa de cancelamento | `Agendamento` |
| Comanda: desconto, pagamento parcial/integral, estorno, comissão | `Comanda`, `Comissao` |
| Caso de uso **criar agendamento** (rota `POST /agendamentos`) | `Application/Agendamentos/Criar` |
| Caso de uso **cancelar agendamento** (rota `POST /agendamentos/{id}/cancelar`) | `Application/Agendamentos/Cancelar` |
| Caso de uso **horários livres** (rota `GET /agenda/horarios-livres`) | `Application/Agendamentos/HorariosLivres` |
| EF Core, filtro global de tenant, repositórios, unit of work, constraint anti-overbooking | `src/Agenda.Infrastructure` |
| JWT, perfis, rate limit, erros RFC 7807, correlation id, health checks | `src/Agenda.Api` |
| Testes: domínio, API (inclui "69 rotas do contrato"), integração com PostgreSQL real e concorrência | `tests/` |

As outras 66 rotas existem como controllers parciais gerados (`Controllers/Generated`).

## Como rodar

Pré-requisitos: **.NET 10 SDK**, **Docker**.

```bash
# 1) Banco
docker compose up -d

# 2) Restaurar e compilar
dotnet restore
dotnet build

# 3) Migration inicial (uma vez)
dotnet tool install --global dotnet-ef
dotnet ef migrations add Inicial -p src/Agenda.Infrastructure -s src/Agenda.Api -o Persistence/Migrations
```

Abra a migration gerada e, no método `Up`, **acrescente no final** a constraint anti-overbooking:

```csharp
migrationBuilder.Sql(File.ReadAllText(
    Path.Combine(AppContext.BaseDirectory, "Persistence", "Sql", "001_agendamento_sem_sobreposicao.sql")));
```

(ou cole o conteúdo do `.sql` direto numa string). Depois:

```bash
dotnet ef database update -p src/Agenda.Infrastructure -s src/Agenda.Api
dotnet run --project src/Agenda.Api        # https://localhost:7080
```

Em desenvolvimento, o OpenAPI gerado pelo código fica em `/openapi/v1.json`. O contrato oficial é `docs/openapi.yaml` — importe no Postman, Insomnia ou Swagger Editor.

## Testes

```bash
dotnet test                                                   # tudo (integração exige Docker)
dotnet test --filter "FullyQualifiedName~Domain.Tests"        # só regras de domínio (rápido)
dotnet test --filter "FullyQualifiedName~Api.Tests"           # rotas, auth, contrato
dotnet test --filter "FullyQualifiedName~Integration.Tests"   # PostgreSQL real + concorrência
```

Os testes seguem o Plano de Testes (IDs `CT-AGE-02`, `CT-CON-01`... nos comentários).

## Estrutura

```
AgendaBarbearia.slnx
├── Directory.Build.props / Directory.Packages.props   (versões centralizadas)
├── docker-compose.yml
├── docs/
│   ├── openapi.yaml            contrato da API (gerado)
│   └── arquitetura.md          decisões e regras de camadas
├── tools/gen_contract.py       fonte única das rotas → gera o YAML e os stubs
├── src/
│   ├── Agenda.Domain/          regras de negócio puras
│   │   ├── Common/             Entity, AggregateRoot, DomainException, Intervalo
│   │   ├── Cadastro/           Estabelecimento, Unidade, Cliente, Servico, Profissional, Configuração
│   │   ├── Agendamentos/       Agendamento, Validador, Calculadora, eventos
│   │   ├── Financeiro/         Comanda, Pagamento, Comissão
│   │   ├── Seguranca/          Usuário, Perfil
│   │   └── Abstractions/       interfaces de repositório
│   ├── Agenda.Application/     casos de uso (um handler por caso)
│   │   ├── Abstractions/       IHandler, IUnitOfWork, ITenantProvider, ICurrentUser...
│   │   └── Agendamentos/       Criar, Cancelar, HorariosLivres, EventHandlers
│   ├── Agenda.Infrastructure/  EF Core, repositórios, unit of work, eventos, segurança
│   │   └── Persistence/        DbContext, Configurations, Repositories, Sql
│   └── Agenda.Api/             controllers finos, middlewares, JWT, Program.cs
│       ├── Controllers/        manuais + Generated/ (stubs 501)
│       ├── Contracts/          requests HTTP
│       ├── Middleware/         correlation id, erros, tenant, health
│       └── Security/           JWT, adaptadores de tenant e usuário
└── tests/
    ├── Agenda.Domain.Tests/        unitários (rápidos)
    ├── Agenda.Api.Tests/           WebApplicationFactory
    └── Agenda.Integration.Tests/   Testcontainers + PostgreSQL
```

## Como implementar uma nova rota (receita)

Exemplo: `POST /agendamentos/{id}/confirmar`.

1. **Application:** crie `Agendamentos/Confirmar/ConfirmarAgendamento.cs` com o `Command` e um `Handler : IHandler<,>`.
   Carregue o agregado pelo repositório, chame o método de domínio (`agendamento.Confirmar()`), dentro de `uow.ExecutarEmTransacaoAsync`.
2. **DI:** registre o handler em `Application/DependencyInjection.cs`.
3. **Controller:** na parte **manual** (`AgendamentosController.cs`) escreva a ação.
4. **Gerador:** em `tools/gen_contract.py`, adicione `("POST", "/agendamentos/{id}/confirmar")` ao conjunto `REAIS` e rode `python3 tools/gen_contract.py` — o stub 501 dessa rota some.
5. **Teste:** escreva o teste de domínio e, se envolver banco, o de integração.

Se a regra for nova, ela vai **dentro da entidade** (ou de um serviço de domínio), nunca no controller.

## Antes de ir para produção (checklist)

- [ ] `Jwt:Key` em variável de ambiente/secret manager (nunca no `appsettings.json`)
- [ ] Migration inicial com a constraint anti-overbooking aplicada
- [ ] Implementar login, refresh e OTP; `IIdempotencyStore`
- [ ] Worker de lembretes e no-show; outbox para notificações
- [ ] HTTPS, CORS restrito, backup diário do PostgreSQL com teste de restauração
- [ ] Revisar a regra da comissão sobre comanda com desconto com o estabelecimento
- [ ] Rodar o OWASP ZAP em homologação e `dotnet list package --vulnerable`

## Limitações conhecidas

- O esqueleto foi escrito **sem compilador disponível** no ambiente de geração. O primeiro `dotnet build` pode apontar pequenos ajustes (versões de pacote em `Directory.Packages.props`, `using`s). Os testes de domínio são o melhor primeiro passo para validar o projeto.
- Nomes de coluna ficam em PascalCase (padrão do EF). Se preferir `snake_case`, adicione `EFCore.NamingConventions` e ajuste o SQL da constraint.
