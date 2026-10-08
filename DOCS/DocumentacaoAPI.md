# Documentação da API — MVP Sistema de Agendamento (ASP.NET Core)

| Campo | Valor |
| --- | --- |
| Versão da API | v1 |
| Estilo | REST sobre HTTPS, JSON (UTF-8) |
| URL base | `https://{host}/api/v1` |
| Autenticação | JWT Bearer |
| Escopo | Fase 1 (MVP) |

## 1. Convenções gerais

| Tema | Padrão |
| --- | --- |
| Datas e horas | ISO 8601 em **UTC** (ex.: `2026-10-15T13:30:00Z`). A interface converte para o fuso da unidade. |
| Datas simples | `YYYY-MM-DD` |
| Dinheiro | Número decimal com 2 casas (ex.: `40.00`), moeda BRL. No C#, sempre `decimal`. |
| Identificadores | Inteiros (`long`) |
| Multi-tenant | O estabelecimento vem **sempre do token**. Nunca é enviado no corpo. Em rotas públicas, usa-se o cabeçalho `X-Estabelecimento: {slug}`. |
| Paginação | `?pagina=1&tamanho=20` (tamanho máximo 100) |
| Ordenação | `?ordenarPor=campo&direcao=asc` |
| Criação (POST) | Retorna `201 Created` com cabeçalho `Location` apontando para o novo recurso |
| Idempotência | Cabeçalho `Idempotency-Key` (GUID) em `POST /agendamentos` e `POST /comandas/{id}/pagamentos`. Repetição com a mesma chave devolve o resultado original. |
| Exclusão | `DELETE` inativa o registro (soft delete). Nada com histórico é apagado. |
| Rastreio | Toda resposta traz `X-Correlation-Id` para suporte e logs |

**Envelope de listas paginadas:**

```json
{
  "itens": [ ],
  "pagina": 1,
  "tamanho": 20,
  "totalItens": 134,
  "totalPaginas": 7
}
```

## 2. Autenticação e autorização

**Fluxo da equipe:** `POST /auth/login` devolve `accessToken` (validade de 15 min) e `refreshToken` (7 dias, rotativo, uso único). O cliente envia `Authorization: Bearer {accessToken}`.

**Fluxo do cliente final:** identificação por telefone com código OTP (`/auth/cliente/...`), gerando token com perfil Cliente.

**Claims do token:** `sub` (id do usuário), `estabelecimento_id`, `perfil`, `profissional_id` ou `cliente_id` quando aplicável.

| Perfil | Descrição |
| --- | --- |
| **Admin** | Dono ou gerente. Acesso total ao estabelecimento. |
| **Recepcao** | Opera agenda, clientes e comandas. Sem acesso a configurações, usuários e estornos. |
| **Profissional** | Vê apenas a própria agenda e as próprias comissões. Inicia e conclui os próprios atendimentos. |
| **Cliente** | Vê e gerencia apenas os próprios dados e agendamentos. |
| **Público** | Sem token. Apenas rotas de login, OTP, saúde e dados da página de agendamento. |

**Limites de requisição (rate limiting):** login, solicitação e verificação de OTP têm limite por IP e por telefone ou e-mail. Excesso retorna `429` com cabeçalho `Retry-After`.

## 3. Padrão de erros (RFC 7807, ProblemDetails)

Todos os erros usam o mesmo formato:

```json
{
  "type": "https://api.exemplo.com/erros/AGE_HORARIO_OCUPADO",
  "title": "Horário indisponível",
  "status": 409,
  "codigo": "AGE_HORARIO_OCUPADO",
  "detail": "O profissional já possui atendimento neste horário.",
  "instance": "/api/v1/agendamentos",
  "correlationId": "b1f0c2a4-...",
  "erros": { "inicio": ["Mensagem específica do campo"] }
}
```

O campo `erros` aparece apenas em falhas de validação (400). Stack trace nunca é exposto.

