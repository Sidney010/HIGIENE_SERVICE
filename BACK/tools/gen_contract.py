#!/usr/bin/env python3
"""
Fonte única das rotas do MVP.
Gera:  docs/openapi.yaml
       src/Agenda.Api/Controllers/Generated/*.Stubs.cs  (controllers parciais, 501 até implementar)

Uso:   python3 tools/gen_contract.py
Aviso: os stubs são regenerados. Ao implementar uma rota, mova-a para a parte manual
       do controller (ex.: AgendamentosController.cs) e adicione (METODO, caminho) em REAIS.
"""
import re, sys, pathlib, yaml

ROOT = pathlib.Path(__file__).resolve().parent.parent

TAGS = {  # chave -> (nome no OpenAPI, controller)
    "Autenticacao": "Autenticação", "Publico": "Público", "Sistema": "Sistema",
    "Unidades": "Unidades", "Clientes": "Clientes", "Especialidades": "Especialidades",
    "Servicos": "Serviços", "Profissionais": "Profissionais", "Agenda": "Agenda",
    "Agendamentos": "Agendamentos", "Comandas": "Comandas e pagamentos",
    "Comissoes": "Comissões", "Relatorios": "Relatórios", "Usuarios": "Usuários",
    "Configuracoes": "Configurações",
}
ROLE = {"A": "Admin", "R": "Recepcao", "P": "Profissional", "C": "Cliente"}

# rotas já implementadas à mão (não geram stub)
REAIS = {("POST", "/agendamentos"), ("POST", "/agendamentos/{id}/cancelar"),
         ("GET", "/agenda/horarios-livres")}
# rotas atendidas pelo framework (health checks)
BUILTIN = {("GET", "/health"), ("GET", "/health/ready")}

ROUTES = []
def r(method, path, tag, op, summary, roles, q="", body=None, ok=(200, None),
      err=None, idem=False, tenant=False):
    ROUTES.append(dict(method=method, path=path, tag=tag, op=op, summary=summary,
                       roles=roles, q=q, body=body, ok=ok, err=err or {}, idem=idem,
                       tenant=tenant))

ALL = "A,R,P,C"
# ---------------------------------------------------------------- Autenticação
r("POST", "/auth/login", "Autenticacao", "Login", "Autentica usuário da equipe", "Pub",
  body="LoginRequest", ok=(200, "TokenResponse"),
  err={400: "VAL_INVALIDO", 401: "AUTH_CREDENCIAIS", 429: "RATE_LIMIT"})
r("POST", "/auth/refresh", "Autenticacao", "Refresh", "Renova o token de acesso", "Pub",
  body="RefreshRequest", ok=(200, "TokenResponse"), err={401: "AUTH_TOKEN_EXPIRADO"})
r("POST", "/auth/logout", "Autenticacao", "Logout", "Encerra a sessão (revoga o refresh token)",
  ALL, body="RefreshRequest", ok=(204, None))
r("POST", "/auth/cliente/solicitar-codigo", "Autenticacao", "SolicitarCodigo",
  "Envia código OTP ao telefone do cliente", "Pub", tenant=True,
  body="SolicitarCodigoRequest", ok=(202, None), err={400: "VAL_INVALIDO", 429: "RATE_LIMIT"})
r("POST", "/auth/cliente/verificar-codigo", "Autenticacao", "VerificarCodigo",
  "Valida o OTP e devolve tokens com perfil Cliente", "Pub", tenant=True,
  body="VerificarCodigoRequest", ok=(200, "TokenClienteResponse"),
  err={401: "AUTH_OTP_INVALIDO", 429: "RATE_LIMIT"})
# ---------------------------------------------------------------- Público / sistema
r("GET", "/publico/estabelecimento", "Publico", "ObterEstabelecimento",
  "Dados da página pública de agendamento", "Pub", tenant=True,
  ok=(200, "EstabelecimentoPublico"), err={404: "RECURSO_NAO_ENCONTRADO"})
r("GET", "/publico/politica-privacidade", "Publico", "ObterPoliticaPrivacidade",
  "Termo de consentimento LGPD vigente", "Pub", tenant=True,
  ok=(200, "PoliticaPrivacidade"), err={404: "RECURSO_NAO_ENCONTRADO"})
r("GET", "/health", "Sistema", "Health", "Processo no ar (liveness)", "Pub", ok=(200, "HealthResponse"))
r("GET", "/health/ready", "Sistema", "HealthReady", "Banco e dependências OK (readiness)", "Pub",
  ok=(200, "HealthResponse"), err={503: "Dependência indisponível"})
r("POST", "/webhooks/mensageria", "Sistema", "WebhookMensageria",
  "Recebe respostas do provedor de mensagens (assinatura HMAC no cabeçalho)", "Pub",
  body="MensageriaWebhook", ok=(200, None), err={401: "Assinatura inválida"})
