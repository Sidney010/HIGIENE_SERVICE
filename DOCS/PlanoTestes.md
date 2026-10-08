# Plano de Testes — MVP Sistema de Agendamento (C# / .NET)

| Campo | Valor |
| --- | --- |
| Projeto | Sistema de agendamento para barbearia, salão e manicure |
| Escopo | Fase 1 (MVP) |
| Versão do documento | 1.0 |
| Responsável pela qualidade | (preencher) |
| Referências | Documento de Requisitos (IDs RF, RN e RNF) e Documentação da API |

## 1. Objetivo

Garantir que o MVP entregue ao estabelecimento seja **correto, seguro e estável**, com foco especial nas regras que, se falharem, causam prejuízo direto: **agendamentos sobrepostos, preços e comissões errados, vazamento de dados entre estabelecimentos e falhas de LGPD**.

## 2. Escopo

**Dentro do escopo (MVP):** Cadastro, Agenda e Agendamento, Atendimento web do cliente, Financeiro básico (comanda, pagamento, desconto, comissão), Notificações de confirmação e lembrete, Usuários e Segurança, Relatórios básicos e isolamento multi-tenant.

**Fora do escopo (F2 e F3):** Caixa, estoque, lista de espera, sinal antecipado, bot de WhatsApp, pacotes e assinaturas, fidelidade, cobrança SaaS.

## 3. Estratégia de testes

A estratégia segue a **pirâmide de testes**: muitos testes rápidos na base e poucos testes lentos no topo.

| Nível | O que valida | Ferramentas | Esforço | Meta |
| --- | --- | --- | --- | --- |
| Unitário | Regras de domínio isoladas (sobreposição, status, preço congelado, comissão) | xUnit, FluentAssertions, NSubstitute, Bogus | 60% | 90% de cobertura no domínio |
| Integração | Repositórios, EF Core, transações, migrations, constraints | xUnit, Testcontainers (banco real), Respawn | 20% | Todos os repositórios e consultas críticas |
| API (componente) | Rotas, autenticação, validação, status HTTP, contratos JSON | WebApplicationFactory, xUnit | 15% | 100% das rotas do MVP |
| Ponta a ponta (E2E) | Jornadas completas pela interface web | Playwright para .NET | 5% | 5 a 8 jornadas críticas |
| Não funcional | Desempenho, segurança, concorrência | k6 ou NBomber, OWASP ZAP | Por release | Metas da seção 9 |
| Exploratório e aceite | Usabilidade e fluxo real com o dono do estabelecimento | Sessões guiadas | Por release | Aceite assinado |

**Princípios:**

- Regras de negócio ficam em classes de domínio puras, sem dependência de banco, para testar rápido.
- Testes de integração usam **banco real em contêiner** (mesmo SGBD de produção), nunca banco em memória, porque constraints e concorrência se comportam de forma diferente.
- Todo bug encontrado vira um teste automatizado antes da correção (teste de regressão).
- Relógio abstraído (`IClock` ou `TimeProvider`) para testar prazos de cancelamento, antecedência e lembretes sem esperar o tempo passar.

## 4. Ferramentas e stack de teste

| Finalidade | Ferramenta |
| --- | --- |
| Framework de testes | xUnit |
| Asserções legíveis | FluentAssertions |
| Mocks | NSubstitute (ou Moq) |
| Dados falsos | Bogus |
| Banco para integração | Testcontainers (PostgreSQL ou SQL Server) |
| Limpeza entre testes | Respawn |
| Testes de API | Microsoft.AspNetCore.Mvc.Testing (WebApplicationFactory) |
| Cobertura | coverlet e ReportGenerator |
| Testes de mutação (opcional) | Stryker.NET nas regras de agendamento |
| E2E | Playwright para .NET |
| Carga | k6 ou NBomber |
| Segurança | OWASP ZAP, dotnet list package --vulnerable |
| Qualidade de código | SonarQube ou SonarCloud |
| Gestão de casos e defeitos | GitHub Issues, Azure DevOps ou Jira |

**Estrutura sugerida da solução:**

