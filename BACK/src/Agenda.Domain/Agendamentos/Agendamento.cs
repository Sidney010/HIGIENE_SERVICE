using Agenda.Domain.Agendamentos.Events;
using Agenda.Domain.Common;

namespace Agenda.Domain.Agendamentos;

public sealed record ResultadoCancelamento(bool MultaAplicada, decimal ValorMulta);

public sealed class Agendamento : AggregateRoot
{
    private readonly List<ItemAgendamento> _itens = new();

    private Agendamento() { }

    public long UnidadeId { get; private set; }
    public long ClienteId { get; private set; }
    public long ProfissionalId { get; private set; }
    public long? RecursoId { get; private set; }
    public DateTime Inicio { get; private set; }
    /// <summary>Inclui o intervalo de higienização configurado.</summary>
    public DateTime Fim { get; private set; }
    public StatusAgendamento Status { get; private set; }
    public string? Origem { get; private set; }
    public decimal ValorTotal { get; private set; }
    public string? MotivoCancelamento { get; private set; }
    public DateTime? CheckInEmUtc { get; private set; }
    public long? RemarcadoParaId { get; private set; }
    public DateTime CriadoEmUtc { get; private set; }

    public IReadOnlyCollection<ItemAgendamento> Itens => _itens;
    public Intervalo Intervalo => new(Inicio, Fim);

    // ------------------------------------------------------------------ criação
    public static Agendamento Criar(long estabelecimentoId, long unidadeId, long clienteId, long profissionalId,
        DateTime inicioUtc, IEnumerable<ItemAgendamento> itens, int intervaloHigienizacaoMin, string? origem, DateTime agoraUtc)
    {
        var lista = itens.ToList();
        if (lista.Count == 0) throw DomainException.Invalido("Informe ao menos um serviço.");

        var inicio = DataHora.GarantirUtc(inicioUtc);
        var duracao = lista.Sum(i => i.DuracaoMin) + Math.Max(0, intervaloHigienizacaoMin);

        var agendamento = new Agendamento
        {
            EstabelecimentoId = estabelecimentoId,
            UnidadeId = unidadeId,
            ClienteId = clienteId,
            ProfissionalId = profissionalId,
            Inicio = inicio,
            Fim = inicio.AddMinutes(duracao),
            Status = StatusAgendamento.PENDENTE,
            Origem = origem,
            ValorTotal = lista.Sum(i => i.PrecoCongelado),
            CriadoEmUtc = DataHora.GarantirUtc(agoraUtc),
        };
        agendamento._itens.AddRange(lista);
        agendamento.Raise(new AgendamentoCriado(agendamento));
        return agendamento;
    }

    // ------------------------------------------------------------------ regras de agenda
    /// <summary>Status que ocupam o horário do profissional/cliente.</summary>
    public static readonly StatusAgendamento[] StatusQueOcupamAgenda =
    [
        StatusAgendamento.PENDENTE, StatusAgendamento.CONFIRMADO,
        StatusAgendamento.EM_ATENDIMENTO, StatusAgendamento.CONCLUIDO,
    ];

    public bool OcupaAgenda => StatusQueOcupamAgenda.Contains(Status);

    public bool ConflitaComProfissional(Agendamento outro) =>
        ProfissionalId == outro.ProfissionalId && OcupaAgenda && outro.OcupaAgenda && Intervalo.Sobrepoe(outro.Intervalo);

    public bool ConflitaComCliente(Agendamento outro) =>
        ClienteId == outro.ClienteId && OcupaAgenda && outro.OcupaAgenda && Intervalo.Sobrepoe(outro.Intervalo);

    /// <summary>Atalho usado nos testes: conflito de agenda do profissional.</summary>
    public bool Conflita(Agendamento outro) => ConflitaComProfissional(outro);

    // ------------------------------------------------------------------ ciclo de vida
    private static readonly Dictionary<StatusAgendamento, StatusAgendamento[]> Permitidas = new()
    {
        [StatusAgendamento.PENDENTE] = [StatusAgendamento.CONFIRMADO, StatusAgendamento.CANCELADO, StatusAgendamento.REMARCADO, StatusAgendamento.NO_SHOW],
        [StatusAgendamento.CONFIRMADO] = [StatusAgendamento.EM_ATENDIMENTO, StatusAgendamento.CANCELADO, StatusAgendamento.REMARCADO, StatusAgendamento.NO_SHOW],
        [StatusAgendamento.EM_ATENDIMENTO] = [StatusAgendamento.CONCLUIDO],
    };

    private void MudarPara(StatusAgendamento novo)
    {
        if (!Permitidas.TryGetValue(Status, out var destinos) || !destinos.Contains(novo))
            throw new DomainException("AGE_TRANSICAO_INVALIDA",
                $"Não é possível mudar de {Status} para {novo}.", TipoErro.Conflito);
        Status = novo;
    }

    public void Confirmar()
    {
        MudarPara(StatusAgendamento.CONFIRMADO);
        Raise(new AgendamentoConfirmado(this));
    }

    /// <summary>Registra a presença; um agendamento PENDENTE é confirmado implicitamente.</summary>
    public void RegistrarCheckIn(DateTime agoraUtc)
    {
        if (Status == StatusAgendamento.PENDENTE) Confirmar();
        if (Status != StatusAgendamento.CONFIRMADO)
            throw new DomainException("AGE_TRANSICAO_INVALIDA", $"Check-in não permitido em {Status}.", TipoErro.Conflito);
        CheckInEmUtc = DataHora.GarantirUtc(agoraUtc);
    }

    public void Iniciar() => MudarPara(StatusAgendamento.EM_ATENDIMENTO);

    public void Concluir()
    {
        MudarPara(StatusAgendamento.CONCLUIDO);
        Raise(new AtendimentoConcluido(this));
    }

    public void MarcarNoShow()
    {
        MudarPara(StatusAgendamento.NO_SHOW);
        Raise(new NoShowRegistrado(this));
    }

    public ResultadoCancelamento Cancelar(string? motivo, DateTime agoraUtc, int prazoCancelamentoHoras, decimal multaPercentual)
    {
        MudarPara(StatusAgendamento.CANCELADO);
        MotivoCancelamento = motivo?.Trim();

        var tardio = DataHora.GarantirUtc(agoraUtc) > Inicio.AddHours(-prazoCancelamentoHoras);
        var multa = tardio ? Math.Round(ValorTotal * multaPercentual / 100m, 2, MidpointRounding.AwayFromZero) : 0m;

        var resultado = new ResultadoCancelamento(multa > 0, multa);
        Raise(new AgendamentoCancelado(this, resultado.MultaAplicada, resultado.ValorMulta));
        return resultado;
    }

    /// <summary>Marca este como REMARCADO apontando para o novo agendamento (criado pelo caso de uso).</summary>
    public void MarcarComoRemarcado(Agendamento novo)
    {
        MudarPara(StatusAgendamento.REMARCADO);
        RemarcadoParaId = novo.Id == 0 ? null : novo.Id;
        Raise(new AgendamentoRemarcado(this));
    }
}