# ---------------------------------------------------------------- Unidades
r("GET", "/unidades", "Unidades", "Listar", "Lista unidades", ALL, q="ativo:bool?,pg", ok=(200, "Page<Unidade>"))
r("GET", "/unidades/{id}", "Unidades", "ObterPorId", "Detalha uma unidade", ALL, ok=(200, "Unidade"), err={404: "RECURSO_NAO_ENCONTRADO"})
r("POST", "/unidades", "Unidades", "Criar", "Cria unidade", "A", body="UnidadeRequest", ok=(201, "Unidade"), err={400: "VAL_INVALIDO", 409: "Nome duplicado"})
r("PUT", "/unidades/{id}", "Unidades", "Atualizar", "Atualiza unidade", "A", body="UnidadeRequest", ok=(200, "Unidade"), err={400: "VAL_INVALIDO", 404: "RECURSO_NAO_ENCONTRADO"})
r("DELETE", "/unidades/{id}", "Unidades", "Inativar", "Inativa unidade (soft delete)", "A", ok=(204, None), err={404: "RECURSO_NAO_ENCONTRADO", 409: "CAD_POSSUI_AGENDAMENTOS_FUTUROS"})
# ---------------------------------------------------------------- Clientes
r("GET", "/clientes", "Clientes", "Listar", "Lista clientes", "A,R", q="busca:str?,ativo:bool?,pg", ok=(200, "Page<Cliente>"))
r("GET", "/clientes/{id}", "Clientes", "ObterPorId", "Detalha cliente", "A,R,C*", ok=(200, "Cliente"), err={403: "AUTH_PERMISSAO", 404: "RECURSO_NAO_ENCONTRADO"})
r("POST", "/clientes", "Clientes", "Criar", "Cadastra cliente (inclusive autocadastro)", "A,R,C", body="ClienteRequest", ok=(201, "Cliente"),
  err={400: "VAL_INVALIDO", 409: "CAD_TELEFONE_DUPLICADO", 422: "CAD_LGPD_OBRIGATORIO"})
r("PUT", "/clientes/{id}", "Clientes", "Atualizar", "Atualiza cliente", "A,R,C*", body="ClienteRequest", ok=(200, "Cliente"),
  err={400: "VAL_INVALIDO", 403: "AUTH_PERMISSAO", 404: "RECURSO_NAO_ENCONTRADO", 409: "CAD_TELEFONE_DUPLICADO"})
r("DELETE", "/clientes/{id}", "Clientes", "Inativar", "Inativa cliente (soft delete)", "A", ok=(204, None), err={404: "RECURSO_NAO_ENCONTRADO"})
r("GET", "/clientes/{id}/agendamentos", "Clientes", "ListarAgendamentos", "Histórico de agendamentos do cliente", "A,R,C*",
  q="status:StatusAgendamento?,de:dt?,ate:dt?,pg", ok=(200, "Page<AgendamentoResumo>"), err={403: "AUTH_PERMISSAO", 404: "RECURSO_NAO_ENCONTRADO"})
# ---------------------------------------------------------------- Especialidades / Serviços
r("GET", "/especialidades", "Especialidades", "Listar", "Lista especialidades", ALL, ok=(200, "List<Especialidade>"))
r("POST", "/especialidades", "Especialidades", "Criar", "Cria especialidade", "A", body="EspecialidadeRequest", ok=(201, "Especialidade"), err={400: "VAL_INVALIDO", 409: "Nome duplicado"})
r("GET", "/servicos", "Servicos", "Listar", "Lista serviços", ALL, q="especialidadeId:int?,ativo:bool?,pg", ok=(200, "Page<Servico>"))
r("GET", "/servicos/{id}", "Servicos", "ObterPorId", "Detalha serviço", ALL, ok=(200, "Servico"), err={404: "RECURSO_NAO_ENCONTRADO"})
r("POST", "/servicos", "Servicos", "Criar", "Cria serviço (duração > 0, preço >= 0)", "A", body="ServicoRequest", ok=(201, "Servico"), err={400: "VAL_INVALIDO", 404: "RECURSO_NAO_ENCONTRADO"})
r("PUT", "/servicos/{id}", "Servicos", "Atualizar", "Atualiza serviço (não altera preços já congelados)", "A", body="ServicoRequest", ok=(200, "Servico"), err={400: "VAL_INVALIDO", 404: "RECURSO_NAO_ENCONTRADO"})
r("DELETE", "/servicos/{id}", "Servicos", "Inativar", "Inativa serviço (soft delete)", "A", ok=(204, None), err={404: "RECURSO_NAO_ENCONTRADO"})
# ---------------------------------------------------------------- Profissionais
r("GET", "/profissionais", "Profissionais", "Listar", "Lista profissionais", ALL, q="unidadeId:int?,servicoId:int?,ativo:bool?,pg", ok=(200, "Page<Profissional>"))
r("GET", "/profissionais/{id}", "Profissionais", "ObterPorId", "Detalha profissional", ALL, ok=(200, "Profissional"), err={404: "RECURSO_NAO_ENCONTRADO"})
r("POST", "/profissionais", "Profissionais", "Criar", "Cadastra profissional", "A", body="ProfissionalRequest", ok=(201, "Profissional"), err={400: "VAL_INVALIDO", 404: "RECURSO_NAO_ENCONTRADO"})
r("PUT", "/profissionais/{id}", "Profissionais", "Atualizar", "Atualiza profissional", "A", body="ProfissionalRequest", ok=(200, "Profissional"), err={400: "VAL_INVALIDO", 404: "RECURSO_NAO_ENCONTRADO"})
r("DELETE", "/profissionais/{id}", "Profissionais", "Inativar", "Inativa profissional (soft delete)", "A", ok=(204, None), err={404: "RECURSO_NAO_ENCONTRADO", 409: "CAD_POSSUI_AGENDAMENTOS_FUTUROS"})
r("GET", "/profissionais/{id}/servicos", "Profissionais", "ListarServicos", "Serviços habilitados com preço e duração efetivos", ALL, ok=(200, "List<ProfissionalServico>"), err={404: "RECURSO_NAO_ENCONTRADO"})
r("PUT", "/profissionais/{id}/servicos", "Profissionais", "DefinirServicos", "Define os serviços que o profissional executa", "A", body="List<ProfissionalServicoRequest>", ok=(200, "List<ProfissionalServico>"), err={400: "VAL_INVALIDO", 404: "RECURSO_NAO_ENCONTRADO"})
r("GET", "/profissionais/{id}/disponibilidade", "Profissionais", "ObterDisponibilidade", "Faixas de atendimento por dia da semana", "A,R,P*", ok=(200, "List<FaixaDisponibilidade>"), err={403: "AUTH_PERMISSAO", 404: "RECURSO_NAO_ENCONTRADO"})
r("PUT", "/profissionais/{id}/disponibilidade", "Profissionais", "DefinirDisponibilidade", "Substitui a disponibilidade semanal", "A", body="List<FaixaDisponibilidade>", ok=(200, "List<FaixaDisponibilidade>"), err={400: "VAL_INVALIDO", 422: "AGE_FORA_DISPONIBILIDADE (faixas sobrepostas ou fora da unidade)"})
r("GET", "/profissionais/{id}/bloqueios", "Profissionais", "ListarBloqueios", "Bloqueios (folga, férias, almoço)", "A,R,P*", q="de:dt?,ate:dt?", ok=(200, "List<Bloqueio>"), err={403: "AUTH_PERMISSAO", 404: "RECURSO_NAO_ENCONTRADO"})
r("POST", "/profissionais/{id}/bloqueios", "Profissionais", "CriarBloqueio", "Cria bloqueio de agenda", "A,R", body="BloqueioRequest", ok=(201, "Bloqueio"), err={400: "VAL_INVALIDO", 409: "AGE_BLOQUEIO (há agendamentos no período; devolve a lista)"})
r("DELETE", "/profissionais/{id}/bloqueios/{bloqueioId}", "Profissionais", "RemoverBloqueio", "Remove bloqueio", "A,R", ok=(204, None), err={404: "RECURSO_NAO_ENCONTRADO"})
# ---------------------------------------------------------------- Agenda
r("GET", "/agenda/horarios-livres", "Agenda", "HorariosLivres", "Horários livres para os serviços escolhidos", ALL,
  q="unidadeId:int,servicoIds:ints,data:date,profissionalId:int?", ok=(200, "HorariosLivresResponse"), err={400: "VAL_INVALIDO", 404: "RECURSO_NAO_ENCONTRADO"})
