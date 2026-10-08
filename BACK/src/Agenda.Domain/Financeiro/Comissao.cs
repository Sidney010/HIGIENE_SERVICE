using Agenda.Domain.Common;

namespace Agenda.Domain.Financeiro;

public enum StatusComissao { PENDENTE, PAGA, REVERTIDA }

public sealed class Comissao : TenantEntity
{
    private Comissao() { }

    public long ComandaId { get; private set; }
    public long ProfissionalId { get; private set; }
    public decimal Valor { get; private set; }
    public StatusComissao Status { get; private set; } = StatusComissao.PENDENTE;
    public DateTime GeradaEmUtc { get; private set; }

    /// <summary>Sempre decimal, com arredondamento explícito (nunca double para dinheiro).</summary>
    public static decimal Calcular(decimal valorBase, decimal percentual) =>
        Math.Round(valorBase * percentual / 100m, 2, MidpointRounding.AwayFromZero);

    public static Comissao Gerar(long estabelecimentoId, long comandaId, long profissionalId, decimal valor, DateTime agoraUtc) => new()
    {
        EstabelecimentoId = estabelecimentoId,
        ComandaId = comandaId,
        ProfissionalId = profissionalId,
        Valor = valor,
        GeradaEmUtc = DataHora.GarantirUtc(agoraUtc),
    };

    public void Reverter() => Status = StatusComissao.REVERTIDA;
}
