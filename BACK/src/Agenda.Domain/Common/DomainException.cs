namespace Agenda.Domain.Common;

/// <summary>O valor numérico é o status HTTP que a API devolve para cada tipo de erro.</summary>
public enum TipoErro
{
    Validacao = 400,
    NaoAutenticado = 401,
    Proibido = 403,
    NaoEncontrado = 404,
    Conflito = 409,
    RegraDeNegocio = 422,
}

public class DomainException : Exception
{
    public string Codigo { get; }
    public TipoErro Tipo { get; }

    public DomainException(string codigo, string mensagem, TipoErro tipo = TipoErro.RegraDeNegocio)
        : base(mensagem)
    {
        Codigo = codigo;
        Tipo = tipo;
    }

    public static DomainException NaoEncontrado(string recurso) =>
        new("RECURSO_NAO_ENCONTRADO", $"{recurso} não encontrado(a).", TipoErro.NaoEncontrado);

    public static DomainException Proibido(string mensagem = "Você não tem permissão para esta operação.") =>
        new("AUTH_PERMISSAO", mensagem, TipoErro.Proibido);

    public static DomainException Invalido(string mensagem) =>
        new("VAL_INVALIDO", mensagem, TipoErro.Validacao);
}