r("GET", "/agenda", "Agenda", "Consultar", "Visão da recepção: agendamentos e bloqueios (máx. 31 dias)", "A,R,P*",
  q="unidadeId:int,de:dt,ate:dt,profissionalId:int?", ok=(200, "AgendaResponse"), err={400: "VAL_INVALIDO", 403: "AUTH_PERMISSAO"})
# ---------------------------------------------------------------- Agendamentos
STATE_ERR = {404: "RECURSO_NAO_ENCONTRADO", 409: "AGE_TRANSICAO_INVALIDA"}
r("POST", "/agendamentos", "Agendamentos", "Criar", "Cria agendamento (status PENDENTE)", "A,R,C", idem=True,
  body="CriarAgendamentoRequest", ok=(201, "Agendamento"),
  err={400: "VAL_INVALIDO", 404: "RECURSO_NAO_ENCONTRADO",
       409: "AGE_HORARIO_OCUPADO | AGE_BLOQUEIO | AGE_CLIENTE_CONFLITO | IDEMPOTENCIA_CONFLITO",
       422: "AGE_FORA_DISPONIBILIDADE | AGE_PROFISSIONAL_NAO_HABILITADO | AGE_ANTECEDENCIA_INVALIDA"})
r("GET", "/agendamentos", "Agendamentos", "Listar", "Lista agendamentos", "A,R,P*,C*",
  q="unidadeId:int?,profissionalId:int?,clienteId:int?,status:StatusAgendamento?,de:dt?,ate:dt?,pg",
  ok=(200, "Page<AgendamentoResumo>"), err={400: "VAL_INVALIDO"})
r("GET", "/agendamentos/{id}", "Agendamentos", "ObterPorId", "Detalha agendamento (itens, preços congelados, histórico)", "A,R,P*,C*",
  ok=(200, "Agendamento"), err={403: "AUTH_PERMISSAO", 404: "RECURSO_NAO_ENCONTRADO"})
r("POST", "/agendamentos/{id}/confirmar", "Agendamentos", "Confirmar", "PENDENTE -> CONFIRMADO", "A,R,C*", ok=(200, "Agendamento"), err=STATE_ERR)
r("POST", "/agendamentos/{id}/check-in", "Agendamentos", "CheckIn", "Registra presença do cliente", "A,R", ok=(200, "Agendamento"), err=STATE_ERR)
r("POST", "/agendamentos/{id}/iniciar", "Agendamentos", "Iniciar", "CONFIRMADO -> EM_ATENDIMENTO", "A,P*", ok=(200, "Agendamento"), err=STATE_ERR)
r("POST", "/agendamentos/{id}/concluir", "Agendamentos", "Concluir", "EM_ATENDIMENTO -> CONCLUIDO", "A,P*", ok=(200, "Agendamento"), err=STATE_ERR)
r("POST", "/agendamentos/{id}/cancelar", "Agendamentos", "Cancelar", "Cancela; aplica multa se fora do prazo", "A,R,C*", body="CancelarRequest?", ok=(200, "CancelarResponse"), err=STATE_ERR)
r("POST", "/agendamentos/{id}/remarcar", "Agendamentos", "Remarcar", "Cria novo agendamento e marca o antigo como REMARCADO", "A,R,C*",
  body="RemarcarRequest", ok=(200, "Agendamento"),
  err={404: "RECURSO_NAO_ENCONTRADO", 409: "AGE_HORARIO_OCUPADO | AGE_TRANSICAO_INVALIDA", 422: "mesmas regras da criação"})