```
src/
  Agenda.Domain/
  Agenda.Application/
  Agenda.Infrastructure/
  Agenda.Api/
tests/
  Agenda.Domain.Tests/          (unitários)
  Agenda.Application.Tests/     (unitários de casos de uso)
  Agenda.Integration.Tests/     (banco real)
  Agenda.Api.Tests/             (WebApplicationFactory)
  Agenda.E2E.Tests/             (Playwright)
  Agenda.Performance.Tests/     (k6 ou NBomber)
```

## 5. Ambientes

| Ambiente | Uso | Dados |
| --- | --- | --- |
| Local (dev) | Desenvolvimento e testes unitários | Contêineres locais, dados gerados |
| CI | Execução automática a cada push | Contêineres descartáveis |
| Homologação | Testes de aceite com o estabelecimento | Dados fictícios realistas |
| Produção | Smoke test pós-deploy | Dados reais, somente leitura nos testes |

**Regra:** nunca usar dados reais de clientes fora de produção (LGPD).

## 6. Dados de teste

- **Dois estabelecimentos** de teste, para validar o isolamento entre tenants.
- **Uma unidade** por estabelecimento, com horário 09:00 às 19:00.
- **Profissionais:** 1 barbeiro (corte e barba), 1 cabeleireira (corte, coloração), 1 manicure, 1 profissional inativo.
- **Serviços:** Corte (30 min, R$ 40), Barba (20 min, R$ 30), Coloração (90 min, R$ 150), Manicure (45 min, R$ 35).
- **Clientes:** 1 comum, 1 com no-shows, 1 sem consentimento LGPD, 1 inativo.
- **Usuários:** um por perfil (Administrador, Recepção, Profissional, Cliente).
- Dados gerados por **builders** (padrão Object Mother ou Bogus) para evitar massa de dados frágil.

## 7. Critérios

**Entrada (iniciar testes de uma feature):** código revisado e integrado, build verde, ambiente disponível, requisitos e critérios de aceite definidos.

**Saída (liberar uma versão):**

- 100% dos casos de prioridade Crítica e Alta executados e aprovados.
- Zero defeitos abertos de severidade Crítica ou Alta.
- Cobertura de linhas no domínio de 90% ou mais, e geral de 75% ou mais.
- Testes de concorrência aprovados.
- Nenhuma vulnerabilidade Alta ou Crítica nas dependências.
- Aceite do estabelecimento nas jornadas principais.

## 8. Casos de teste por módulo

**Legenda:** U = unitário, I = integração, A = API, E = E2E. Prioridade: C = Crítica, A = Alta, M = Média.

### 8.1 Cadastro

| ID | Cenário | Resultado esperado | Nível | Prior. | Req. |
| --- | --- | --- | --- | --- | --- |
| CT-CAD-01 | Criar cliente com dados válidos e consentimento LGPD | 201 e cliente persistido | A | A | RF-CAD-01, 02 |
| CT-CAD-02 | Criar cliente sem consentimento LGPD | 422 com código CAD\_LGPD\_OBRIGATORIO | A | C | RF-CAD-02 |
| CT-CAD-03 | Criar cliente com telefone já existente no mesmo estabelecimento | 409 CAD\_TELEFONE\_DUPLICADO | A | A | RN-CAD-02 |
| CT-CAD-04 | Mesmo telefone em outro estabelecimento | 201 (permitido) | I | A | RN-CAD-02 |
| CT-CAD-05 | Telefone com formato inválido | 400 com detalhe do campo | U | M | RF-CAD-01 |
| CT-CAD-06 | Inativar cliente com histórico | 204; registro continua no banco com flag inativo | I | C | RN-CAD-01 |
| CT-CAD-07 | Habilitar serviço para profissional com preço próprio | Preço próprio é usado no agendamento | U | A | RF-CAD-05 |
| CT-CAD-08 | Criar serviço com duração zero ou preço negativo | 400 | U | M | RF-CAD-04 |
| CT-CAD-09 | Inativar profissional com agendamentos futuros | 409 listando os agendamentos afetados | A | A | RF-CAD-03 |

### 8.2 Agenda e agendamento (módulo mais crítico)

