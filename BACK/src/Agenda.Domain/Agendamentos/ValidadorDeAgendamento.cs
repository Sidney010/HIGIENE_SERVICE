using Agenda.Domain.Cadastro;
using Agenda.Domain.Common;

namespace Agenda.Domain.Agendamentos;

public sealed record ContextoValidacao(
    Agendamento Novo,
    Profissional Profissional,
    Unidade Unidade,
    ConfiguracaoEstabelecimento Config,
    IReadOnlyCollection<long> ServicoIds,
    IReadOnlyCollection<Agendamento> AgendamentosDoProfissional,
    IReadOnlyCollection<Agendamento> AgendamentosDoCliente,
    DateTime AgoraUtc);

/// <summary>Serviço de domínio: regras RN-AGE-01 a RN-AGE-06. Puro, sem acesso a banco.</summary>
public sealed class ValidadorDeAgendamento
{
    public void Validar(ContextoValidacao c)
    {
        // RN-AGE-04
        if (!c.Profissional.Ativo || c.ServicoIds.Any(id => !c.Profissional.Atende(id)))
            throw new DomainException("AGE_PROFISSIONAL_NAO_HABILITADO",
                "O profissional não está habilitado para todos os serviços escolhidos.");

        // RN-AGE-05
        if (!c.Config.AntecedenciaOk(c.Novo.Inicio, c.AgoraUtc))
            throw new DomainException("AGE_ANTECEDENCIA_INVALIDA",
                "O horário está fora da antecedência mínima ou máxima permitida.");

        // RN-AGE-02
        if (!c.Profissional.EstaDisponivel(c.Unidade, c.Novo.Intervalo))
            throw new DomainException("AGE_FORA_DISPONIBILIDADE",
                "O horário está fora do expediente do profissional ou da unidade.");

        // Bloqueios (folga, férias, almoço)
        if (c.Profissional.TemBloqueio(c.Novo.Intervalo))
            throw new DomainException("AGE_BLOQUEIO", "O profissional possui um bloqueio neste horário.", TipoErro.Conflito);

        // RN-AGE-01
        if (c.AgendamentosDoProfissional.Any(a => c.Novo.ConflitaComProfissional(a)))
            throw new DomainException("AGE_HORARIO_OCUPADO",
                "O profissional já possui atendimento neste horário.", TipoErro.Conflito);

        // RN-AGE-06
        if (c.AgendamentosDoCliente.Any(a => c.Novo.ConflitaComCliente(a)))
            throw new DomainException("AGE_CLIENTE_CONFLITO",
                "O cliente já possui um agendamento neste horário.", TipoErro.Conflito);
    }
}