r("POST", "/agendamentos/{id}/no-show", "Agendamentos", "MarcarNoShow", "Registra falta do cliente", "A,R", ok=(200, "Agendamento"), err=STATE_ERR)
# ---------------------------------------------------------------- Comandas e pagamentos
r("POST", "/comandas", "Comandas", "Criar", "Abre comanda (de um agendamento ou avulsa)", "A,R", body="CriarComandaRequest", ok=(201, "Comanda"),
  err={400: "VAL_INVALIDO", 404: "RECURSO_NAO_ENCONTRADO", 409: "FIN_COMANDA_FECHADA", 422: "FIN_AGENDAMENTO_NAO_INICIADO"})
r("GET", "/comandas", "Comandas", "Listar", "Lista comandas", "A,R", q="status:StatusComanda?,clienteId:int?,de:dt?,ate:dt?,pg", ok=(200, "Page<Comanda>"), err={400: "VAL_INVALIDO"})
r("GET", "/comandas/{id}", "Comandas", "ObterPorId", "Detalha comanda (itens, descontos, pagamentos, saldo)", "A,R", ok=(200, "Comanda"), err={404: "RECURSO_NAO_ENCONTRADO"})
r("POST", "/comandas/{id}/descontos", "Comandas", "AplicarDesconto", "Aplica desconto (motivo obrigatório; Recepção só até o limite configurado)", "A,R*", body="DescontoRequest", ok=(200, "Comanda"),
  err={403: "AUTH_PERMISSAO", 404: "RECURSO_NAO_ENCONTRADO", 409: "FIN_COMANDA_FECHADA", 422: "FIN_DESCONTO_SEM_MOTIVO"})
r("POST", "/comandas/{id}/pagamentos", "Comandas", "RegistrarPagamento", "Registra pagamento (parcial ou integral)", "A,R", idem=True, body="PagamentoRequest", ok=(201, "PagamentoResponse"),
  err={400: "VAL_INVALIDO", 404: "RECURSO_NAO_ENCONTRADO", 409: "FIN_COMANDA_FECHADA | IDEMPOTENCIA_CONFLITO", 422: "FIN_VALOR_EXCEDE_SALDO"})
r("POST", "/pagamentos/{id}/estorno", "Comandas", "EstornarPagamento", "Estorna pagamento e reverte a comissão", "A", body="EstornoRequest", ok=(200, "Pagamento"),
  err={404: "RECURSO_NAO_ENCONTRADO", 409: "Pagamento já estornado", 422: "FIN_DESCONTO_SEM_MOTIVO"})
# ---------------------------------------------------------------- Comissões / Relatórios
r("GET", "/comissoes", "Comissoes", "Listar", "Lista comissões", "A,R,P*", q="profissionalId:int?,de:dt?,ate:dt?,status:StatusComissao?,pg", ok=(200, "Page<Comissao>"), err={400: "VAL_INVALIDO", 403: "AUTH_PERMISSAO"})
r("GET", "/comissoes/resumo", "Comissoes", "Resumo", "Total por profissional e por serviço", "A,P*", q="de:dt,ate:dt,profissionalId:int?", ok=(200, "List<ComissaoResumoItem>"), err={400: "VAL_INVALIDO", 403: "AUTH_PERMISSAO"})
r("GET", "/relatorios/faturamento", "Relatorios", "Faturamento", "Faturamento por período", "A", q="de:dt,ate:dt,agrupar:enum(dia|semana|mes),unidadeId:int?", ok=(200, "List<FaturamentoItem>"), err={400: "VAL_INVALIDO"})
r("GET", "/relatorios/atendimentos", "Relatorios", "Atendimentos", "Atendimentos concluídos por profissional ou serviço", "A", q="de:dt,ate:dt,agruparPor:enum(profissional|servico)", ok=(200, "List<AtendimentoRelItem>"), err={400: "VAL_INVALIDO"})
# ---------------------------------------------------------------- Usuários / Configurações
r("GET", "/usuarios", "Usuarios", "Listar", "Lista usuários da equipe", "A", q="perfil:Perfil?,ativo:bool?,pg", ok=(200, "Page<Usuario>"))
r("POST", "/usuarios", "Usuarios", "Criar", "Cria usuário", "A", body="UsuarioCriarRequest", ok=(201, "Usuario"), err={400: "VAL_INVALIDO (senha fraca)", 409: "E-mail duplicado"})
r("PUT", "/usuarios/{id}", "Usuarios", "Atualizar", "Altera o perfil do usuário", "A", body="UsuarioAtualizarRequest", ok=(200, "Usuario"), err={400: "VAL_INVALIDO", 404: "RECURSO_NAO_ENCONTRADO"})
r("PATCH", "/usuarios/{id}/senha", "Usuarios", "AlterarSenha", "Altera senha (própria exige senha atual)", "A,R*,P*", body="SenhaRequest", ok=(204, None), err={400: "VAL_INVALIDO", 401: "AUTH_CREDENCIAIS", 403: "AUTH_PERMISSAO"})
r("PATCH", "/usuarios/{id}/status", "Usuarios", "AlterarStatus", "Ativa/inativa (não permite inativar o último Admin)", "A", body="StatusRequest", ok=(204, None), err={404: "RECURSO_NAO_ENCONTRADO", 409: "Último administrador"})
r("GET", "/configuracoes", "Configuracoes", "Obter", "Regras vigentes do estabelecimento", "A,R", ok=(200, "Configuracoes"))
r("PUT", "/configuracoes", "Configuracoes", "Atualizar", "Atualiza regras (antecedência, multas, intervalos...)", "A", body="Configuracoes", ok=(200, "Configuracoes"), err={400: "VAL_INVALIDO"})

