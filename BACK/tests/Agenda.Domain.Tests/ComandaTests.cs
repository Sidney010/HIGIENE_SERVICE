using Agenda.Domain.Common;
using Agenda.Domain.Financeiro;

namespace Agenda.Domain.Tests;

public class ComandaTests
{
    /// <summary>Corte R$ 40 com comissão de 40%.</summary>
    private static Comanda ComandaDeCorte() =>
        Comanda.Abrir(Dados.Tenant, 1, null, [new ItemComanda(1, "Corte", 40m, 1, 40m)], Dados.Agora);

    private static void DeveFalharCom(Action acao, string codigo) =>
        acao.Should().Throw<DomainException>().Which.Codigo.Should().Be(codigo);

    [Fact] // CT-FIN-03
    public void Pagamento_integral_fecha_a_comanda_e_dispara_evento()
    {
        var c = ComandaDeCorte();

        c.RegistrarPagamento(FormaPagamento.PIX, 40m, Dados.Agora);

        c.Status.Should().Be(StatusComanda.PAGA);
        c.Saldo.Should().Be(0m);
        c.DomainEvents.Should().ContainSingle(e => e is PagamentoConfirmado);
    }

    [Fact] // CT-FIN-04
    public void Pagamento_dividido_so_fecha_quando_a_soma_zera_o_saldo()
    {
        var c = ComandaDeCorte();

        c.RegistrarPagamento(FormaPagamento.DINHEIRO, 15m, Dados.Agora);
        c.Status.Should().Be(StatusComanda.ABERTA);
        c.Saldo.Should().Be(25m);

        c.RegistrarPagamento(FormaPagamento.CARTAO_DEBITO, 25m, Dados.Agora);
        c.Status.Should().Be(StatusComanda.PAGA);
    }

    [Fact] // CT-FIN-05
    public void Pagamento_maior_que_o_saldo()
    {
        var c = ComandaDeCorte();

        DeveFalharCom(() => c.RegistrarPagamento(FormaPagamento.PIX, 40.01m, Dados.Agora), "FIN_VALOR_EXCEDE_SALDO");
    }

    [Fact]
    public void Comanda_paga_nao_aceita_novos_pagamentos()
    {
        var c = ComandaDeCorte();
        c.RegistrarPagamento(FormaPagamento.PIX, 40m, Dados.Agora);

        DeveFalharCom(() => c.RegistrarPagamento(FormaPagamento.PIX, 1m, Dados.Agora), "FIN_COMANDA_FECHADA");
    }

    [Fact] // CT-FIN-07
    public void Desconto_sem_motivo()
    {
        var c = ComandaDeCorte();

        DeveFalharCom(() => c.AplicarDesconto(TipoDesconto.VALOR, 5m, "  "), "FIN_DESCONTO_SEM_MOTIVO");
    }

    [Fact]
    public void Desconto_percentual_reduz_o_total()
    {
        var c = ComandaDeCorte();

        c.AplicarDesconto(TipoDesconto.PERCENTUAL, 10m, "Cliente fiel");

        c.Total.Should().Be(36m);
    }

    [Fact]
    public void Desconto_nao_pode_exceder_o_subtotal()
    {
        var c = ComandaDeCorte();

        DeveFalharCom(() => c.AplicarDesconto(TipoDesconto.VALOR, 41m, "Erro de digitação"), "VAL_INVALIDO");
    }

    [Fact] // CT-FIN-09
    public void Comissao_percentual_sobre_o_valor_do_servico()
    {
        var c = ComandaDeCorte();

        c.CalcularComissoes().Should().ContainSingle().Which.Valor.Should().Be(16.00m);
    }

    [Fact] // CT-FIN-12 (regra a confirmar com o estabelecimento)
    public void Comissao_e_calculada_sobre_o_valor_efetivamente_cobrado()
    {
        var c = ComandaDeCorte();
        c.AplicarDesconto(TipoDesconto.PERCENTUAL, 10m, "Cliente fiel"); // total 36,00

        c.CalcularComissoes().Should().ContainSingle().Which.Valor.Should().Be(14.40m);
    }

    [Theory] // CT-FIN-15: arredondamento explícito de centavos
    [InlineData(33.33, 10, 3.33)]
    [InlineData(10.05, 50, 5.03)] // 5,025 -> arredonda para cima (AwayFromZero)
    [InlineData(0, 40, 0)]
    public void Comissao_arredonda_centavos(double valorBase, double percentual, double esperado) =>
        Comissao.Calcular((decimal)valorBase, (decimal)percentual).Should().Be((decimal)esperado);

    [Fact] // CT-FIN-13
    public void Estorno_sem_motivo()
    {
        var c = ComandaDeCorte();
        c.RegistrarPagamento(FormaPagamento.PIX, 40m, Dados.Agora);

        DeveFalharCom(() => c.EstornarPagamento(0, ""), "FIN_DESCONTO_SEM_MOTIVO");
    }

    [Fact] // CT-FIN-14
    public void Estorno_reabre_a_comanda()
    {
        var c = ComandaDeCorte();
        c.RegistrarPagamento(FormaPagamento.PIX, 40m, Dados.Agora);

        c.EstornarPagamento(0, "Cobrança em duplicidade"); // Id 0 porque ainda não passou pelo banco

        c.Status.Should().Be(StatusComanda.ABERTA);
        c.Saldo.Should().Be(40m);
        c.DomainEvents.Should().Contain(e => e is PagamentoEstornado);
    }
}