| ID | Cenário | Resultado esperado | Nível | Prior. | Req. |
| --- | --- | --- | --- | --- | --- |
| CT-AGE-01 | Agendar em horário livre dentro da disponibilidade | 201, status PENDENTE, fim = início + soma das durações | U, A | C | RF-AGE-04 |
| CT-AGE-02 | Agendar horário que sobrepõe outro do mesmo profissional | 409 AGE\_HORARIO\_OCUPADO | U, A | C | RN-AGE-01 |
| CT-AGE-03 | Agendamentos encostados (um termina às 10:00 e outro começa às 10:00) | Permitido | U | C | RN-AGE-01 |
| CT-AGE-04 | Agendar fora da disponibilidade do profissional | 422 AGE\_FORA\_DISPONIBILIDADE | U | C | RN-AGE-02 |
| CT-AGE-05 | Agendar fora do horário da unidade | 422 | U | A | RN-AGE-02 |
| CT-AGE-06 | Agendar em período de bloqueio (folga ou almoço) | 409 ou 422 | U | C | RF-AGE-02 |
| CT-AGE-07 | Dois serviços (corte e barba) | Duração = 50 min; dois itens com preço congelado | U | C | RN-AGE-03 |
| CT-AGE-08 | Intervalo de higienização configurado em 10 min | Fim do agendamento inclui o intervalo | U | A | RN-AGE-03 |
| CT-AGE-09 | Profissional não habilitado para o serviço | 422 AGE\_PROFISSIONAL\_NAO\_HABILITADO | U, A | C | RN-AGE-04 |
| CT-AGE-10 | Antecedência menor que a mínima | 422 AGE\_ANTECEDENCIA\_INVALIDA | U | A | RN-AGE-05 |
| CT-AGE-11 | Antecedência maior que a máxima | 422 AGE\_ANTECEDENCIA\_INVALIDA | U | M | RN-AGE-05 |
| CT-AGE-12 | Cliente com dois agendamentos no mesmo horário | 409 AGE\_CLIENTE\_CONFLITO | U, A | A | RN-AGE-06 |
| CT-AGE-13 | Alterar o preço do serviço após agendar | Agendamento existente mantém o preço antigo | I | C | RN-AGE-08 |
| CT-AGE-14 | Transição válida (Pendente, Confirmado, Em atendimento, Concluído) | Cada passo retorna 200 | U | C | RN-AGE-07 |
| CT-AGE-15 | Transição inválida (concluir um cancelado) | 409 AGE\_TRANSICAO\_INVALIDA | U, A | C | RN-AGE-07 |
| CT-AGE-16 | Cancelar dentro do prazo | Status CANCELADO, sem multa, horário liberado | U, A | A | RF-AGE-06 |
| CT-AGE-17 | Cancelar fora do prazo | Status CANCELADO, multa registrada conforme configuração | U | A | RF-AGE-06 |
| CT-AGE-18 | Remarcar para horário livre | Antigo REMARCADO, novo criado, vínculo entre eles | U, A | A | RF-AGE-06 |
| CT-AGE-19 | Remarcar para horário ocupado | 409 e agendamento original intacto (rollback) | I | C | RF-AGE-06 |
| CT-AGE-20 | Marcar no-show | Status NO\_SHOW e contador do cliente incrementado | U | A | RF-AGE-07 |
| CT-AGE-21 | Consultar horários livres com serviços múltiplos | Apenas janelas com duração total disponível | U, A | C | RF-AGE-03 |
| CT-AGE-22 | Horário livre muda de fuso (unidade em fuso diferente do servidor) | Horários exibidos no fuso da unidade; banco em UTC | U | A | RNF-06 |
| CT-AGE-23 | Mudança de horário de verão ou virada de dia | Duração e intervalo calculados corretamente | U | M | RNF-06 |

### 8.3 Atendimento ao cliente (self-service)

| ID | Cenário | Resultado esperado | Nível | Prior. | Req. |
| --- | --- | --- | --- | --- | --- |
| CT-CLI-01 | Solicitar código OTP com telefone válido | 202 e mensagem enviada | A | A | RF-CLI-03 |
| CT-CLI-02 | Verificar código correto | 200 com token de perfil Cliente | A | A | RF-CLI-03 |
| CT-CLI-03 | Código incorreto 5 vezes | 429 e bloqueio temporário | A | C | RNF-04 |
| CT-CLI-04 | Código expirado | 401 | U | A | RF-CLI-03 |
| CT-CLI-05 | Cliente tenta ver agendamento de outro cliente | 403 ou 404 | A | C | RN-SEG-02 |
| CT-CLI-06 | Jornada completa: escolher serviço, horário, confirmar | Agendamento criado e confirmação recebida | E | C | RF-CLI-01 |
| CT-CLI-07 | Cancelar e remarcar pelo celular (tela pequena) | Fluxo funcional e responsivo | E | A | RF-CLI-02 |