# ====================================================================== SCHEMAS
SCHEMAS_YAML = r"""
Perfil: {type: string, enum: [Admin, Recepcao, Profissional, Cliente]}
StatusAgendamento: {type: string, enum: [PENDENTE, CONFIRMADO, EM_ATENDIMENTO, CONCLUIDO, CANCELADO, NO_SHOW, REMARCADO]}
StatusComanda: {type: string, enum: [ABERTA, PAGA, CANCELADA]}
StatusPagamento: {type: string, enum: [CONFIRMADO, ESTORNADO]}
StatusComissao: {type: string, enum: [PENDENTE, PAGA, REVERTIDA]}
FormaPagamento: {type: string, enum: [PIX, CARTAO_CREDITO, CARTAO_DEBITO, DINHEIRO]}
OrigemAgendamento: {type: string, enum: [APP, SITE, BALCAO, TELEFONE, WHATSAPP]}
TipoDesconto: {type: string, enum: [VALOR, PERCENTUAL]}
ProblemDetails:
  type: object
  properties:
    type: STR
    title: STR
    status: INT
    codigo: {type: string, description: Código de erro de negócio (ex. AGE_HORARIO_OCUPADO)}
    detail: STR
    instance: STR
    correlationId: STR
    erros: {type: object, additionalProperties: {type: array, items: STR}, description: Erros por campo (somente 400)}
HealthResponse: {type: object, properties: {status: STR}}
LoginRequest: {type: object, required: [email, senha], properties: {email: {type: string, format: email}, senha: {type: string, format: password}}}
RefreshRequest: {type: object, required: [refreshToken], properties: {refreshToken: STR}}
UsuarioResumo: {type: object, properties: {id: ID, email: STR, perfil: @Perfil}}
TokenResponse: {type: object, properties: {accessToken: STR, refreshToken: STR, expiraEm: DT, usuario: @UsuarioResumo}}
SolicitarCodigoRequest: {type: object, required: [telefone], properties: {telefone: STR}}
VerificarCodigoRequest: {type: object, required: [telefone, codigo], properties: {telefone: STR, codigo: STR}}
TokenClienteResponse: {type: object, properties: {accessToken: STR, refreshToken: STR, expiraEm: DT, clienteExistente: BOOL}}
MensageriaWebhook: {type: object, additionalProperties: true}
EstabelecimentoPublico: {type: object, properties: {nome: STR, logoUrl: STR, unidades: {type: array, items: @Unidade}, servicos: {type: array, items: @Servico}}}
PoliticaPrivacidade: {type: object, properties: {versao: STR, texto: STR}}
Unidade: {type: object, properties: {id: ID, nome: STR, endereco: STR, telefone: STR, abreAs: TIME, fechaAs: TIME, fusoHorario: STR, ativo: BOOL}}
UnidadeRequest: {type: object, required: [nome, endereco, abreAs, fechaAs, fusoHorario], properties: {nome: STR, endereco: STR, telefone: STR, abreAs: TIME, fechaAs: TIME, fusoHorario: {type: string, example: America/Sao_Paulo}}}
Cliente: {type: object, properties: {id: ID, nome: STR, telefone: STR, email: STR, dataNascimento: DATE, observacoes: {type: string, description: Dado sensível; somente Admin e Recepcao}, consentimentoLgpd: BOOL, totalNoShows: INT, ativo: BOOL}}
ClienteRequest: {type: object, required: [nome, telefone, consentimentoLgpd], properties: {nome: STR, telefone: STR, email: {type: string, format: email}, dataNascimento: DATE, observacoes: STR, consentimentoLgpd: BOOL}}
Especialidade: {type: object, properties: {id: ID, nome: STR}}
EspecialidadeRequest: {type: object, required: [nome], properties: {nome: STR}}
Servico: {type: object, properties: {id: ID, nome: STR, especialidadeId: ID, duracaoMin: INT, preco: MONEY, ativo: BOOL}}
ServicoRequest: {type: object, required: [nome, especialidadeId, duracaoMin, preco], properties: {nome: STR, especialidadeId: ID, duracaoMin: {type: integer, minimum: 1}, preco: {type: number, minimum: 0}}}
Profissional: {type: object, properties: {id: ID, unidadeId: ID, nome: STR, comissaoPadrao: MONEY, especialidadeIds: {type: array, items: ID}, ativo: BOOL}}
ProfissionalRequest: {type: object, required: [unidadeId, nome, comissaoPadrao], properties: {unidadeId: ID, nome: STR, comissaoPadrao: {type: number, minimum: 0, maximum: 100}, especialidadeIds: {type: array, items: ID}, usuarioId: ID}}
ProfissionalServico: {type: object, properties: {servicoId: ID, nome: STR, precoEfetivo: MONEY, duracaoEfetivaMin: INT, comissaoEfetiva: MONEY}}
ProfissionalServicoRequest: {type: object, required: [servicoId], properties: {servicoId: ID, precoPersonalizado: MONEY, duracaoPersonalizada: INT, comissao: MONEY}}
FaixaDisponibilidade: {type: object, required: [diaSemana, inicio, fim], properties: {diaSemana: {type: integer, minimum: 0, maximum: 6, description: 0 = domingo}, inicio: TIME, fim: TIME}}
Bloqueio: {type: object, properties: {id: ID, inicio: DT, fim: DT, motivo: STR}}
BloqueioRequest: {type: object, required: [inicio, fim], properties: {inicio: DT, fim: DT, motivo: STR}}
HorarioLivre: {type: object, properties: {profissionalId: ID, inicio: DT, fim: DT}}
HorariosLivresResponse: {type: object, properties: {data: DATE, duracaoTotalMin: INT, horarios: {type: array, items: @HorarioLivre}}}
BloqueioAgenda: {type: object, properties: {profissionalId: ID, inicio: DT, fim: DT, motivo: STR}}
AgendaResponse: {type: object, properties: {agendamentos: {type: array, items: @AgendamentoResumo}, bloqueios: {type: array, items: @BloqueioAgenda}}}
ClienteResumo: {type: object, properties: {id: ID, nome: STR}}
ProfissionalResumo: {type: object, properties: {id: ID, nome: STR}}
ItemAgendamento: {type: object, properties: {servicoId: ID, nome: STR, duracaoMin: INT, precoCongelado: MONEY}}
Agendamento: {type: object, properties: {id: ID, status: @StatusAgendamento, inicio: DT, fim: DT, unidadeId: ID, cliente: @ClienteResumo, profissional: @ProfissionalResumo, itens: {type: array, items: @ItemAgendamento}, valorTotal: MONEY, origem: @OrigemAgendamento, criadoEm: DT}}
AgendamentoResumo: {type: object, properties: {id: ID, status: @StatusAgendamento, inicio: DT, fim: DT, clienteNome: STR, profissionalId: ID, profissionalNome: STR, valorTotal: MONEY}}
CriarAgendamentoRequest: {type: object, required: [unidadeId, profissionalId, inicio, servicoIds], properties: {clienteId: {type: integer, format: int64, description: Ignorado para o perfil Cliente (usa o do token)}, unidadeId: ID, profissionalId: ID, inicio: DT, servicoIds: {type: array, minItems: 1, items: ID}, origem: @OrigemAgendamento}}
CancelarRequest: {type: object, properties: {motivo: STR}}
CancelarResponse: {type: object, properties: {id: ID, status: @StatusAgendamento, multaAplicada: BOOL, valorMulta: MONEY}}
RemarcarRequest: {type: object, required: [novoInicio], properties: {novoInicio: DT, profissionalId: ID}}
ItemComanda: {type: object, properties: {servicoId: ID, nome: STR, preco: MONEY}}
DescontoAplicado: {type: object, properties: {tipo: @TipoDesconto, valor: MONEY, valorCalculado: MONEY, motivo: STR}}
Pagamento: {type: object, properties: {id: ID, forma: @FormaPagamento, valor: MONEY, status: @StatusPagamento, pagoEm: DT}}
Comanda: {type: object, properties: {id: ID, agendamentoId: ID, clienteId: ID, status: @StatusComanda, itens: {type: array, items: @ItemComanda}, descontos: {type: array, items: @DescontoAplicado}, pagamentos: {type: array, items: @Pagamento}, subtotal: MONEY, desconto: MONEY, total: MONEY, saldo: MONEY}}
CriarComandaRequest: {type: object, description: "Informe agendamentoId OU (clienteId, profissionalId, servicoIds) para comanda avulsa", properties: {agendamentoId: ID, clienteId: ID, profissionalId: ID, servicoIds: {type: array, items: ID}}}
DescontoRequest: {type: object, required: [tipo, valor, motivo], properties: {tipo: @TipoDesconto, valor: {type: number, minimum: 0}, motivo: {type: string, minLength: 3}}}
PagamentoRequest: {type: object, required: [forma, valor], properties: {forma: @FormaPagamento, valor: {type: number, exclusiveMinimum: 0}}}
PagamentoResponse: {type: object, properties: {pagamentoId: ID, saldoRestante: MONEY, statusComanda: @StatusComanda}}
EstornoRequest: {type: object, required: [motivo], properties: {motivo: {type: string, minLength: 3}}}
Comissao: {type: object, properties: {id: ID, comandaId: ID, profissionalId: ID, valor: MONEY, status: @StatusComissao, geradaEm: DT}}
ComissaoResumoItem: {type: object, properties: {profissionalId: ID, profissionalNome: STR, totalComissao: MONEY, quantidadeAtendimentos: INT, porServico: {type: array, items: {type: object, properties: {servicoId: ID, nome: STR, total: MONEY}}}}}
FaturamentoItem: {type: object, properties: {periodo: STR, valor: MONEY, quantidadeAtendimentos: INT}}
AtendimentoRelItem: {type: object, properties: {chave: ID, nome: STR, quantidade: INT, valor: MONEY}}
Usuario: {type: object, properties: {id: ID, email: STR, perfil: @Perfil, ativo: BOOL}}
UsuarioCriarRequest: {type: object, required: [email, senhaInicial, perfil], properties: {email: {type: string, format: email}, senhaInicial: {type: string, format: password, minLength: 8}, perfil: {type: string, enum: [Admin, Recepcao, Profissional]}}}
UsuarioAtualizarRequest: {type: object, required: [perfil], properties: {perfil: @Perfil}}
SenhaRequest: {type: object, required: [novaSenha], properties: {senhaAtual: {type: string, format: password}, novaSenha: {type: string, format: password, minLength: 8}}}
StatusRequest: {type: object, required: [ativo], properties: {ativo: BOOL}}
Configuracoes: {type: object, properties: {antecedenciaMinimaMinutos: INT, antecedenciaMaximaDias: INT, prazoCancelamentoHoras: INT, intervaloHigienizacaoMinutos: INT, multaCancelamentoTardioPercentual: MONEY, limiteDescontoRecepcaoPercentual: MONEY, canaisNotificacao: {type: array, items: {type: string, enum: [WHATSAPP, SMS, EMAIL]}}, lembretesHorasAntes: {type: array, items: INT}}}
"""
TOKENS = {"ID": "{type: integer, format: int64}", "DT": "{type: string, format: date-time}",
          "DATE": "{type: string, format: date}", "TIME": "{type: string, format: time, example: '09:00:00'}",
          "STR": "{type: string}", "BOOL": "{type: boolean}", "INT": "{type: integer, format: int32}",
          "MONEY": "{type: number, format: double}"}

