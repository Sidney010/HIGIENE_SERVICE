using Agenda.Domain.Common;

namespace Agenda.Domain.Cadastro;

/// <summary>Regras configuráveis por estabelecimento (nada de números fixos no código).</summary>
public sealed class ConfiguracaoEstabelecimento : TenantEntity
{
    private ConfiguracaoEstabelecimento() { }

    public int AntecedenciaMinimaMinutos { get; private set; } = 60;
    public int AntecedenciaMaximaDias { get; private set; } = 60;
    public int PrazoCancelamentoHoras { get; private set; } = 2;
    public int IntervaloHigienizacaoMinutos { get; private set; }
    public decimal MultaCancelamentoTardioPercentual { get; private set; }
    public decimal LimiteDescontoRecepcaoPercentual { get; private set; } = 10;

    public static ConfiguracaoEstabelecimento Padrao(long estabelecimentoId) => new() { EstabelecimentoId = estabelecimentoId };

    public static ConfiguracaoEstabelecimento Criar(long estabelecimentoId, int antecedenciaMinMin, int antecedenciaMaxDias,
        int prazoCancelamentoHoras, int intervaloHigienizacaoMin, decimal multaPercentual, decimal limiteDescontoRecepcao) => new()
    {
        EstabelecimentoId = estabelecimentoId,
        AntecedenciaMinimaMinutos = antecedenciaMinMin,
        AntecedenciaMaximaDias = antecedenciaMaxDias,
        PrazoCancelamentoHoras = prazoCancelamentoHoras,
        IntervaloHigienizacaoMinutos = intervaloHigienizacaoMin,
        MultaCancelamentoTardioPercentual = multaPercentual,
        LimiteDescontoRecepcaoPercentual = limiteDescontoRecepcao,
    };

    public bool AntecedenciaOk(DateTime inicioUtc, DateTime agoraUtc)
    {
        var minutos = (inicioUtc - agoraUtc).TotalMinutes;
        return minutos >= AntecedenciaMinimaMinutos && inicioUtc <= agoraUtc.AddDays(AntecedenciaMaximaDias);
    }
}
