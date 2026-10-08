using Agenda.Domain.Agendamentos;
using Agenda.Domain.Agendamentos.Events;
using Agenda.Domain.Common;

namespace Agenda.Domain.Tests;

public class AgendamentoTests
{
    private static readonly DateTime Inicio = Dados.SegundaOnzeLocal;

    [Fact] // CT-AGE-01 / CT-AGE-07
    public void Criar_soma_duracao_e_precos_dos_itens()
    {
        var itens = new[]
        {
            new ItemAgendamento(1, "Corte", 30, 40m),
            new ItemAgendamento(2, "Barba", 20, 30m),
        };

        var a = Agendamento.Criar(1, 1, 1, 1, Inicio, itens, 0, "BALCAO", Dados.Agora);

        a.Fim.Should().Be(Inicio.AddMinutes(50));
        a.ValorTotal.Should().Be(70m);
        a.Status.Should().Be(StatusAgendamento.PENDENTE);
        a.DomainEvents.Should().ContainSingle(e => e is AgendamentoCriado);
    }

    [Fact] // CT-AGE-08
    public void Criar_inclui_intervalo_de_higienizacao_no_fim()
    {
        var a = Agendamento.Criar(1, 1, 1, 1, Inicio, [new ItemAgendamento(1, "Corte", 30, 40m)], 10, null, Dados.Agora);

        a.Fim.Should().Be(Inicio.AddMinutes(40));
    }

    [Fact]
    public void Criar_sem_itens_e_invalido()
    {
        var act = () => Agendamento.Criar(1, 1, 1, 1, Inicio, [], 0, null, Dados.Agora);

        act.Should().Throw<DomainException>().Which.Codigo.Should().Be("VAL_INVALIDO");
    }

    [Theory] // CT-AGE-02 / CT-AGE-03: intervalo semiaberto [início, fim)
    [InlineData(0, 30, true)]    // mesmo horário
    [InlineData(-15, 30, true)]  // começa antes e entra no existente
    [InlineData(15, 30, true)]   // começa dentro do existente
    [InlineData(-30, 30, false)] // termina exatamente quando o existente começa
    [InlineData(30, 30, false)]  // começa exatamente quando o existente termina
    public void Conflita_respeita_intervalo_semiaberto(int deslocamentoMin, int duracaoMin, bool esperado)
    {
        var existente = Dados.Agendamento(Inicio, 30);
        var novo = Dados.Agendamento(Inicio.AddMinutes(deslocamentoMin), duracaoMin);

        novo.Conflita(existente).Should().Be(esperado);
    }

    [Fact]
    public void Profissionais_diferentes_nao_conflitam()
    {
        var a = Dados.Agendamento(Inicio, 30, profissionalId: 1);
        var b = Dados.Agendamento(Inicio, 30, profissionalId: 2);

        a.ConflitaComProfissional(b).Should().BeFalse();
    }

    [Fact]
    public void Agendamento_cancelado_libera_o_horario()
    {
        var existente = Dados.Agendamento(Inicio, 30);
        existente.Cancelar("desistiu", Dados.Agora, 2, 0);
        var novo = Dados.Agendamento(Inicio, 30);

        novo.Conflita(existente).Should().BeFalse();
    }

    [Fact] // CT-AGE-14
    public void Fluxo_normal_pendente_confirmado_em_atendimento_concluido()
    {
        var a = Dados.Agendamento(Inicio);

        a.Confirmar();
        a.Iniciar();
        a.Concluir();

        a.Status.Should().Be(StatusAgendamento.CONCLUIDO);
        a.DomainEvents.Should().Contain(e => e is AtendimentoConcluido);
    }

    [Fact] // CT-AGE-15
    public void Concluir_agendamento_cancelado_e_transicao_invalida()
    {
        var a = Dados.Agendamento(Inicio);
        a.Cancelar(null, Dados.Agora, 2, 0);

        var act = () => a.Concluir();

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Codigo.Should().Be("AGE_TRANSICAO_INVALIDA");
        ex.Tipo.Should().Be(TipoErro.Conflito);
    }

    [Theory]
    [InlineData(StatusAgendamento.CONCLUIDO)]
    [InlineData(StatusAgendamento.NO_SHOW)]
    [InlineData(StatusAgendamento.CANCELADO)]
    public void Estados_finais_nao_aceitam_nova_transicao(StatusAgendamento final)
    {
        var a = Dados.Agendamento(Inicio);
        Levar(a, final);

        var act = () => a.Confirmar();

        act.Should().Throw<DomainException>().Which.Codigo.Should().Be("AGE_TRANSICAO_INVALIDA");
    }

    [Fact] // CT-AGE-16
    public void Cancelar_dentro_do_prazo_nao_aplica_multa()
    {
        var a = Dados.Agendamento(Inicio); // 3 dias depois de "agora"

        var r = a.Cancelar("imprevisto", Dados.Agora, prazoCancelamentoHoras: 2, multaPercentual: 50);

        r.MultaAplicada.Should().BeFalse();
        r.ValorMulta.Should().Be(0m);
        a.Status.Should().Be(StatusAgendamento.CANCELADO);
    }

    [Fact] // CT-AGE-17
    public void Cancelar_fora_do_prazo_aplica_multa_percentual()
    {
        var a = Dados.Agendamento(Inicio); // R$ 40
        var umaHoraAntes = Inicio.AddHours(-1);

        var r = a.Cancelar(null, umaHoraAntes, prazoCancelamentoHoras: 2, multaPercentual: 50);

        r.MultaAplicada.Should().BeTrue();
        r.ValorMulta.Should().Be(20.00m);
    }

    [Fact] // CT-AGE-20
    public void No_show_muda_status_e_dispara_evento()
    {
        var a = Dados.Agendamento(Inicio);
        a.Confirmar();

        a.MarcarNoShow();

        a.Status.Should().Be(StatusAgendamento.NO_SHOW);
        a.DomainEvents.Should().Contain(e => e is NoShowRegistrado);
    }

    [Fact]
    public void Check_in_em_pendente_confirma_implicitamente()
    {
        var a = Dados.Agendamento(Inicio);

        a.RegistrarCheckIn(Inicio.AddMinutes(-5));

        a.Status.Should().Be(StatusAgendamento.CONFIRMADO);
        a.CheckInEmUtc.Should().Be(Inicio.AddMinutes(-5));
    }

    private static void Levar(Agendamento a, StatusAgendamento destino)
    {
        switch (destino)
        {
            case StatusAgendamento.CANCELADO: a.Cancelar(null, Dados.Agora, 2, 0); break;
            case StatusAgendamento.NO_SHOW: a.MarcarNoShow(); break;
            case StatusAgendamento.CONCLUIDO: a.Confirmar(); a.Iniciar(); a.Concluir(); break;
        }
    }
}