### 8.4 Financeiro

| ID | Cenário | Resultado esperado | Nível | Prior. | Req. |
| --- | --- | --- | --- | --- | --- |
| CT-FIN-01 | Abrir comanda a partir de agendamento em atendimento | Itens e preços congelados copiados | U, A | C | RF-FIN-01 |
| CT-FIN-02 | Abrir comanda avulsa (sem hora marcada) | Comanda criada com os serviços | A | A | RF-FIN-01 |
| CT-FIN-03 | Pagamento integral em Pix | Comanda PAGA e evento PagamentoConfirmado | U, A | C | RF-FIN-02 |
| CT-FIN-04 | Pagamento dividido (dinheiro e cartão) | Soma igual ao total; comanda PAGA | U | A | RF-FIN-02 |
| CT-FIN-05 | Pagamento maior que o saldo | 422 FIN\_VALOR\_EXCEDE\_SALDO | U | C | RF-FIN-02 |
| CT-FIN-06 | Mesmo pagamento enviado duas vezes (mesma Idempotency-Key) | Segunda chamada retorna o mesmo resultado, sem duplicar | I, A | C | RF-FIN-02 |
| CT-FIN-07 | Desconto sem motivo | 422 FIN\_DESCONTO\_SEM\_MOTIVO | U | A | RF-FIN-03 |
| CT-FIN-08 | Desconto por perfil sem permissão | 403 | A | A | RF-FIN-03 |
| CT-FIN-09 | Comissão percentual (40% sobre R$ 40) | R$ 16,00 | U | C | RF-FIN-04 |
| CT-FIN-10 | Comissão com percentual específico do profissional para o serviço | Usa o específico, não o padrão | U | A | RF-FIN-04 |
| CT-FIN-11 | Comissão de atendimento não pago | Não gera comissão | U | C | RN-FIN-01 |
| CT-FIN-12 | Comissão sobre comanda com desconto | Calculada sobre o valor efetivamente cobrado (regra definida e documentada) | U | A | RF-FIN-04 |
| CT-FIN-13 | Estorno sem motivo | 422 | U | A | RN-FIN-02 |
| CT-FIN-14 | Estorno aprovado | Pagamento estornado e comissão revertida | I | C | RN-FIN-02 |
| CT-FIN-15 | Arredondamento de centavos em divisão de comissão | Soma das partes igual ao total (uso de decimal) | U | A | RF-FIN-04 |

### 8.5 Notificações

| ID | Cenário | Resultado esperado | Nível | Prior. | Req. |
| --- | --- | --- | --- | --- | --- |
| CT-NOT-01 | Agendamento criado | Mensagem de confirmação enfileirada | I | A | RF-NOT-01 |
| CT-NOT-02 | Lembrete 24h antes | Enviado uma única vez (sem duplicar em reexecução do job) | I | A | RF-NOT-02 |
| CT-NOT-03 | Cliente responde Confirmar | Status CONFIRMADO | A | A | RF-NOT-02 |
| CT-NOT-04 | Falha do provedor de mensagens | Reenvio com política de retentativa; agendamento não é afetado | I | A | RNF-05 |
| CT-NOT-05 | Cliente sem consentimento de marketing | Não recebe mensagens promocionais | U | A | RN-NOT-01 |
| CT-NOT-06 | Webhook com assinatura inválida | 401, mensagem ignorada | A | C | RNF-04 |

### 8.6 Usuários e segurança

