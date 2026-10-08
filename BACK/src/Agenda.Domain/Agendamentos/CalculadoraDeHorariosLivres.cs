using Agenda.Domain.Cadastro;
using Agenda.Domain.Common;

namespace Agenda.Domain.Agendamentos;

public sealed record AgendaDoProfissional(Profissional Profissional, IReadOnlyCollection<Agendamento> Agendamentos);

public sealed record HorarioLivre(long ProfissionalId, DateTime InicioUtc, DateTime FimUtc);

/// <summary>Serviço de domínio que gera as janelas livres de um dia, respeitando todas as regras de agendamento.</summary>
public sealed class CalculadoraDeHorariosLivres
{
    public IReadOnlyList<HorarioLivre> Calcular(
        Unidade unidade,
        ConfiguracaoEstabelecimento config,
        IReadOnlyCollection<Servico> servicos,
        IEnumerable<AgendaDoProfissional> agendas,
        DateOnly dataLocal,
        DateTime agoraUtc,
        int passoMinutos = 15)
    {
        var resultado = new List<HorarioLivre>();

        foreach (var agenda in agendas)
        {
            var profissional = agenda.Profissional;
            var existentes = agenda.Agendamentos;
            var duracaoServicos = servicos.Sum(s => profissional.DuracaoEfetiva(s));
            var duracaoComIntervalo = duracaoServicos + config.IntervaloHigienizacaoMinutos;

            var cursor = unidade.AbreAs;
            while (true)
            {
                var inicioUtc = unidade.ParaUtc(dataLocal, cursor);
                var fimComIntervalo = inicioUtc.AddMinutes(duracaoComIntervalo);

                var fimLocal = unidade.ParaLocal(fimComIntervalo);
                if (DateOnly.FromDateTime(fimLocal) != dataLocal || TimeOnly.FromDateTime(fimLocal) > unidade.FechaAs)
                    break; // os próximos candidatos também não cabem no expediente

                var janela = new Intervalo(inicioUtc, fimComIntervalo);
                var livre =
                    config.AntecedenciaOk(inicioUtc, agoraUtc)
                    && profissional.EstaDisponivel(unidade, janela)
                    && !profissional.TemBloqueio(janela)
                    && !existentes.Any(a => a.OcupaAgenda && a.Intervalo.Sobrepoe(janela));

                if (livre)
                    resultado.Add(new HorarioLivre(profissional.Id, inicioUtc, inicioUtc.AddMinutes(duracaoServicos)));

                var proximo = cursor.AddMinutes(passoMinutos);
                if (proximo <= cursor) break; // passou da meia-noite
                cursor = proximo;
            }
        }

        return resultado.OrderBy(h => h.InicioUtc).ThenBy(h => h.ProfissionalId).ToList();
    }
}
