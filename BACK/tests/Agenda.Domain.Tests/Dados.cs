using Agenda.Domain.Agendamentos;
using Agenda.Domain.Cadastro;
using Agenda.Domain.Common;

namespace Agenda.Domain.Tests;

/// <summary>Massa de teste (Object Mother). Datas fixas: nada de DateTime.Now nos testes.</summary>
internal static class Dados
{
    public const long Tenant = 1;

    /// <summary>Sexta-feira 04/01/2030 12:00 UTC (09:00 em São Paulo).</summary>
    public static readonly DateTime Agora = new(2030, 1, 4, 12, 0, 0, DateTimeKind.Utc);

    /// <summary>Segunda-feira 07/01/2030 às 11:00 locais (14:00 UTC; São Paulo = UTC-3, sem horário de verão).</summary>
    public static readonly DateTime SegundaOnzeLocal = new(2030, 1, 7, 14, 0, 0, DateTimeKind.Utc);

    public static T ComId<T>(this T entidade, long id) where T : Entity
    {
        typeof(Entity).GetProperty(nameof(Entity.Id))!.SetValue(entidade, id);
        return entidade;
    }

    public static Unidade Unidade() =>
        Cadastro.Unidade.Criar(Tenant, "Centro", "Rua A, 10", "11999990000",
            new TimeOnly(9, 0), new TimeOnly(19, 0), "America/Sao_Paulo").ComId(1);

    public static Servico Corte() =>
        Servico.Criar(Tenant, 1, "Corte", 30, 40m).ComId(1);

    public static Servico Barba() =>
        Servico.Criar(Tenant, 1, "Barba", 20, 30m).ComId(2);

    /// <summary>Seg a sex, 09:00–18:00 locais; habilitado para corte e barba.</summary>
    public static Profissional Barbeiro(TimeOnly? inicio = null, TimeOnly? fim = null)
    {
        var p = Profissional.Criar(Tenant, 1, "João", 40m).ComId(1);
        p.DefinirServicos([new ProfissionalServico(1), new ProfissionalServico(2)]);
        p.DefinirDisponibilidade(
            new[] { DayOfWeek.Monday, DayOfWeek.Tuesday, DayOfWeek.Wednesday, DayOfWeek.Thursday, DayOfWeek.Friday }
                .Select(d => new Disponibilidade(d, inicio ?? new TimeOnly(9, 0), fim ?? new TimeOnly(18, 0))));
        return p;
    }

    public static ConfiguracaoEstabelecimento Config() => ConfiguracaoEstabelecimento.Padrao(Tenant);

    public static Cliente Cliente() =>
        Cadastro.Cliente.Criar(Tenant, "Carlos Souza", "(11) 99999-0001", null, null, null, true, Agora).ComId(1);

    /// <summary>Agendamento de um item com a duração informada (corte, R$ 40).</summary>
    public static Agendamento Agendamento(DateTime inicioUtc, int minutos = 30, long profissionalId = 1, long clienteId = 1) =>
        Domain.Agendamentos.Agendamento.Criar(Tenant, 1, clienteId, profissionalId, inicioUtc,
            [new ItemAgendamento(1, "Corte", minutos, 40m)], 0, "BALCAO", Agora);
}
