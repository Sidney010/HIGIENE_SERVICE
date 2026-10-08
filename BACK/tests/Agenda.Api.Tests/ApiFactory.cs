using System.Net.Http.Headers;
using Agenda.Api.Security;
using Agenda.Domain.Seguranca;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

namespace Agenda.Api.Tests;

/// <summary>Sobe a API em memória. Estes testes não precisam de banco (rotas, autenticação, autorização, contrato).</summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder) => builder.UseEnvironment("Testing");

    public HttpClient ClienteComo(Perfil perfil, long estabelecimentoId = 1)
    {
        var jwt = Services.GetRequiredService<JwtTokenService>();
        var (token, _) = jwt.GerarAccessToken(usuarioId: 1, estabelecimentoId, perfil,
            clienteId: perfil == Perfil.Cliente ? 1 : null,
            profissionalId: perfil == Perfil.Profissional ? 1 : null);

        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }
}