| ID | Cenário | Resultado esperado | Nível | Prior. | Req. |
| --- | --- | --- | --- | --- | --- |
| CT-SEG-01 | Login válido | 200 com tokens | A | C | RF-SEG-01 |
| CT-SEG-02 | Senha incorreta | 401 com mensagem genérica (sem revelar se o e-mail existe) | A | C | RF-SEG-01 |
| CT-SEG-03 | Excesso de tentativas de login | 429 | A | A | RNF-04 |
| CT-SEG-04 | Rota protegida sem token | 401 | A | C | RF-SEG-02 |
| CT-SEG-05 | Token expirado | 401; refresh gera novo token | A | A | RF-SEG-01 |
| CT-SEG-06 | Profissional acessa agenda de outro profissional | 403 | A | C | RF-SEG-03 |
| CT-SEG-07 | Profissional acessa comissões de outro | 403 | A | C | RF-SEG-03 |
| CT-SEG-08 | Recepção tenta rota exclusiva de administrador | 403 | A | C | RF-SEG-02 |
| CT-SEG-09 | Senha armazenada | Hash (bcrypt ou argon2); nunca em texto puro nem em log | I | C | RN-SEG-01 |

### 8.7 Isolamento multi-tenant

| ID | Cenário | Resultado esperado | Nível | Prior. | Req. |
| --- | --- | --- | --- | --- | --- |
| CT-TEN-01 | Usuário do estabelecimento A busca cliente do B por id | 404 | A | C | RF-SAAS-01 |
| CT-TEN-02 | Listagens (clientes, agendamentos, serviços) | Retornam só dados do próprio tenant | A, I | C | RF-SAAS-01 |
| CT-TEN-03 | Enviar estabelecimentoId no corpo da requisição | Valor ignorado; tenant vem sempre do token | A | C | RF-SAAS-01 |
| CT-TEN-04 | Consulta sem filtro de tenant no repositório | Impossível por design (filtro global do EF Core); teste garante a presença | I | C | RF-SAAS-01 |
| CT-TEN-05 | Relatórios e comissões | Nunca somam dados de outro tenant | A | C | RF-REL-01 |

### 8.8 Relatórios

| ID | Cenário | Resultado esperado | Nível | Prior. | Req. |
| --- | --- | --- | --- | --- | --- |
| CT-REL-01 | Faturamento por dia | Soma confere com pagamentos confirmados | I | A | RF-REL-01 |
| CT-REL-02 | Período sem dados | 200 com lista vazia (não erro) | A | M | RF-REL-01 |
| CT-REL-03 | Atendimentos por profissional | Contagem apenas de CONCLUÍDOS | I | A | RF-REL-02 |
| CT-REL-04 | Intervalo de datas inválido (fim antes do início) | 400 | A | M | RF-REL-01 |

## 9. Testes não funcionais

### 9.1 Concorrência (crítico)

| ID | Cenário | Resultado esperado |
| --- | --- | --- |
| CT-CON-01 | 20 requisições simultâneas para o mesmo horário e profissional | Exatamente 1 sucesso (201) e 19 conflitos (409) |
| CT-CON-02 | Cancelar e remarcar o mesmo agendamento ao mesmo tempo | Estado final consistente, sem duplicidade |
| CT-CON-03 | Dois pagamentos simultâneos na mesma comanda | Soma nunca excede o total |
| CT-CON-04 | Job de lembrete executado em duas instâncias | Cada lembrete enviado uma vez |

A garantia final deve estar também no **banco** (por exemplo, constraint de exclusão por intervalo no PostgreSQL ou índice único com controle de transação), e não apenas no código.

### 9.2 Desempenho

| ID | Cenário | Meta |
| --- | --- | --- |
| CT-PER-01 | Consultar horários livres com 10 profissionais e 6 meses de histórico | p95 abaixo de 2 s (RNF-01) |
| CT-PER-02 | 50 usuários simultâneos navegando e agendando | Taxa de erro abaixo de 1% |
| CT-PER-03 | Listagem paginada de 50 mil clientes | p95 abaixo de 500 ms |
| CT-PER-04 | Teste de pico de sábado (rajada de agendamentos) | Sem perda de dados e sem duplicidade |

### 9.3 Segurança (baseado no OWASP Top 10)

