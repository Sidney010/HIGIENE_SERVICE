using Agenda.Domain.Common;

namespace Agenda.Domain.Agendamentos;

/// <summary>Serviço dentro de um agendamento. O preço é CONGELADO no momento da criação.</summary>
public sealed class ItemAgendamento : Entity
{
    private ItemAgendamento() { }

    public ItemAgendamento(long servicoId, string nome, int duracaoMin, decimal precoCongelado)
    {
        ServicoId = servicoId;
        Nome = Guard.NaoVazio(nome, "Nome do serviço", 120);
        DuracaoMin = Guard.Positivo(duracaoMin, "Duração");
        PrecoCongelado = Guard.NaoNegativo(precoCongelado, "Preço");
    }

    public long ServicoId { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public int DuracaoMin { get; private set; }
    public decimal PrecoCongelado { get; private set; }
}