def expand(text):
    text = re.sub(r"@(\w+)", lambda m: "{$ref: '#/components/schemas/%s'}" % m.group(1), text)
    for k, v in TOKENS.items():
        text = re.sub(r"(?<![\w$'])%s(?![\w'])" % k, v, text)
    return text

SCHEMAS = yaml.safe_load(expand(SCHEMAS_YAML))
REF = lambda n: {"$ref": f"#/components/schemas/{n}"}

def schema_for(name):
    """Resolve Page<X>, List<X>, X? para um schema (cria PaginaX quando preciso)."""
    name = name.rstrip("?")
    m = re.fullmatch(r"(Page|List)<(\w+)>", name)
    if not m:
        return REF(name)
    kind, inner = m.groups()
    if kind == "List":
        return {"type": "array", "items": REF(inner)}
    pg = f"Pagina{inner}"
    SCHEMAS.setdefault(pg, {"type": "object", "properties": {
        "itens": {"type": "array", "items": REF(inner)}, "pagina": {"type": "integer"},
        "tamanho": {"type": "integer"}, "totalItens": {"type": "integer"}, "totalPaginas": {"type": "integer"}}})
    return REF(pg)

def qschema(typ):
    base = {"int": {"type": "integer", "format": "int64"}, "str": {"type": "string"}, "bool": {"type": "boolean"},
            "date": {"type": "string", "format": "date"}, "dt": {"type": "string", "format": "date-time"}}
    if typ in base: return base[typ]
    if typ == "ints": return {"type": "array", "items": {"type": "integer", "format": "int64"}}
    if typ.startswith("enum("): return {"type": "string", "enum": typ[5:-1].split("|")}
    return REF(typ)  # nome de schema (ex. StatusAgendamento)