### Status HTTP usados

| Status | Significado | Quando |
| --- | --- | --- |
| 200 | OK | Consulta ou ação concluída com corpo de retorno |
| 201 | Criado | Recurso criado |
| 202 | Aceito | Processamento assíncrono (ex.: envio de OTP) |
| 204 | Sem conteúdo | Sucesso sem corpo (exclusão lógica, logout) |
| 400 | Requisição inválida | Formato ou validação de campos |
| 401 | Não autenticado | Token ausente, inválido ou expirado |
| 403 | Proibido | Perfil sem permissão ou recurso de outro dono |
| 404 | Não encontrado | Inclusive recursos de outro estabelecimento |
| 409 | Conflito | Choque de estado: horário ocupado, duplicidade, transição inválida |
| 422 | Regra de negócio violada | Dados válidos, mas a regra não permite |
| 429 | Excesso de requisições | Rate limiting |
| 500 | Erro interno | Falha inesperada (registrada com correlationId) |

### Códigos de erro de negócio

| Código | Status | Significado | Regra |
| --- | --- | --- | --- |
| VAL\_INVALIDO | 400 | Campos inválidos ou ausentes | — |
| AUTH\_CREDENCIAIS | 401 | E-mail ou senha incorretos | RF-SEG-01 |
| AUTH\_TOKEN\_EXPIRADO | 401 | Token expirado ou inválido | RF-SEG-01 |
| AUTH\_OTP\_INVALIDO | 401 | Código OTP incorreto ou expirado | RF-CLI-03 |
| AUTH\_PERMISSAO | 403 | Perfil sem acesso à rota ou ao dado | RF-SEG-02, 03 |
| RECURSO\_NAO\_ENCONTRADO | 404 | Id inexistente ou de outro tenant | RF-SAAS-01 |
| CAD\_TELEFONE\_DUPLICADO | 409 | Telefone já cadastrado no estabelecimento | RN-CAD-02 |
| CAD\_LGPD\_OBRIGATORIO | 422 | Consentimento LGPD não informado | RF-CAD-02 |
| CAD\_POSSUI\_AGENDAMENTOS\_FUTUROS | 409 | Não é possível inativar com agendamentos futuros | RF-CAD-03 |
| AGE\_HORARIO\_OCUPADO | 409 | Sobreposição com outro agendamento do profissional | RN-AGE-01 |
| AGE\_FORA\_DISPONIBILIDADE | 422 | Fora do expediente do profissional ou da unidade | RN-AGE-02 |
| AGE\_BLOQUEIO | 409 | Horário coincide com um bloqueio | RF-AGE-02 |
| AGE\_PROFISSIONAL\_NAO\_HABILITADO | 422 | Profissional não executa algum dos serviços | RN-AGE-04 |
| AGE\_ANTECEDENCIA\_INVALIDA | 422 | Fora da antecedência mínima ou máxima | RN-AGE-05 |
| AGE\_CLIENTE\_CONFLITO | 409 | Cliente já tem agendamento neste horário | RN-AGE-06 |
| AGE\_TRANSICAO\_INVALIDA | 409 | Mudança de status não permitida | RN-AGE-07 |
| FIN\_VALOR\_EXCEDE\_SALDO | 422 | Pagamento maior que o saldo da comanda | RF-FIN-02 |
| FIN\_DESCONTO\_SEM\_MOTIVO | 422 | Desconto ou estorno sem motivo | RF-FIN-03, RN-FIN-02 |
| FIN\_COMANDA\_FECHADA | 409 | Comanda já paga ou cancelada | RF-FIN-01 |
| FIN\_AGENDAMENTO\_NAO\_INICIADO | 422 | Comanda exige atendimento iniciado | RF-FIN-01 |
| IDEMPOTENCIA\_CONFLITO | 409 | Mesma chave com corpo diferente | RF-FIN-02 |
| RATE\_LIMIT | 429 | Excesso de tentativas | RNF-04 |

