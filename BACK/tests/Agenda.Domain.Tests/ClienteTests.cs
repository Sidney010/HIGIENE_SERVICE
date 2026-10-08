using Agenda.Domain.Cadastro;
using Agenda.Domain.Common;

namespace Agenda.Domain.Tests;

public class ClienteTests
{
    [Fact] // CT-CAD-01
    public void Cria_cliente_valido_e_normaliza_o_telefone()
    {
        var c = Cliente.Criar(1, " Carlos ", "(11) 99999-0001", "Carlos@Email.com", null, null, true, Dados.Agora);

        c.Nome.Should().Be("Carlos");
        c.Telefone.Should().Be("11999990001");
        c.Email.Should().Be("carlos@email.com");
        c.ConsentimentoEmUtc.Should().Be(Dados.Agora);
    }

    [Fact] // CT-CAD-02
    public void Sem_consentimento_lgpd_nao_cadastra()
    {
        var act = () => Cliente.Criar(1, "Carlos", "11999990001", null, null, null, false, Dados.Agora);

        var ex = act.Should().Throw<DomainException>().Which;
        ex.Codigo.Should().Be("CAD_LGPD_OBRIGATORIO");
        ex.Tipo.Should().Be(TipoErro.RegraDeNegocio);
    }

    [Theory] // CT-CAD-05
    [InlineData("123")]
    [InlineData("")]
    [InlineData("abcdefghijk")]
    public void Telefone_invalido(string telefone)
    {
        var act = () => Cliente.Criar(1, "Carlos", telefone, null, null, null, true, Dados.Agora);

        act.Should().Throw<DomainException>().Which.Codigo.Should().Be("VAL_INVALIDO");
    }

    [Fact]
    public void Registrar_no_show_incrementa_o_contador()
    {
        var c = Dados.Cliente();

        c.RegistrarNoShow();
        c.RegistrarNoShow();

        c.TotalNoShows.Should().Be(2);
    }

    [Fact] // CT-CAD-06 (a parte de domínio: inativar não apaga)
    public void Inativar_mantem_o_historico()
    {
        var c = Dados.Cliente();

        c.Inativar();

        c.Ativo.Should().BeFalse();
        c.Nome.Should().NotBeEmpty();
    }
}