def build_params(rt):
    out = []
    for n in re.findall(r"{(\w+)}", rt["path"]):
        out.append({"name": n, "in": "path", "required": True, "schema": {"type": "integer", "format": "int64"}})
    for tok in [t.strip() for t in rt["q"].split(",") if t.strip()]:
        if tok == "pg":
            out += [{"$ref": "#/components/parameters/Pagina"}, {"$ref": "#/components/parameters/Tamanho"}]; continue
        name, typ = tok.split(":", 1)
        req = not typ.endswith("?"); typ = typ.rstrip("?")
        p = {"name": name, "in": "query", "required": req, "schema": qschema(typ)}
        if typ == "ints": p.update(style="form", explode=False, description="Lista separada por vírgula (ex.: 1,2)")
        out.append(p)
    if rt["tenant"]: out.append({"$ref": "#/components/parameters/XEstabelecimento"})
    if rt["idem"]: out.append({"$ref": "#/components/parameters/IdempotencyKey"})
    return out

def roles_text(roles):
    if roles == "Pub": return "Público (sem token)"
    parts = []
    for tok in roles.split(","):
        own = tok.endswith("*"); k = tok.rstrip("*")
        parts.append(ROLE[k] + (" (somente o próprio)" if own else ""))
    return ", ".join(parts)

def build_openapi():
    paths, opids = {}, set()
    for rt in ROUTES:
        key = (rt["tag"], rt["op"]); assert key not in opids, f"operationId duplicado {key}"; opids.add(key)
        desc = [f"**Perfis:** {roles_text(rt['roles'])}."]
        if rt["err"]:
            desc.append("**Erros:** " + "; ".join(f"{c} {t}" for c, t in sorted(rt["err"].items())) + ".")
        op = {"tags": [TAGS[rt["tag"]]], "operationId": f"{rt['tag']}_{rt['op']}", "summary": rt["summary"],
              "description": "\n\n".join(desc),
              "x-perfis": [] if rt["roles"] == "Pub" else [ROLE[t.rstrip('*')] for t in rt["roles"].split(",")]}
        op["security"] = [] if rt["roles"] == "Pub" else [{"bearerAuth": []}]
        params = build_params(rt)
        if params: op["parameters"] = params
        if rt["body"]:
            optional = rt["body"].endswith("?")
            op["requestBody"] = {"required": not optional,
                                 "content": {"application/json": {"schema": schema_for(rt["body"])}}}
        code, sch = rt["ok"]
        titles = {200: "OK", 201: "Criado", 202: "Aceito", 204: "Sem conteúdo"}
        resp = {str(code): {"description": titles[code]}}
        if code == 201:
            resp["201"]["headers"] = {"Location": {"description": "URL do recurso criado", "schema": {"type": "string"}}}
        if sch:
            resp[str(code)]["content"] = {"application/json": {"schema": schema_for(sch)}}
        errs = set(rt["err"]) | ({401, 403} if rt["roles"] != "Pub" else set()) | {500}
        if rt["roles"] != "Pub" and rt["roles"] == ALL and 403 not in rt["err"]:
            errs.discard(403)
        for c in sorted(errs):
            resp[str(c)] = {"$ref": f"#/components/responses/E{c}"}
        op["responses"] = resp
        paths.setdefault(rt["path"], {})[rt["method"].lower()] = op

    def prob(desc): return {"description": desc, "content": {"application/problem+json": {"schema": REF("ProblemDetails")}}}
    return {
        "openapi": "3.0.3",
        "info": {"title": "API do Sistema de Agendamento — MVP", "version": "1.0.0",
                 "description": "Contrato do MVP (Fase 1). Datas em UTC (ISO 8601); valores monetários em BRL. "
                                "Erros seguem RFC 7807 com o campo `codigo`. O estabelecimento é sempre derivado do token "
                                "(rotas públicas usam o cabeçalho `X-Estabelecimento`)."},
        "servers": [{"url": "https://localhost:7080/api/v1", "description": "Desenvolvimento"},
                    {"url": "https://{host}/api/v1", "description": "Produção", "variables": {"host": {"default": "api.exemplo.com"}}}],
        "tags": [{"name": n} for n in TAGS.values()],
        "paths": paths,
        "components": {
            "securitySchemes": {"bearerAuth": {"type": "http", "scheme": "bearer", "bearerFormat": "JWT"}},
            "parameters": {
                "Pagina": {"name": "pagina", "in": "query", "schema": {"type": "integer", "minimum": 1, "default": 1}},
                "Tamanho": {"name": "tamanho", "in": "query", "schema": {"type": "integer", "minimum": 1, "maximum": 100, "default": 20}},
                "XEstabelecimento": {"name": "X-Estabelecimento", "in": "header", "required": True,
                                     "description": "Slug do estabelecimento (rotas públicas)", "schema": {"type": "string"}},
                "IdempotencyKey": {"name": "Idempotency-Key", "in": "header", "required": False,
                                   "description": "GUID para repetir a chamada sem duplicar efeitos", "schema": {"type": "string", "format": "uuid"}}},
            "responses": {"E400": prob("Requisição inválida (VAL_INVALIDO)"), "E401": prob("Não autenticado"),
                          "E403": prob("Sem permissão (AUTH_PERMISSAO)"), "E404": prob("Não encontrado (RECURSO_NAO_ENCONTRADO)"),
                          "E409": prob("Conflito de estado"), "E422": prob("Regra de negócio violada"),
                          "E429": prob("Excesso de requisições (RATE_LIMIT)"), "E500": prob("Erro interno"),
                          "E503": prob("Serviço indisponível")},
            "schemas": SCHEMAS}}

