using Agenda.Domain.Common;

namespace Agenda.Domain.Financeiro;

public enum StatusComanda { ABERTA, PAGA, CANCELADA }
public enum StatusPagamento { CONFIRMADO, ESTORNADO }
public enum FormaPagamento { PIX, CARTAO_CREDITO, CARTAO_DEBITO, DINHEIRO }
public enum TipoDesconto { VALOR, PERCENTUAL }

public sealed class ItemComanda : Entity
{
    private ItemComanda() { }

    public ItemComanda(long servicoId, string nome, decimal preco, long profissionalId, decimal comissaoPercentual)
    {
        ServicoId = servicoId;
        Nome = Guard.NaoVazio(nome, "Nome", 120);
        Preco = Guard.NaoNegativo(preco, "Preço");
        ProfissionalId = profissionalId;
        ComissaoPercentual = comissaoPercentual;
    }

    public long ServicoId { get; private set; }
    public string Nome { get; private set; } = string.Empty;
    public decimal Preco { get; private set; }
    public long ProfissionalId { get; private set; }
    public decimal ComissaoPercentual { get; private set; }
}

public sealed class DescontoAplicado : Entity
{
    private DescontoAplicado() { }

    public DescontoAplicado(TipoDesconto tipo, decimal valor, decimal valorCalculado, string motivo)
    {
        Tipo = tipo;
        Valor = valor;
        ValorCalculado = valorCalculado;
        Motivo = motivo;
    }

    public TipoDesconto Tipo { get; private set; }
    public decimal Valor { get; private set; }
    public decimal ValorCalculado { get; private set; }
    public string Motivo { get; private set; } = string.Empty;
}

public sealed class Pagamento : Entity
{
    private Pagamento() { }

    public Pagamento(FormaPagamento forma, decimal valor, DateTime pagoEmUtc)
    {
        Forma = forma;
        Valor = valor;
        PagoEmUtc = DataHora.GarantirUtc(pagoEmUtc);
        Status = StatusPagamento.CONFIRMADO;
    }

    public FormaPagamento Forma { get; private set; }
    public decimal Valor { get; private set; }
    public StatusPagamento Status { get; private set; }
    public DateTime PagoEmUtc { get; private set; }
    public string? MotivoEstorno { get; private set; }

    internal void Estornar(string motivo)
    {
        if (Status == StatusPagamento.ESTORNADO)
            throw new DomainException("FIN_PAGAMENTO_JA_ESTORNADO", "O pagamento já foi estornado.", TipoErro.Conflito);
        Status = StatusPagamento.ESTORNADO;
        MotivoEstorno = motivo;
    }
}

public sealed record PagamentoConfirmado(Comanda Comanda, Pagamento Pagamento) : DomainEvent;
public sealed record PagamentoEstornado(Comanda Comanda, Pagamento Pagamento) : DomainEvent;

public sealed record ComissaoCalculada(long ProfissionalId, long ServicoId, decimal Valor);

public sealed class Comanda : AggregateRoot
{
    private readonly List<ItemComanda> _itens = new();
    private readonly List<DescontoAplicado> _descontos = new();
    private readonly List<Pagamento> _pagamentos = new();

    private Comanda() { }

    public long? AgendamentoId { get; private set; }
    public long ClienteId { get; private set; }
    public StatusComanda Status { get; private set; } = StatusComanda.ABERTA;
    public DateTime CriadaEmUtc { get; private set; }

    public IReadOnlyCollection<ItemComanda> Itens => _itens;
    public IReadOnlyCollection<DescontoAplicado> Descontos => _descontos;
    public IReadOnlyCollection<Pagamento> Pagamentos => _pagamentos;

