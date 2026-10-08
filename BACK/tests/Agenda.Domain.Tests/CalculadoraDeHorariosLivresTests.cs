using Agenda.Domain.Agendamentos;

namespace Agenda.Domain.Tests;

public class CalculadoraDeHorariosLivresTests
{
    private readonly CalculadoraDeHorariosLivres _calculadora = new();
    private static readonly DateOnly Segunda = new(2030, 1, 7);

    [Fact] // CT-AGE-21
    public void Gera_janelas_de_15_em_15_minutos_dentro_da_faixa_do_profissional()
    {
        // Profissional 09:00–12:00, serviço de 30 min -> inícios de 09:00 até 11:30.
        var profissional = Dados.Barbeiro(new TimeOnly(9, 0), new TimeOnly(12, 0));

        var horarios = _calculadora.Calcular(Dados.Unidade(), Dados.Config(), [Dados.Corte()],
            [new AgendaDoProfissional(profissional, [])], Segunda, Dados.Agora);

        horarios.Should().HaveCount(11);
        horarios.First().InicioUtc.Should().Be(new DateTime(2030, 1, 7, 12, 0, 0, DateTimeKind.Utc)); // 09:00 locais
        horarios.Last().InicioUtc.Should().Be(new DateTime(2030, 1, 7, 14, 30, 0, DateTimeKind.Utc)); // 11:30 locais
    }

    [Fact]
    public void Remove_as_janelas_que_sobrepoem_um_agendamento_existente()
    {
        var profissional = Dados.Barbeiro(new TimeOnly(9, 0), new TimeOnly(12, 0));
        var existente = Dados.Agendamento(new DateTime(2030, 1, 7, 13, 0, 0, DateTimeKind.Utc), 30); // 10:00–10:30 locais

        var horarios = _calculadora.Calcular(Dados.Unidade(), Dados.Config(), [Dados.Corte()],
            [new AgendaDoProfissional(profissional, [existente])], Segunda, Dados.Agora);

        // Saem 09:45, 10:00 e 10:15. Ficam 09:30 (termina às 10:00) e 10:30 (começa às 10:30).
        horarios.Should().HaveCount(8);
        horarios.Select(h => h.InicioUtc).Should().NotContain(new[]
        {
            new DateTime(2030, 1, 7, 12, 45, 0, DateTimeKind.Utc), // 09:45 locais
            new DateTime(2030, 1, 7, 13, 0, 0, DateTimeKind.Utc),  // 10:00 locais
            new DateTime(2030, 1, 7, 13, 15, 0, DateTimeKind.Utc), // 10:15 locais
        });
    }

    [Fact]
    public void Servicos_somados_exigem_janela_maior()
    {
        // Corte (30) + barba (20) = 50 min numa faixa 09:00–10:00: só o início das 09:00 cabe (09:15 + 50 min = 10:05).
        var profissional = Dados.Barbeiro(new TimeOnly(9, 0), new TimeOnly(10, 0));

        var horarios = _calculadora.Calcular(Dados.Unidade(), Dados.Config(), [Dados.Corte(), Dados.Barba()],
            [new AgendaDoProfissional(profissional, [])], Segunda, Dados.Agora);

        horarios.Should().ContainSingle().Which.InicioUtc.Should().Be(new DateTime(2030, 1, 7, 12, 0, 0, DateTimeKind.Utc));
    }

    [Fact]
    public void Dia_em_que_o_profissional_nao_trabalha_nao_tem_horarios()
    {
        var domingo = new DateOnly(2030, 1, 6);

        var horarios = _calculadora.Calcular(Dados.Unidade(), Dados.Config(), [Dados.Corte()],
            [new AgendaDoProfissional(Dados.Barbeiro(), [])], domingo, Dados.Agora);

        horarios.Should().BeEmpty();
    }

    [Fact]
    public void Respeita_a_antecedencia_minima_no_proprio_dia()
    {
        var profissional = Dados.Barbeiro(new TimeOnly(9, 0), new TimeOnly(12, 0));
        // "Agora" = segunda 10:00 locais (13:00 UTC); antecedência 60 min -> primeiro horário possível 11:00 locais.
        var agora = new DateTime(2030, 1, 7, 13, 0, 0, DateTimeKind.Utc);

        var horarios = _calculadora.Calcular(Dados.Unidade(), Dados.Config(), [Dados.Corte()],
            [new AgendaDoProfissional(profissional, [])], Segunda, agora);

        horarios.First().InicioUtc.Should().Be(new DateTime(2030, 1, 7, 14, 0, 0, DateTimeKind.Utc));
    }
}