## 4. Rotas

**Legenda de perfis:** A = Admin, R = Recepcao, P = Profissional, C = Cliente, Pub = Público. "Próprio" significa que o perfil só acessa dados que lhe pertencem.

Todas as rotas abaixo ficam sob `/api/v1`. Erros 401 (rotas protegidas), 403 e 429 podem ocorrer em qualquer rota e só são listados quando têm particularidade.

### 4.1 Autenticação

| Método | Rota | Perfis | Entrada | Sucesso | Erros |
| --- | --- | --- | --- | --- | --- |
| POST | `/auth/login` | Pub | corpo: `email`, `senha` | 200: `accessToken`, `refreshToken`, `expiraEm`, `usuario` | 400, 401 AUTH\_CREDENCIAIS, 429 |
| POST | `/auth/refresh` | Pub | corpo: `refreshToken` | 200: novos tokens | 401 AUTH\_TOKEN\_EXPIRADO |
| POST | `/auth/logout` | A, R, P, C | corpo: `refreshToken` | 204 | 401 |
| POST | `/auth/cliente/solicitar-codigo` | Pub | cabeçalho `X-Estabelecimento`; corpo: `telefone` | 202 (código enviado) | 400, 429 |
| POST | `/auth/cliente/verificar-codigo` | Pub | cabeçalho `X-Estabelecimento`; corpo: `telefone`, `codigo` | 200: tokens com perfil Cliente; `clienteExistente` (bool) | 401 AUTH\_OTP\_INVALIDO, 429 |

Mensagens de erro de login são sempre genéricas, sem revelar se o e-mail existe.

### 4.2 Público e sistema

| Método | Rota | Perfis | Entrada | Sucesso | Erros |
| --- | --- | --- | --- | --- | --- |
| GET | `/publico/estabelecimento` | Pub | cabeçalho `X-Estabelecimento` | 200: nome, logo, unidades, serviços ativos | 404 |
| GET | `/publico/politica-privacidade` | Pub | cabeçalho `X-Estabelecimento` | 200: texto e versão do termo LGPD | 404 |
| GET | `/health` | Pub | — | 200: processo no ar | — |
| GET | `/health/ready` | Pub | — | 200: banco e dependências OK | 503 |
| POST | `/webhooks/mensageria` | Provedor | assinatura HMAC no cabeçalho; corpo do provedor (respostas Confirmar ou Cancelar) | 200 | 401 assinatura inválida |

### 4.3 Unidades

| Método | Rota | Perfis | Entrada | Sucesso | Erros |
| --- | --- | --- | --- | --- | --- |
| GET | `/unidades` | A, R, P, C | query: `ativo`, paginação | 200: lista paginada | 401 |
| GET | `/unidades/{id}` | A, R, P, C | — | 200: unidade | 404 |
| POST | `/unidades` | A | corpo: `nome`, `endereco`, `telefone`, `abreAs`, `fechaAs`, `fusoHorario` | 201 + Location | 400, 409 nome duplicado |
| PUT | `/unidades/{id}` | A | mesmo corpo do POST | 200: unidade atualizada | 400, 404 |
| DELETE | `/unidades/{id}` | A | — | 204 (inativa) | 404, 409 se houver agendamentos futuros |

### 4.4 Clientes

| Método | Rota | Perfis | Entrada | Sucesso | Erros |
| --- | --- | --- | --- | --- | --- |
| GET | `/clientes` | A, R | query: `busca` (nome ou telefone), `ativo`, paginação | 200: lista paginada | — |
| GET | `/clientes/{id}` | A, R, C próprio | — | 200: cliente | 403, 404 |
| POST | `/clientes` | A, R, C | corpo: `nome`, `telefone`, `email?`, `dataNascimento?`, `observacoes?`, `consentimentoLgpd` | 201 + Location | 400, 409 CAD\_TELEFONE\_DUPLICADO, 422 CAD\_LGPD\_OBRIGATORIO |
| PUT | `/clientes/{id}` | A, R, C próprio | mesmo corpo (sem alterar o consentimento já dado) | 200 | 400, 403, 404, 409 |
| DELETE | `/clientes/{id}` | A | — | 204 (inativa) | 404 |
| GET | `/clientes/{id}/agendamentos` | A, R, C próprio | query: `status`, `de`, `ate`, paginação | 200: histórico paginado | 403, 404 |