| Verificação | Como testar |
| --- | --- |
| Controle de acesso quebrado (BOLA/IDOR) | Trocar ids em todas as rotas com tokens de perfis e tenants diferentes |
| Injeção SQL | Payloads em filtros e buscas; uso exclusivo de consultas parametrizadas |
| XSS | Campos de observação e nome com scripts; verificação de encoding na saída |
| Autenticação | Brute force, token adulterado, token com algoritmo none, refresh reutilizado |
| Exposição de dados | Respostas sem campos sensíveis; mensagens de erro sem stack trace |
| Configuração | HTTPS obrigatório, cabeçalhos de segurança, CORS restrito |
| Dependências | dotnet list package --vulnerable no pipeline |
| Varredura automatizada | OWASP ZAP em homologação a cada release |
| LGPD | Termo exibido no primeiro agendamento, dados sensíveis acessíveis só por perfis autorizados |

### 9.4 Usabilidade e compatibilidade

- Recepção cria agendamento em até 4 cliques (RNF-03).
- Teste em Chrome, Safari, Edge e Firefox atuais, e em celulares Android e iPhone.
- Sessão de teste com a recepcionista e um profissional reais, com observação e anotação das dificuldades.

### 9.5 Resiliência e recuperação

- Queda do provedor de mensagens: o sistema continua agendando.
- Restauração de backup em homologação, validando integridade dos dados.
- Reinício da aplicação durante um pagamento: sem estado inconsistente.

## 10. Automação e CI/CD

**Pipeline sugerido (GitHub Actions, Azure DevOps ou GitLab CI):**

1. **Build** da solução e restauração de pacotes.
2. **Análise estática** e verificação de vulnerabilidades em dependências.
3. **Testes unitários** (rápidos, a cada commit).
4. **Testes de integração e de API** (contêineres de banco subindo no pipeline).
5. **Relatório de cobertura**, com falha do pipeline se ficar abaixo da meta.
6. **Deploy em homologação**.
7. **Testes E2E e smoke tests** em homologação.
8. **Aprovação manual** e deploy em produção.
9. **Smoke test pós-deploy** (health check, login, consulta de horários).

**Regras:** merge só com pipeline verde; testes instáveis (flaky) são corrigidos ou removidos em até uma semana; testes E2E rodam à noite e antes de cada release.

## 11. Exemplos de código

**Teste unitário da regra de sobreposição (xUnit e FluentAssertions):**

```csharp
public class AgendamentoTests
{
    [Theory]
    [InlineData("10:00", "10:30", true)]   // sobrepõe
    [InlineData("09:30", "10:00", false)]  // encosta antes
    [InlineData("10:30", "11:00", false)]  // encosta depois
    [InlineData("09:45", "10:15", true)]   // sobrepõe parcialmente
    public void Conflita_deve_respeitar_intervalo_semiaberto(
        string inicio, string fim, bool esperado)
    {
        var existente = AgendamentoBuilder.Novo()
            .Das("10:00").Ate("10:30").Build();
        var novo = AgendamentoBuilder.Novo()
            .Das(inicio).Ate(fim).Build();

        novo.Conflita(existente).Should().Be(esperado);
    }
}
```

**Teste de API com concorrência (WebApplicationFactory):**

```csharp
[Fact]
public async Task Vinte_requisicoes_no_mesmo_horario_geram_um_unico_agendamento()
{
    var client = _factory.CreateClientAutenticado(Perfil.Recepcao);
    var pedido = PedidoBuilder.HorarioLivre(profissionalId: 1);

    var tarefas = Enumerable.Range(0, 20)
        .Select(_ => client.PostAsJsonAsync("/api/v1/agendamentos", pedido));
    var respostas = await Task.WhenAll(tarefas);

    respostas.Count(r => r.StatusCode == HttpStatusCode.Created).Should().Be(1);
    respostas.Count(r => r.StatusCode == HttpStatusCode.Conflict).Should().Be(19);
}
```

## 12. Gestão de defeitos

| Severidade | Definição | Prazo de correção |
| --- | --- | --- |
| Crítica | Perda de dados, vazamento entre tenants, agendamento duplicado, cobrança errada | Imediato; bloqueia release |
| Alta | Funcionalidade principal indisponível, sem alternativa | Até 2 dias; bloqueia release |
| Média | Funcionalidade com alternativa ou impacto parcial | Na próxima sprint |
| Baixa | Visual, texto ou melhoria | Backlog |

