using Agenda.Domain.Common;

namespace Agenda.Domain.Cadastro;

public sealed class Cliente : TenantEntity
{
    private Cliente() { }

    public string Nome { get; private set; } = string.Empty;
    public string Telefone { get; private set; } = string.Empty;
    public string? Email { get; private set; }
    public DateOnly? DataNascimento { get; private set; }
    /// <summary>Dado sensível (alergias, pele, unha): exposição restrita a Admin e Recepção.</summary>
    public string? Observacoes { get; private set; }
    public bool ConsentimentoLgpd { get; private set; }
    public DateTime? ConsentimentoEmUtc { get; private set; }
    public int TotalNoShows { get; private set; }
    public bool Ativo { get; private set; } = true;

    public static Cliente Criar(long estabelecimentoId, string nome, string telefone, string? email,
        DateOnly? dataNascimento, string? observacoes, bool consentimentoLgpd, DateTime agoraUtc)
    {
        if (!consentimentoLgpd)
            throw new DomainException("CAD_LGPD_OBRIGATORIO", "O consentimento LGPD é obrigatório para o cadastro.");

        return new Cliente
        {
            EstabelecimentoId = estabelecimentoId,
            Nome = Guard.NaoVazio(nome, "Nome", 120),
            Telefone = NormalizarTelefone(telefone),
            Email = string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant(),
            DataNascimento = dataNascimento,
            Observacoes = observacoes?.Trim(),
            ConsentimentoLgpd = true,
            ConsentimentoEmUtc = DataHora.GarantirUtc(agoraUtc),
        };
    }

    public static string NormalizarTelefone(string? telefone)
    {
        var digitos = new string((telefone ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digitos.Length is < 10 or > 13)
            throw DomainException.Invalido("Telefone inválido. Informe DDD e número.");
        return digitos;
    }

    public void RegistrarNoShow() => TotalNoShows++;

    public void Inativar() => Ativo = false;
}