def check_refs(doc):
    bad = []
    def walk(n):
        if isinstance(n, dict):
            for k, v in n.items():
                if k == "$ref" and isinstance(v, str) and v.startswith("#/"):
                    cur = doc
                    try:
                        for part in v[2:].split("/"): cur = cur[part]
                    except KeyError: bad.append(v)
                else: walk(v)
        elif isinstance(n, list):
            for i in n: walk(i)
    walk(doc); return bad

def cs_stubs():
    out = pathlib.Path(ROOT / "src/Agenda.Api/Controllers/Generated"); out.mkdir(parents=True, exist_ok=True)
    for f in out.glob("*.Stubs.cs"): f.unlink()
    by_tag = {}
    for rt in ROUTES:
        k = (rt["method"], rt["path"])
        if k in REAIS or k in BUILTIN: continue
        by_tag.setdefault(rt["tag"], []).append(rt)
    for tag, items in by_tag.items():
        lines = ["// <auto-generated>", "// Gerado por tools/gen_contract.py a partir da tabela de rotas. NÃO EDITE.",
                 "// Para implementar uma rota: mova-a para a parte manual do controller e inclua em REAIS no gerador.",
                 "// </auto-generated>", "using Microsoft.AspNetCore.Authorization;", "using Microsoft.AspNetCore.Mvc;"]
        if tag == "Autenticacao": lines.append("using Microsoft.AspNetCore.RateLimiting;")
        lines += ["", "namespace Agenda.Api.Controllers;", ""]
        if tag == "Autenticacao": lines.append('[EnableRateLimiting("auth")]')
        lines += [f"public partial class {tag}Controller : ApiControllerBase", "{"]
        for i, rt in enumerate(items):
            tmpl = re.sub(r"{(\w+)}", r"{\1:long}", rt["path"]).lstrip("/")
            verb = rt["method"].capitalize()
            args = ", ".join(f"long {n}" for n in re.findall(r"{(\w+)}", rt["path"]))
            auth = "[AllowAnonymous]" if rt["roles"] == "Pub" else \
                '[Authorize(Roles = "%s")]' % ",".join(ROLE[t.rstrip("*")] for t in rt["roles"].split(","))
            todo = ""
            if "*" in rt["roles"]:
                own = [ROLE[t.rstrip("*")] for t in rt["roles"].split(",") if t.endswith("*")]
                todo = f"    // TODO(propriedade): perfis {', '.join(own)} só podem acessar os próprios dados.\n"
            summ = rt["summary"].replace("&", "&amp;").replace("<", "&lt;").replace(">", "&gt;")
            lines.append(f"    /// <summary>{summ}</summary>")
            lines.append(f"    /// <remarks>{rt['method']} /api/v1{rt['path']}</remarks>")
            lines.append(f'    [Http{verb}("{tmpl}")]')
            lines.append(f"    {auth}")
            if todo: lines.append(todo.rstrip("\n"))
            lines.append(f"    public IActionResult {rt['op']}({args}) => NaoImplementado();")
            if i < len(items) - 1: lines.append("")
        lines += ["}", ""]
        (out / f"{tag}Controller.Stubs.cs").write_text("\n".join(lines), encoding="utf-8")
    return {t: len(v) for t, v in by_tag.items()}

if __name__ == "__main__":
    doc = build_openapi()
    bad = check_refs(doc)
    if bad: print("REFS QUEBRADAS:", sorted(set(bad))); sys.exit(1)
    n_ops = sum(len(v) for v in doc["paths"].values())
    assert n_ops == len(ROUTES) == 69, (n_ops, len(ROUTES))
    (ROOT / "docs").mkdir(exist_ok=True)
    with open(ROOT / "docs/openapi.yaml", "w", encoding="utf-8") as f:
        yaml.dump(doc, f, sort_keys=False, allow_unicode=True, default_flow_style=False, width=110)
    stubs = cs_stubs()
    print(f"OK: {n_ops} operações, {len(doc['components']['schemas'])} schemas")
    print("stubs por controller:", stubs)