**Ciclo de vida:** Novo, Confirmado, Em correção, Em reteste, Fechado (ou Reaberto).

**Campos mínimos do defeito:** título, passos para reproduzir, resultado esperado, resultado obtido, ambiente e versão, severidade, evidência (print ou log) e ID do caso de teste.

## 13. Riscos e mitigação

| Risco | Impacto | Mitigação |
| --- | --- | --- |
| Regras de fuso e horário mal tratadas | Horários errados | UTC no banco, TimeProvider, testes CT-AGE-22 e 23 |
| Condição de corrida em agendamentos | Overbooking | Transação com lock e constraint no banco, testes de concorrência |
| Vazamento de dados entre estabelecimentos | Crítico (LGPD e reputação) | Filtro global por tenant, testes CT-TEN |
| Dependência de provedor de WhatsApp ou SMS | Lembretes não enviados | Fila com retentativa e canal alternativo |
| Prazo curto e pouca cobertura de teste | Bugs em produção | Priorizar testes das regras críticas da seção 8.2 e 8.4 |
| Dados reais usados em homologação | Violação da LGPD | Massa fictícia, política de anonimização |
| Testes E2E instáveis | Perda de confiança no pipeline | Poucos E2E, esperas explícitas, correção rápida de flaky |

## 14. Cronograma sugerido

| Sprint | Foco de qualidade |
| --- | --- |
| 1 | Estrutura de testes, pipeline CI, testes de domínio de Cadastro e Segurança |
| 2 | Regras de Agenda (sobreposição, disponibilidade, status), testes de integração com banco real |
| 3 | Agendamento via API, concorrência, cancelamento e remarcação |
| 4 | Financeiro (comanda, pagamento, comissão), idempotência |
| 5 | Notificações, relatórios, multi-tenant, testes de segurança |
| 6 | E2E, desempenho, testes exploratórios, aceite com o estabelecimento, correção final |

## 15. Rastreabilidade (requisitos para testes)

| Módulo | Requisitos cobertos | Casos de teste |
| --- | --- | --- |
| Cadastro | RF-CAD-01 a 06, RN-CAD-01 e 02 | CT-CAD-01 a 09 |
| Agenda e agendamento | RF-AGE-01 a 07, RN-AGE-01 a 09 | CT-AGE-01 a 23, CT-CON-01 e 02 |
| Atendimento ao cliente | RF-CLI-01 a 03 | CT-CLI-01 a 07 |
| Financeiro | RF-FIN-01 a 05, RN-FIN-01 e 02 | CT-FIN-01 a 15, CT-CON-03 |
| Notificações | RF-NOT-01, 02, 05, RN-NOT-01 | CT-NOT-01 a 06, CT-CON-04 |
| Segurança | RF-SEG-01 a 03, RN-SEG-01 e 02 | CT-SEG-01 a 09 |
| Relatórios | RF-REL-01 e 02 | CT-REL-01 a 04 |
| Multi-tenant | RF-SAAS-01 e 02 | CT-TEN-01 a 05 |
| Não funcionais | RNF-01, 03, 04, 06 | CT-PER-01 a 04, seção 9.3 |

## 16. Modelos de documentação

**Modelo de caso de teste:**

| Campo | Descrição |
| --- | --- |
| ID | CT-MOD-00 |
| Título | Resumo do cenário |
| Pré-condições | Estado inicial e massa de dados |
| Passos | Ações numeradas |
| Resultado esperado | O que deve acontecer |
| Resultado obtido | Preenchido na execução |
| Status | Aprovado, Reprovado, Bloqueado |
| Requisito | ID do requisito relacionado |

**Modelo de relatório de execução (por release):**

- Versão testada e data.
- Total de casos planejados, executados, aprovados, reprovados e bloqueados.
- Cobertura de código (domínio e geral).
- Defeitos abertos por severidade.
- Resultados de desempenho e segurança.
- Riscos pendentes e recomendação: **liberar**, **liberar com ressalvas** ou **não liberar**.

**Checklist de smoke test pós-deploy:**

- Health check responde 200.
- Login de administrador funciona.
- Consulta de horários livres retorna dados.
- Criação e cancelamento de um agendamento de teste.
- Envio de uma notificação de teste.