O campo `observacoes` (alergias, pele, unha) é dado sensível e só é devolvido para A e R.

### 4.5 Especialidades e serviços

| Método | Rota | Perfis | Entrada | Sucesso | Erros |
| --- | --- | --- | --- | --- | --- |
| GET | `/especialidades` | A, R, P, C | — | 200: lista | — |
| POST | `/especialidades` | A | corpo: `nome` | 201 | 400, 409 |
| GET | `/servicos` | A, R, P, C | query: `especialidadeId`, `ativo`, paginação | 200: lista paginada | — |
| GET | `/servicos/{id}` | A, R, P, C | — | 200: serviço | 404 |
| POST | `/servicos` | A | corpo: `nome`, `especialidadeId`, `duracaoMin` (maior que 0), `preco` (maior ou igual a 0) | 201 + Location | 400, 404 especialidade |
| PUT | `/servicos/{id}` | A | mesmo corpo do POST | 200 (não altera preços já congelados em agendamentos) | 400, 404 |
| DELETE | `/servicos/{id}` | A | — | 204 (inativa) | 404 |

### 4.6 Profissionais, disponibilidade e bloqueios

| Método | Rota | Perfis | Entrada | Sucesso | Erros |
| --- | --- | --- | --- | --- | --- |
| GET | `/profissionais` | A, R, P, C | query: `unidadeId`, `servicoId`, `ativo`, paginação | 200: lista paginada | — |
| GET | `/profissionais/{id}` | A, R, P, C | — | 200: profissional | 404 |
| POST | `/profissionais` | A | corpo: `unidadeId`, `nome`, `comissaoPadrao` (0 a 100), `especialidadeIds[]`, `usuarioId?` | 201 + Location | 400, 404 |
| PUT | `/profissionais/{id}` | A | mesmo corpo do POST | 200 | 400, 404 |
| DELETE | `/profissionais/{id}` | A | — | 204 (inativa) | 404, 409 CAD\_POSSUI\_AGENDAMENTOS\_FUTUROS |
| GET | `/profissionais/{id}/servicos` | A, R, P, C | — | 200: serviços habilitados com preço e duração efetivos | 404 |
| PUT | `/profissionais/{id}/servicos` | A | corpo: lista de `{ servicoId, precoPersonalizado?, duracaoPersonalizada?, comissao? }` | 200: lista atualizada | 400, 404 |
| GET | `/profissionais/{id}/disponibilidade` | A, R, P próprio | — | 200: faixas por dia da semana | 403, 404 |
| PUT | `/profissionais/{id}/disponibilidade` | A | corpo: lista de `{ diaSemana (0 a 6), inicio, fim }` | 200 | 400, 422 faixas sobrepostas ou fora do horário da unidade |
| GET | `/profissionais/{id}/bloqueios` | A, R, P próprio | query: `de`, `ate` | 200: lista | 403, 404 |
| POST | `/profissionais/{id}/bloqueios` | A, R | corpo: `inicio`, `fim`, `motivo` | 201 | 400, 409 se houver agendamentos no período (devolve a lista deles) |
| DELETE | `/profissionais/{id}/bloqueios/{bloqueioId}` | A, R | — | 204 | 404 |

### 4.7 Agenda

