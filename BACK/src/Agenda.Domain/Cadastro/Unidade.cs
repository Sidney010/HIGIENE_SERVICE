using Agenda.Domain.Common;

namespace Agenda.Domain.Cadastro;

public sealed class Unidade : TenantEntity
{
    private Unidade() { }

    public string Nome { get; private set; } = string.Empty;
    public string Endereco { get; private set; } = string.Empty;
    public string Telefone { get; private set; } = string.Empty;
    public TimeOnly AbreAs { get; private set; }
    public TimeOnly FechaAs { get; private set; }
    public string FusoHorario { get; private set; } = "America/Sao_Paulo";
    public bool Ativo { get; private set; } = true;

    public static Unidade Criar(long estabelecimentoId, string nome, string endereco, string telefone,
        TimeOnly abreAs, TimeOnly fechaAs, string fusoHorario)
    {
        if (fechaAs <= abreAs) throw DomainException.Invalido("O horário de fechamento deve ser posterior ao de abertura.");
        try { TimeZoneInfo.FindSystemTimeZoneById(fusoHorario); }
        catch (TimeZoneNotFoundException) { throw DomainException.Invalido("Fuso horário inválido."); }

        return new Unidade
        {
            EstabelecimentoId = estabelecimentoId,
            Nome = Guard.NaoVazio(nome, "Nome", 120),
            Endereco = Guard.NaoVazio(endereco, "Endereço", 250),
            Telefone = telefone?.Trim() ?? string.Empty,
            AbreAs = abreAs,
            FechaAs = fechaAs,
            FusoHorario = fusoHorario,
        };
    }

    public void Inativar() => Ativo = false;

    // --- Conversões de fuso: o banco guarda UTC; as regras de expediente são no horário local da unidade.
    private TimeZoneInfo Zona => TimeZoneInfo.FindSystemTimeZoneById(FusoHorario);

    public DateTime ParaLocal(DateTime utc) => TimeZoneInfo.ConvertTimeFromUtc(DataHora.GarantirUtc(utc), Zona);

    /// <remarks>Horários inexistentes na virada de horário de verão devem ser tratados caso a caso.</remarks>
    public DateTime ParaUtc(DateOnly data, TimeOnly hora) =>
        TimeZoneInfo.ConvertTimeToUtc(data.ToDateTime(hora, DateTimeKind.Unspecified), Zona);
}
