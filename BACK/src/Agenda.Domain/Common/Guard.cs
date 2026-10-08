namespace Agenda.Domain.Common;

public static class Guard
{
    public static string NaoVazio(string? valor, string campo, int max = 200)
    {
        if (string.IsNullOrWhiteSpace(valor))
            throw DomainException.Invalido($"{campo} é obrigatório.");
        var limpo = valor.Trim();
        if (limpo.Length > max)
            throw DomainException.Invalido($"{campo} excede {max} caracteres.");
        return limpo;
    }

    public static decimal NaoNegativo(decimal valor, string campo)
    {
        if (valor < 0) throw DomainException.Invalido($"{campo} não pode ser negativo.");
        return valor;
    }

    public static int Positivo(int valor, string campo)
    {
        if (valor <= 0) throw DomainException.Invalido($"{campo} deve ser maior que zero.");
        return valor;
    }
}

public static class DataHora
{
    /// <summary>Normaliza para UTC. Datas sem Kind são tratadas como UTC (a API sempre recebe UTC).</summary>
    public static DateTime GarantirUtc(DateTime dt) => dt.Kind switch
    {
        DateTimeKind.Utc => dt,
        DateTimeKind.Local => dt.ToUniversalTime(),
        _ => DateTime.SpecifyKind(dt, DateTimeKind.Utc),
    };
}

/// <summary>Intervalo semiaberto [Inicio, Fim): atendimentos encostados não conflitam.</summary>
public readonly record struct Intervalo(DateTime Inicio, DateTime Fim)
{
    public bool Sobrepoe(Intervalo outro) => Inicio < outro.Fim && outro.Inicio < Fim;
}