| Método | Rota | Perfis | Entrada | Sucesso | Erros |
| --- | --- | --- | --- | --- | --- |
| GET | `/agenda/horarios-livres` | A, R, P, C | query: `unidadeId`, `servicoIds` (ex.: `1,2`), `data`, `profissionalId?` | 200: duração total e lista de horários por profissional | 400, 404 |
| GET | `/agenda` | A, R, P próprio | query: `unidadeId`, `de`, `ate`, `profissionalId?` | 200: agendamentos e bloqueios do período (visão da recepção) | 400, 403 |

O período máximo de `/agenda` é de 31 dias por consulta.

### 4.8 Agendamentos

| Método | Rota | Perfis | Entrada | Sucesso | Erros |
| --- | --- | --- | --- | --- | --- |
| POST | `/agendamentos` | A, R, C | cabeçalho `Idempotency-Key`; corpo: `clienteId` (ignorado para C, usa o do token), `unidadeId`, `profissionalId`, `inicio`, `servicoIds[]`, `origem?` | 201 + Location: agendamento PENDENTE | 400, 404, 409 (AGE\_HORARIO\_OCUPADO, AGE\_BLOQUEIO, AGE\_CLIENTE\_CONFLITO, IDEMPOTENCIA\_CONFLITO), 422 (AGE\_FORA\_DISPONIBILIDADE, AGE\_PROFISSIONAL\_NAO\_HABILITADO, AGE\_ANTECEDENCIA\_INVALIDA) |
| GET | `/agendamentos` | A, R, P próprio, C próprio | query: `unidadeId`, `profissionalId`, `clienteId`, `status`, `de`, `ate`, paginação | 200: lista paginada | 400 |
| GET | `/agendamentos/{id}` | A, R, P próprio, C próprio | — | 200: detalhe com itens, preços congelados e histórico de status | 403, 404 |
| POST | `/agendamentos/{id}/confirmar` | A, R, C próprio | — | 200: status CONFIRMADO | 404, 409 AGE\_TRANSICAO\_INVALIDA |
| POST | `/agendamentos/{id}/check-in` | A, R | — | 200: registra presença | 404, 409 |
| POST | `/agendamentos/{id}/iniciar` | A, P próprio | — | 200: status EM\_ATENDIMENTO | 404, 409 |
| POST | `/agendamentos/{id}/concluir` | A, P próprio | — | 200: status CONCLUIDO | 404, 409 |
| POST | `/agendamentos/{id}/cancelar` | A, R, C próprio | corpo: `motivo?` | 200: `status`, `multaAplicada`, `valorMulta` | 404, 409 AGE\_TRANSICAO\_INVALIDA |
| POST | `/agendamentos/{id}/remarcar` | A, R, C próprio | corpo: `novoInicio`, `profissionalId?` | 200: novo agendamento; o antigo vira REMARCADO | 404, 409, 422 (mesmas regras da criação) |
| POST | `/agendamentos/{id}/no-show` | A, R | — | 200: status NO\_SHOW; contador do cliente incrementado | 404, 409 |

**Transições permitidas:**

| De | Para |
| --- | --- |
| PENDENTE | CONFIRMADO, CANCELADO, REMARCADO, NO\_SHOW |
| CONFIRMADO | EM\_ATENDIMENTO, CANCELADO, REMARCADO, NO\_SHOW |
| EM\_ATENDIMENTO | CONCLUIDO |
| CONCLUIDO, CANCELADO, NO\_SHOW, REMARCADO | (estados finais) |

### 4.9 Comandas e pagamentos

