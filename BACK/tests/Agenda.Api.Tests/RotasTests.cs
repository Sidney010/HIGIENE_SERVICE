using System.Net;
using System.Text.Json;
using Agenda.Domain.Seguranca;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace Agenda.Api.Tests;

public class RotasTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    [Fact]
    public async Task Health_responde_200_sem_autenticacao()
    {
        var resposta = await factory.CreateClient().GetAsync("/api/v1/health");

        resposta.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("status").GetString().Should().Be("Healthy");
    }

    [Fact] // CT-SEG-04
    public async Task Rota_protegida_sem_token_retorna_401()
    {
        var resposta = await factory.CreateClient().GetAsync("/api/v1/clientes");

        resposta.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact] // CT-SEG-08
    public async Task Perfil_sem_permissao_retorna_403()
    {
        var resposta = await factory.ClienteComo(Perfil.Cliente).GetAsync("/api/v1/clientes");

        resposta.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Rota_do_contrato_ainda_nao_implementada_retorna_501()
    {
        var resposta = await factory.ClienteComo(Perfil.Admin).GetAsync("/api/v1/clientes");

        resposta.StatusCode.Should().Be(HttpStatusCode.NotImplemented);
    }

    [Fact]
    public async Task Id_nao_numerico_na_rota_nao_casa_com_nenhuma_rota()
    {
        var resposta = await factory.ClienteComo(Perfil.Admin).GetAsync("/api/v1/clientes/abc");

        resposta.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Cancelamento_com_corpo_invalido_retorna_400_no_formato_padrao()
    {
        var client = factory.ClienteComo(Perfil.Admin);
        var corpo = new StringContent("{ json quebrado", System.Text.Encoding.UTF8, "application/json");

        var resposta = await client.PostAsync("/api/v1/agendamentos/1/cancelar", corpo);

        resposta.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        resposta.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        var json = JsonDocument.Parse(await resposta.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("codigo").GetString().Should().Be("VAL_INVALIDO");
    }

    /// <summary>
    /// Trava o contrato: o MVP tem 69 rotas (docs/openapi.yaml). Se alguém adicionar ou remover uma rota
    /// sem atualizar o contrato (tools/gen_contract.py), este teste falha.
    /// </summary>
    [Fact]
    public void A_api_expoe_exatamente_as_69_rotas_do_contrato()
    {
        _ = factory.CreateClient(); // garante que o host subiu
        var endpoints = factory.Services.GetRequiredService<EndpointDataSource>().Endpoints.OfType<RouteEndpoint>();

        var rotasDaApi = endpoints.Count(e => e.RoutePattern.RawText!.TrimStart('/').StartsWith("api/v1/"));

        rotasDaApi.Should().Be(69);
    }
}
