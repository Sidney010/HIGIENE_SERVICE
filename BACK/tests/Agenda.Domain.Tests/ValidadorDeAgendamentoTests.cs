using Agenda.Domain.Agendamentos;
using Agenda.Domain.Cadastro;
using Agenda.Domain.Common;

namespace Agenda.Domain.Tests;

public class ValidadorDeAgendamentoTests
{
    private readonly ValidadorDeAgendamento _validador = new();
    private readonly Unidade _unidade = Dados.Unidade();
    private readonly ConfiguracaoEstabelecimento _config = Dados.Config();

    private ContextoValidacao Contexto(
        Agendamento novo, Profissional profissional,
        IReadOnlyCollection<Agendamento>? doProfissional = null,
        IReadOnlyCollection<Agendamento>? doCliente = null,
        IReadOnlyCollection<long>? servicoIds = null) =>
        new(novo, profissional, _unidade, _config, servicoIds ?? [1], doProfissional ?? [], doCliente ?? [], Dados.Agora);

    private static Agendamento Novo(DateTime inicio, int minutos = 30) => Dados.Agendamento(inicio, minutos);

    private void DeveFalharCom(ContextoValidacao ctx, string codigo) =>
        FluentActions.Invoking(() => _validador.Validar(ctx))
            .Should().Throw<DomainException>().Which.Codigo.Should().Be(codigo);

    [Fact]
    public void Horario_valido_nao_lanca()
    {
        var act = () => _validador.Validar(Contexto(Novo(Dados.SegundaOnzeLocal), Dados.Barbeiro()));

        act.Should().NotThrow();
    }

    [Fact] // CT-AGE-09
    public void Profissional_nao_habilitado_para_o_servico()
    {
        var semHabilitacao = Profissional.Criar(Dados.Tenant, 1, "Maria", 30m).ComId(2);
        semHabilitacao.DefinirDisponibilidade([new Disponibilidade(DayOfWeek.Monday, new TimeOnly(9, 0), new TimeOnly(18, 0))]);

        DeveFalharCom(Contexto(Novo(Dados.SegundaOnzeLocal), semHabilitacao), "AGE_PROFISSIONAL_NAO_HABILITADO");
    }

    [Fact] // CT-AGE-10
    public void Antecedencia_menor_que_a_minima()
    {
        // Antecedência padrão: 60 min. Aqui faltam 10 min.
        var inicio = Dados.Agora.AddMinutes(10);

        DeveFalharCom(Contexto(Novo(inicio), Dados.Barbeiro()), "AGE_ANTECEDENCIA_INVALIDA");
    }

    [Fact] // CT-AGE-11
    public void Antecedencia_maior_que_a_maxima()
    {
        var inicio = Dados.Agora.AddDays(61);

        DeveFalharCom(Contexto(Novo(inicio), Dados.Barbeiro()), "AGE_ANTECEDENCIA_INVALIDA");
    }

    [Fact] // CT-AGE-04
    public void Fora_do_dia_de_trabalho_do_profissional()
    {
        var domingo = new DateTime(2030, 1, 6, 15, 0, 0, DateTimeKind.Utc); // 12:00 locais

        DeveFalharCom(Contexto(Novo(domingo), Dados.Barbeiro()), "AGE_FORA_DISPONIBILIDADE");
    }

    [Fact]
    public void Termina_depois_do_fim_da_faixa_do_profissional()
    {
        // Faixa até 18:00 locais. Começa 17:45 (20:45 UTC) e dura 30 min -> termina 18:15.
        var inicio = new DateTime(2030, 1, 7, 20, 45, 0, DateTimeKind.Utc);

        DeveFalharCom(Contexto(Novo(inicio), Dados.Barbeiro()), "AGE_FORA_DISPONIBILIDADE");
    }

    [Fact] // CT-AGE-06
    public void Horario_bloqueado_por_folga_ou_almoco()
    {
        var profissional = Dados.Barbeiro();
        profissional.AdicionarBloqueio(new Bloqueio(
            Dados.SegundaOnzeLocal.AddMinutes(-30), Dados.SegundaOnzeLocal.AddMinutes(60), "Almoço"));

        var ex = FluentActions.Invoking(() => _validador.Validar(Contexto(Novo(Dados.SegundaOnzeLocal), profissional)))
            .Should().Throw<DomainException>().Which;

        ex.Codigo.Should().Be("AGE_BLOQUEIO");
        ex.Tipo.Should().Be(TipoErro.Conflito);
    }

    [Fact] // CT-AGE-02
    public void Horario_ja_ocupado_pelo_profissional()
    {
        var existente = Novo(Dados.SegundaOnzeLocal.AddMinutes(15));

        DeveFalharCom(Contexto(Novo(Dados.SegundaOnzeLocal), Dados.Barbeiro(), doProfissional: [existente]), "AGE_HORARIO_OCUPADO");
    }

    [Fact] // CT-AGE-03
    public void Agendamentos_encostados_sao_permitidos()
    {
        var existente = Novo(Dados.SegundaOnzeLocal.AddMinutes(-30));

        var act = () => _validador.Validar(Contexto(Novo(Dados.SegundaOnzeLocal), Dados.Barbeiro(), doProfissional: [existente]));

        act.Should().NotThrow();
    }

    [Fact] // CT-AGE-12
    public void Cliente_ja_tem_agendamento_no_mesmo_horario_com_outro_profissional()
    {
        var doCliente = Dados.Agendamento(Dados.SegundaOnzeLocal, 30, profissionalId: 2, clienteId: 1);

        DeveFalharCom(Contexto(Novo(Dados.SegundaOnzeLocal), Dados.Barbeiro(), doCliente: [doCliente]), "AGE_CLIENTE_CONFLITO");
    }
}