| Método | Rota | Perfis | Entrada | Sucesso | Erros |
| --- | --- | --- | --- | --- | --- |
| POST | `/comandas` | A, R | corpo: `agendamentoId` (comanda do agendamento) **ou** `clienteId`, `profissionalId`, `servicoIds[]` (avulsa, sem hora marcada) | 201 + Location: comanda ABERTA com itens e preços congelados | 400, 404, 409 FIN\_COMANDA\_FECHADA, 422 FIN\_AGENDAMENTO\_NAO\_INICIADO |
| GET | `/comandas` | A, R | query: `status`, `clienteId`, `de`, `ate`, paginação | 200: lista paginada | 400 |
| GET | `/comandas/{id}` | A, R | — | 200: itens, descontos, pagamentos, `total`, `saldo` | 404 |
| POST | `/comandas/{id}/descontos` | A (R apenas dentro do limite configurado) | corpo: `tipo` (VALOR ou PERCENTUAL), `valor`, `motivo` | 200: comanda recalculada | 403, 404, 409 FIN\_COMANDA\_FECHADA, 422 FIN\_DESCONTO\_SEM\_MOTIVO |
| POST | `/comandas/{id}/pagamentos` | A, R | cabeçalho `Idempotency-Key`; corpo: `forma` (PIX, CARTAO\_CREDITO, CARTAO\_DEBITO, DINHEIRO), `valor` | 201: `pagamentoId`, `saldoRestante`, `statusComanda` | 400, 404, 409 (FIN\_COMANDA\_FECHADA, IDEMPOTENCIA\_CONFLITO), 422 FIN\_VALOR\_EXCEDE\_SALDO |
| POST | `/pagamentos/{id}/estorno` | A | corpo: `motivo` | 200: pagamento ESTORNADO; comissão revertida | 404, 409, 422 FIN\_DESCONTO\_SEM\_MOTIVO |

Quando o saldo chega a zero, a comanda vai para PAGA e o sistema gera a comissão do profissional (evento `PagamentoConfirmado`).

### 4.10 Comissões

| Método | Rota | Perfis | Entrada | Sucesso | Erros |
| --- | --- | --- | --- | --- | --- |
| GET | `/comissoes` | A, R, P próprio | query: `profissionalId`, `de`, `ate`, `status`, paginação (P só enxerga as próprias) | 200: lista paginada | 400, 403 |
| GET | `/comissoes/resumo` | A, P próprio | query: `de`, `ate`, `profissionalId?` | 200: total por profissional e por serviço | 400, 403 |

### 4.11 Relatórios

| Método | Rota | Perfis | Entrada | Sucesso | Erros |
| --- | --- | --- | --- | --- | --- |
| GET | `/relatorios/faturamento` | A | query: `de`, `ate`, `agrupar` (dia, semana, mes), `unidadeId?` | 200: série com `periodo`, `valor`, `quantidadeAtendimentos` | 400 (fim antes do início) |
| GET | `/relatorios/atendimentos` | A | query: `de`, `ate`, `agruparPor` (profissional ou servico) | 200: contagem de atendimentos CONCLUIDOS e valor | 400 |

Períodos sem dados retornam 200 com lista vazia.

### 4.12 Usuários

| Método | Rota | Perfis | Entrada | Sucesso | Erros |
| --- | --- | --- | --- | --- | --- |
| GET | `/usuarios` | A | query: `perfil`, `ativo`, paginação | 200: lista paginada | — |
| POST | `/usuarios` | A | corpo: `email`, `senhaInicial`, `perfil` (Admin, Recepcao, Profissional) | 201 + Location | 400 (senha fraca), 409 e-mail duplicado |
| PUT | `/usuarios/{id}` | A | corpo: `perfil` | 200 | 400, 404 |
| PATCH | `/usuarios/{id}/senha` | A, usuário próprio | corpo: `senhaAtual` (obrigatória se for o próprio), `novaSenha` | 204 | 400, 401 AUTH\_CREDENCIAIS, 403 |
| PATCH | `/usuarios/{id}/status` | A | corpo: `ativo` | 204 | 404, 409 (não permite inativar o último Admin) |

### 4.13 Configurações do estabelecimento

