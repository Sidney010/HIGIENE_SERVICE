using Agenda.Domain.Common;

namespace Agenda.Domain.Cadastro;

/// <summary>Habilitação do profissional para um serviço, com preço/duração/comissão próprios opcionais.</summary>
public sealed class ProfissionalServico : Entity
{
    private ProfissionalServico() { }

    public ProfissionalServico(long servicoId, decimal? precoPersonalizado = null,
        int? duracaoPersonalizadaMin = null, decimal? comissao = null)
    {
        ServicoId = servicoId;
        PrecoPersonalizado = precoPersonalizado is null ? null : Guard.NaoNegativo(precoPersonalizado.Value, "Preço");
        DuracaoPersonalizadaMin = duracaoPersonalizadaMin is null ? null : Guard.Positivo(duracaoPersonalizadaMin.Value, "Duração");
        Comissao = comissao;
    }

    public long ServicoId { get; private set; }
    public decimal? PrecoPersonalizado { get; private set; }
    public int? DuracaoPersonalizadaMin { get; private set; }
    public decimal? Comissao { get; private set; }
}

/// <summary>Faixa de atendimento semanal no horário local da unidade.</summary>
public sealed class Disponibilidade : Entity
{
    private Disponibilidade() { }

    public Disponibilidade(DayOfWeek diaSemana, TimeOnly inicio, TimeOnly fim)
    {
        if (fim <= inicio) throw DomainException.Invalido("O fim da faixa deve ser posterior ao início.");
        DiaSemana = diaSemana;
        Inicio = inicio;
        Fim = fim;
    }

    public DayOfWeek DiaSemana { get; private set; }
    public TimeOnly Inicio { get; private set; }
    public TimeOnly Fim { get; private set; }
}

/// <summary>Folga, férias, almoço, atestado. Guardado em UTC.</summary>
public sealed class Bloqueio : Entity
{
    private Bloqueio() { }

    public Bloqueio(DateTime inicioUtc, DateTime fimUtc, string? motivo)
    {
        Inicio = DataHora.GarantirUtc(inicioUtc);
        Fim = DataHora.GarantirUtc(fimUtc);
        if (Fim <= Inicio) throw DomainException.Invalido("O fim do bloqueio deve ser posterior ao início.");
        Motivo = motivo?.Trim();
    }

    public DateTime Inicio { get; private set; }
    public DateTime Fim { get; private set; }
    public string? Motivo { get; private set; }

    public Intervalo Intervalo => new(Inicio, Fim);
}

public sealed class Profissional : TenantEntity
{
    private readonly List<ProfissionalServico> _servicosHabilitados = new();
    private readonly List<Disponibilidade> _disponibilidades = new();
    private readonly List<Bloqueio> _bloqueios = new();

    private Profissional() { }

    public long UnidadeId { get; private set; }
    public long? UsuarioId { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public decimal ComissaoPadrao { get; private set; }
    public bool Ativo { get; private set; } = true;

    public IReadOnlyCollection<ProfissionalServico> ServicosHabilitados => _servicosHabilitados;
    public IReadOnlyCollection<Disponibilidade> Disponibilidades => _disponibilidades;
    public IReadOnlyCollection<Bloqueio> Bloqueios => _bloqueios;

    public static Profissional Criar(long estabelecimentoId, long unidadeId, string nome, decimal comissaoPadrao, long? usuarioId = null)
    {
        if (comissaoPadrao is < 0 or > 100) throw DomainException.Invalido("A comissão deve estar entre 0 e 100.");
        return new Profissional
        {
            EstabelecimentoId = estabelecimentoId,
            UnidadeId = unidadeId,
            UsuarioId = usuarioId,
            Nome = Guard.NaoVazio(nome, "Nome", 120),
            ComissaoPadrao = comissaoPadrao,
        };
    }

    public void Inativar() => Ativo = false;

    // ---- Serviços
    public void DefinirServicos(IEnumerable<ProfissionalServico> servicos)
    {
        _servicosHabilitados.Clear();
        _servicosHabilitados.AddRange(servicos.GroupBy(s => s.ServicoId).Select(g => g.Last()));
    }

    private ProfissionalServico? Habilitacao(long servicoId) =>
        _servicosHabilitados.FirstOrDefault(s => s.ServicoId == servicoId);

    public bool Atende(long servicoId) => Ativo && Habilitacao(servicoId) is not null;
    public decimal PrecoEfetivo(Servico s) => Habilitacao(s.Id)?.PrecoPersonalizado ?? s.Preco;
    public int DuracaoEfetiva(Servico s) => Habilitacao(s.Id)?.DuracaoPersonalizadaMin ?? s.DuracaoMin;
    public decimal ComissaoEfetiva(long servicoId) => Habilitacao(servicoId)?.Comissao ?? ComissaoPadrao;

    // ---- Agenda
    public void DefinirDisponibilidade(IEnumerable<Disponibilidade> faixas)
    {
        var lista = faixas.ToList();
        foreach (var dia in lista.GroupBy(f => f.DiaSemana))
        {
            var ordenadas = dia.OrderBy(f => f.Inicio).ToList();
            for (var i = 1; i < ordenadas.Count; i++)
                if (ordenadas[i].Inicio < ordenadas[i - 1].Fim)
                    throw new DomainException("AGE_FORA_DISPONIBILIDADE", $"Faixas sobrepostas em {dia.Key}.");
        }
        _disponibilidades.Clear();
        _disponibilidades.AddRange(lista);
    }

    public void AdicionarBloqueio(Bloqueio bloqueio) => _bloqueios.Add(bloqueio);

    public bool TemBloqueio(Intervalo utc) => _bloqueios.Any(b => b.Intervalo.Sobrepoe(utc));

    /// <summary>O intervalo cabe no expediente da unidade e em alguma faixa do profissional (no horário local)?</summary>
    public bool EstaDisponivel(Unidade unidade, Intervalo utc)
    {
        var inicioLocal = unidade.ParaLocal(utc.Inicio);
        var fimLocal = unidade.ParaLocal(utc.Fim);
        if (inicioLocal.Date != fimLocal.Date) return false; // não atravessa a meia-noite

        var inicio = TimeOnly.FromDateTime(inicioLocal);
        var fim = TimeOnly.FromDateTime(fimLocal);
        if (inicio < unidade.AbreAs || fim > unidade.FechaAs) return false;

        return _disponibilidades.Any(d => d.DiaSemana == inicioLocal.DayOfWeek && d.Inicio <= inicio && d.Fim >= fim);
    }
}
