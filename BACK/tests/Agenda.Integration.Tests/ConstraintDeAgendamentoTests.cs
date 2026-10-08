using Agenda.Domain.Agendamentos;
using Agenda.Domain.Cadastro;
using Agenda.Domain.Common;
using Agenda.Infrastructure.Persistence;
using Agenda.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;

namespace Agenda.Integration.Tests;

public class ConstraintDeAgendamentoTests(PostgresFixture banco) : IClassFixture<PostgresFixture>
{
    private sealed record Semente(long Tenant, long Unidade, long ClienteA, long ClienteB, long Profissional);

    private static readonly DateTime Inicio = new(2030, 1, 7, 13, 0, 0, DateTimeKind.Utc);

    /// <summary>Cada teste cria o seu próprio estabelecimento: testes independentes, sem limpeza entre eles.</summary>
    private async Task<Semente> SemearAsync()
    {
        long tenant;
        await using (var db0 = banco.NovoContexto(0))
        {
            var estabelecimento = Estabelecimento.Criar("Barbearia Teste", "00.000.000/0001-91", $"teste-{Guid.NewGuid():N}");
            db0.Estabelecimentos.Add(estabelecimento);
            await db0.SaveChangesAsync();
            tenant = estabelecimento.Id;
        }

        await using var db = banco.NovoContexto(tenant);
        var unidade = Unidade.Criar(tenant, "Centro", "Rua A, 10", "11999990000", new TimeOnly(9, 0), new TimeOnly(19, 0), "America/Sao_Paulo");
        var ana = Cliente.Criar(tenant, "Ana", "11999990001", null, null, null, true, DateTime.UtcNow);
        var bia = Cliente.Criar(tenant, "Bia", "11999990002", null, null, null, true, DateTime.UtcNow);
        db.AddRange(unidade, ana, bia);
        await db.SaveChangesAsync();

        var joao = Profissional.Criar(tenant, unidade.Id, "João", 40m);
        db.Add(joao);
        await db.SaveChangesAsync();

        return new Semente(tenant, unidade.Id, ana.Id, bia.Id, joao.Id);
    }

    private async Task GravarAsync(Semente s, long clienteId, DateTime inicio, long? profissionalId = null)
    {
        await using var db = banco.NovoContexto(s.Tenant);
        var uow = new EfUnitOfWork(db, new SemEventos());
        var repositorio = new AgendamentoRepository(db);

        var agendamento = Agendamento.Criar(s.Tenant, s.Unidade, clienteId, profissionalId ?? s.Profissional, inicio,
            [new ItemAgendamento(1, "Corte", 30, 40m)], 0, "TESTE", DateTime.UtcNow);

        await uow.ExecutarEmTransacaoAsync(async () =>
        {
            await repositorio.AdicionarAsync(agendamento, CancellationToken.None);
            return 0;
        }, CancellationToken.None);
    }

    [Fact] // CT-AGE-02 no nível do banco
    public async Task Banco_barra_agendamento_sobreposto_do_mesmo_profissional()
    {
        var s = await SemearAsync();
        await GravarAsync(s, s.ClienteA, Inicio);

        var act = () => GravarAsync(s, s.ClienteB, Inicio.AddMinutes(15));

        var ex = (await act.Should().ThrowAsync<DomainException>()).Which;
        ex.Codigo.Should().Be("AGE_HORARIO_OCUPADO");
        ex.Tipo.Should().Be(TipoErro.Conflito);
    }

    [Fact] // CT-AGE-03: intervalo semiaberto também no banco
    public async Task Banco_permite_agendamentos_encostados()
    {
        var s = await SemearAsync();
        await GravarAsync(s, s.ClienteA, Inicio);

        var act = () => GravarAsync(s, s.ClienteB, Inicio.AddMinutes(30));

        await act.Should().NotThrowAsync();
    }

    [Fact] // CT-CON-01: 20 requisições simultâneas, exatamente 1 vence
    public async Task Vinte_gravacoes_simultaneas_no_mesmo_horario_geram_um_unico_agendamento()
    {
        var s = await SemearAsync();

        var tarefas = Enumerable.Range(0, 20).Select(_ => Task.Run(async () =>
        {
            try
            {
                await GravarAsync(s, s.ClienteA, Inicio);
                return true;
            }
            catch (DomainException ex) when (ex.Codigo == "AGE_HORARIO_OCUPADO")
            {
                return false;
            }
        }));

        var resultados = await Task.WhenAll(tarefas);

        resultados.Count(vitoria => vitoria).Should().Be(1);
        resultados.Count(vitoria => !vitoria).Should().Be(19);
    }

    [Fact] // CT-TEN-01/02: isolamento entre estabelecimentos
    public async Task Tenant_nao_enxerga_agendamentos_de_outro_tenant()
    {
        var a = await SemearAsync();
        var b = await SemearAsync();
        await GravarAsync(a, a.ClienteA, Inicio);

        await using var dbDoTenantB = banco.NovoContexto(b.Tenant);
        var visiveis = await dbDoTenantB.Agendamentos.CountAsync();

        visiveis.Should().Be(0);
    }
}