    public decimal Subtotal => _itens.Sum(i => i.Preco);
    public decimal TotalDescontos => _descontos.Sum(d => d.ValorCalculado);
    public decimal Total => Math.Max(0m, Subtotal - TotalDescontos);
    public decimal TotalPago => _pagamentos.Where(p => p.Status == StatusPagamento.CONFIRMADO).Sum(p => p.Valor);
    public decimal Saldo => Total - TotalPago;

    public static Comanda Abrir(long estabelecimentoId, long clienteId, long? agendamentoId,
        IEnumerable<ItemComanda> itens, DateTime agoraUtc)
    {
        var lista = itens.ToList();
        if (lista.Count == 0) throw DomainException.Invalido("A comanda precisa de ao menos um item.");
        var comanda = new Comanda
        {
            EstabelecimentoId = estabelecimentoId,
            ClienteId = clienteId,
            AgendamentoId = agendamentoId,
            CriadaEmUtc = DataHora.GarantirUtc(agoraUtc),
        };
        comanda._itens.AddRange(lista);
        return comanda;
    }

    private void ExigirAberta()
    {
        if (Status != StatusComanda.ABERTA)
            throw new DomainException("FIN_COMANDA_FECHADA", "A comanda não está aberta.", TipoErro.Conflito);
    }

    public void AplicarDesconto(TipoDesconto tipo, decimal valor, string? motivo)
    {
        ExigirAberta();
        if (string.IsNullOrWhiteSpace(motivo))
            throw new DomainException("FIN_DESCONTO_SEM_MOTIVO", "Informe o motivo do desconto.");
        Guard.NaoNegativo(valor, "Valor do desconto");

        var calculado = tipo == TipoDesconto.PERCENTUAL
            ? Math.Round(Subtotal * valor / 100m, 2, MidpointRounding.AwayFromZero)
            : valor;
        if (TotalDescontos + calculado > Subtotal)
            throw DomainException.Invalido("O desconto não pode exceder o subtotal da comanda.");

        _descontos.Add(new DescontoAplicado(tipo, valor, calculado, motivo.Trim()));
    }

    public Pagamento RegistrarPagamento(FormaPagamento forma, decimal valor, DateTime agoraUtc)
    {
        ExigirAberta();
        if (valor <= 0) throw DomainException.Invalido("O valor do pagamento deve ser maior que zero.");
        if (valor > Saldo)
            throw new DomainException("FIN_VALOR_EXCEDE_SALDO", $"O valor excede o saldo da comanda ({Saldo:0.00}).");

        var pagamento = new Pagamento(forma, valor, agoraUtc);
        _pagamentos.Add(pagamento);

        if (Saldo == 0m)
        {
            Status = StatusComanda.PAGA;
            Raise(new PagamentoConfirmado(this, pagamento));
        }
        return pagamento;
    }

    public void EstornarPagamento(long pagamentoId, string? motivo)
    {
        if (string.IsNullOrWhiteSpace(motivo))
            throw new DomainException("FIN_DESCONTO_SEM_MOTIVO", "Informe o motivo do estorno.");
        var pagamento = _pagamentos.FirstOrDefault(p => p.Id == pagamentoId)
            ?? throw DomainException.NaoEncontrado("Pagamento");

        pagamento.Estornar(motivo.Trim());
        if (Status == StatusComanda.PAGA) Status = StatusComanda.ABERTA;
        Raise(new PagamentoEstornado(this, pagamento));
    }

    /// <summary>
    /// Comissão sobre o valor efetivamente cobrado: o desconto é rateado proporcionalmente entre os itens.
    /// (Regra a confirmar com o estabelecimento — ver plano de testes CT-FIN-12.)
    /// </summary>
    public IReadOnlyList<ComissaoCalculada> CalcularComissoes()
    {
        var fator = Subtotal == 0m ? 0m : Total / Subtotal;
        return _itens
            .Select(i => new ComissaoCalculada(i.ProfissionalId, i.ServicoId, Comissao.Calcular(i.Preco * fator, i.ComissaoPercentual)))
            .ToList();
    }
}
