using Agenda.Application.Abstractions;
using Agenda.Domain.Abstractions;
using Agenda.Domain.Agendamentos;
using Agenda.Domain.Common;
using FluentValidation;

namespace Agenda.Application.Agendamentos.Criar;

public sealed record CriarAgendamentoCommand(
    long? ClienteId,
    long UnidadeId,
    long ProfissionalId,
    DateTime Inicio,
    IReadOnlyList<long> ServicoIds,
    string? Origem,
    Guid? IdempotencyKey);

public sealed class CriarAgendamentoValidator : AbstractValidator<CriarAgendamentoCommand>
{
    public CriarAgendamentoValidator()
    {
        RuleFor(x => x.UnidadeId).GreaterThan(0);
        RuleFor(x => x.ProfissionalId).GreaterThan(0);
        RuleFor(x => x.Inicio).NotEmpty();
        RuleFor(x => x.ServicoIds).NotEmpty().WithMessage("Informe ao menos um serviço.");
        RuleForEach(x => x.ServicoIds).GreaterThan(0);
        RuleFor(x => x.Origem).MaximumLength(20);
    }
}

public sealed class CriarAgendamentoHandler(
    IValidator<CriarAgendamentoCommand> validator,
    IUnitOfWork uow,
    ITenantProvider tenant,
    ICurrentUser usuario,
    TimeProvider relogio,
    IClienteRepository clientes,
    IUnidadeRepository unidades,
    IProfissionalRepository profissionais,
    IServicoRepository servicos,
    IConfiguracaoRepository configuracoes,
    IAgendamentoRepository agendamentos,
    ValidadorDeAgendamento validadorDeRegras)
    : IHandler<CriarAgendamentoCommand, AgendamentoDto>
{
    public async Task<AgendamentoDto> HandleAsync(CriarAgendamentoCommand cmd, CancellationToken ct)
    {
        await validator.ValidateAndThrowAsync(cmd, ct);
        var clienteId = ResolverClienteId(cmd);

        // TODO(idempotência): consultar/gravar cmd.IdempotencyKey num IIdempotencyStore (hash do corpo + resposta por 24h).
        var (criado, clienteCriado, profissionalCriado) = await uow.ExecutarEmTransacaoAsync(async () =>
        {
            var agora = relogio.GetUtcNow().UtcDateTime;

            var cliente = await clientes.ObterAsync(clienteId, ct) ?? throw DomainException.NaoEncontrado("Cliente");
            var unidade = await unidades.ObterAsync(cmd.UnidadeId, ct) ?? throw DomainException.NaoEncontrado("Unidade");
            var profissional = await profissionais.ObterAsync(cmd.ProfissionalId, ct) ?? throw DomainException.NaoEncontrado("Profissional");

            var idsDistintos = cmd.ServicoIds.Distinct().ToList();
            var servicosEscolhidos = await servicos.ListarPorIdsAsync(idsDistintos, ct);
            if (servicosEscolhidos.Count != idsDistintos.Count) throw DomainException.NaoEncontrado("Serviço");

            var config = await configuracoes.ObterAsync(ct);

            // Preço e duração são resolvidos AGORA e congelados no item (RN-AGE-08).
            var itens = servicosEscolhidos
                .Select(s => new ItemAgendamento(s.Id, s.Nome, profissional.DuracaoEfetiva(s), profissional.PrecoEfetivo(s)))
                .ToList();

            var novo = Agendamento.Criar(tenant.EstabelecimentoId, unidade.Id, cliente.Id, profissional.Id,
                cmd.Inicio, itens, config.IntervaloHigienizacaoMinutos, cmd.Origem, agora);

            var doProfissional = await agendamentos.ListarOcupandoAgendaDoProfissionalAsync(profissional.Id, novo.Inicio, novo.Fim, ct);
            var doCliente = await agendamentos.ListarOcupandoAgendaDoClienteAsync(cliente.Id, novo.Inicio, novo.Fim, ct);

            validadorDeRegras.Validar(new ContextoValidacao(novo, profissional, unidade, config, idsDistintos,
                doProfissional, doCliente, agora));

            await agendamentos.AdicionarAsync(novo, ct);
            return (novo, cliente, profissional);
            // A constraint de exclusão no banco é a garantia final contra corrida entre duas requisições.
        }, ct);

        return AgendamentoDto.De(criado, clienteCriado, profissionalCriado);
    }

    private long ResolverClienteId(CriarAgendamentoCommand cmd)
    {
        if (usuario.Perfil == Perfis.Cliente)
            return usuario.ClienteId ?? throw DomainException.Proibido();
        return cmd.ClienteId ?? throw DomainException.Invalido("Informe o cliente.");
    }
}
