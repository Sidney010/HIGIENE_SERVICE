namespace Agenda.Api.Contracts;

// Contratos de entrada da API (espelham docs/openapi.yaml). Ficam separados dos comandos da Application
// para o contrato HTTP poder evoluir sem quebrar os casos de uso (e vice-versa).

public sealed record CriarAgendamentoRequest(
    long? ClienteId,
    long UnidadeId,
    long ProfissionalId,
    DateTime Inicio,
    List<long> ServicoIds,
    string? Origem);

public sealed record CancelarRequest(string? Motivo);