| Método | Rota | Perfis | Entrada | Sucesso | Erros |
| --- | --- | --- | --- | --- | --- |
| GET | `/configuracoes` | A, R | — | 200: regras vigentes | — |
| PUT | `/configuracoes` | A | corpo: `antecedenciaMinimaMinutos`, `antecedenciaMaximaDias`, `prazoCancelamentoHoras`, `intervaloHigienizacaoMinutos`, `multaCancelamentoTardioPercentual`, `limiteDescontoRecepcaoPercentual`, `canaisNotificacao[]`, `lembretesHorasAntes[]` | 200 | 400 |

As regras de agendamento, cancelamento e desconto leem estes valores, sem números fixos no código.

## 5. Exemplos de requisição e resposta

### 5.1 Login

```http
POST /api/v1/auth/login
Content-Type: application/json

{ "email": "recepcao@barbearia.com", "senha": "********" }
```

```json
// 200 OK
{
  "accessToken": "eyJhbGciOi...",
  "refreshToken": "d3f1...",
  "expiraEm": "2026-10-15T13:45:00Z",
  "usuario": { "id": 7, "email": "recepcao@barbearia.com", "perfil": "Recepcao" }
}
```

### 5.2 Horários livres

```http
GET /api/v1/agenda/horarios-livres?unidadeId=1&servicoIds=1,2&data=2026-10-20&profissionalId=3
Authorization: Bearer eyJhbGciOi...
```

```json
// 200 OK
{
  "data": "2026-10-20",
  "duracaoTotalMin": 50,
  "horarios": [
    { "profissionalId": 3, "inicio": "2026-10-20T12:00:00Z", "fim": "2026-10-20T12:50:00Z" },
    { "profissionalId": 3, "inicio": "2026-10-20T12:30:00Z", "fim": "2026-10-20T13:20:00Z" }
  ]
}
```

### 5.3 Criar agendamento

```http
POST /api/v1/agendamentos
Authorization: Bearer eyJhbGciOi...
Idempotency-Key: 6f1c1f0e-3d51-4e0f-9a55-0c1b2f3a4d5e
Content-Type: application/json

{
  "clienteId": 15,
  "unidadeId": 1,
  "profissionalId": 3,
  "inicio": "2026-10-20T12:00:00Z",
  "servicoIds": [1, 2],
  "origem": "BALCAO"
}
```

```json
// 201 Created   (Location: /api/v1/agendamentos/982)
{
  "id": 982,
  "status": "PENDENTE",
  "inicio": "2026-10-20T12:00:00Z",
  "fim": "2026-10-20T12:50:00Z",
  "cliente": { "id": 15, "nome": "Carlos Souza" },
  "profissional": { "id": 3, "nome": "João" },
  "itens": [
    { "servicoId": 1, "nome": "Corte", "duracaoMin": 30, "precoCongelado": 40.00 },
    { "servicoId": 2, "nome": "Barba", "duracaoMin": 20, "precoCongelado": 30.00 }
  ],
  "valorTotal": 70.00
}
```

```json
// 409 Conflict
{
  "type": "https://api.exemplo.com/erros/AGE_HORARIO_OCUPADO",
  "title": "Horário indisponível",
  "status": 409,
  "codigo": "AGE_HORARIO_OCUPADO",
  "detail": "O profissional já possui atendimento neste horário.",
  "correlationId": "b1f0c2a4-7d2e-4a51-9c11-3e9c0a1f55aa"
}
```

### 5.4 Cancelar agendamento

```http
POST /api/v1/agendamentos/982/cancelar
{ "motivo": "Imprevisto no trabalho" }
```

```json
// 200 OK
{ "id": 982, "status": "CANCELADO", "multaAplicada": false, "valorMulta": 0.00 }
```

### 5.5 Registrar pagamento

```http
POST /api/v1/comandas/410/pagamentos
Idempotency-Key: 0a7d6c1e-91b4-4c63-8f0e-5b2d7c9e1a33
{ "forma": "PIX", "valor": 70.00 }
```

```json
// 201 Created
{ "pagamentoId": 880, "saldoRestante": 0.00, "statusComanda": "PAGA" }
```

## 6. Enumerações

| Enum | Valores |
| --- | --- |
| StatusAgendamento | PENDENTE, CONFIRMADO, EM\_ATENDIMENTO, CONCLUIDO, CANCELADO, NO\_SHOW, REMARCADO |
| StatusComanda | ABERTA, PAGA, CANCELADA |
| StatusPagamento | CONFIRMADO, ESTORNADO |
| FormaPagamento | PIX, CARTAO\_CREDITO, CARTAO\_DEBITO, DINHEIRO |
| StatusComissao | PENDENTE, PAGA, REVERTIDA |
| OrigemAgendamento | APP, SITE, BALCAO, TELEFONE, WHATSAPP |
| Perfil | Admin, Recepcao, Profissional, Cliente |
| TipoDesconto | VALOR, PERCENTUAL |
| DiaSemana | 0 (domingo) a 6 (sábado) |

## 7. Eventos de domínio (internos)

Não são rotas, mas disparam efeitos automáticos e devem ser testados.

| Evento | Disparado por | Efeito |
| --- | --- | --- |
| AgendamentoCriado | POST `/agendamentos` | Enfileira confirmação ao cliente |
| AgendamentoCancelado | POST `/agendamentos/{id}/cancelar` | Libera horário; aplica multa se fora do prazo |
| AgendamentoRemarcado | POST `/agendamentos/{id}/remarcar` | Nova confirmação ao cliente |
| AtendimentoConcluido | POST `/agendamentos/{id}/concluir` | Habilita pagamento e pedido de avaliação (F2) |
| PagamentoConfirmado | Pagamento que zera o saldo | Gera comissão do profissional |
| PagamentoEstornado | POST `/pagamentos/{id}/estorno` | Reverte a comissão |
| NoShowRegistrado | POST `/agendamentos/{id}/no-show` | Incrementa o contador do cliente |

Tarefas agendadas (worker): lembretes 24h e 2h antes, marcação automática de no-show para atendimentos vencidos sem check-in.

## 8. Notas de implementação em C#

- **Framework:** ASP.NET Core (.NET 8 ou superior), Controllers ou Minimal APIs com grupos `/api/v1`.
- **Documentação viva:** Swashbuckle ou NSwag gerando OpenAPI, com exemplos e códigos de erro nas anotações (`ProducesResponseType`).
- **Validação:** FluentValidation para 400; exceções de domínio traduzidas para 409 ou 422 por um middleware global que produz `ProblemDetails`.
- **Autenticação:** `Microsoft.AspNetCore.Authentication.JwtBearer`, políticas por perfil e verificação de propriedade ("próprio") em handlers de autorização.
- **Multi-tenant:** `ITenantProvider` lendo a claim `estabelecimento_id`, com **query filter global** no EF Core para todas as entidades com tenant.
- **Concorrência:** transação com nível de isolamento adequado e constraint no banco impedindo intervalos sobrepostos por profissional.
- **Idempotência:** tabela de chaves com hash do corpo, guardando a resposta por 24 horas.
- **Rate limiting:** `Microsoft.AspNetCore.RateLimiting` nas rotas de autenticação.
- **Versionamento:** `Asp.Versioning`, com `/api/v1` no caminho.
- **Relógio:** `TimeProvider` injetado, para testar prazos e lembretes.
- **Observabilidade:** logs estruturados (Serilog) com `correlationId`, health checks e métricas.

## 9. Resumo de rotas do MVP

| Módulo | Quantidade de rotas |
| --- | --- |
| Autenticação | 5 |
| Público e sistema | 5 |
| Unidades | 5 |
| Clientes | 6 |
| Especialidades e serviços | 7 |
| Profissionais, disponibilidade e bloqueios | 12 |
| Agenda | 2 |
| Agendamentos | 9 |
| Comandas e pagamentos | 6 |
| Comissões | 2 |
| Relatórios | 2 |
| Usuários | 5 |
| Configurações | 2 |
| **Total** | **68** |
