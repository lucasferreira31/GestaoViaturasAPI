using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
using GestaoViaturasAPI.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace GestaoViaturasAPI.Tests;

public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string TestKey = "integration-tests-only-key-never-use-in-production-123456";
    public const string TestPassword = "Integration-Test-Password-123!";
    private readonly SqliteConnection connection = new("DataSource=:memory:");
    public ApiFactory()
    {
        Environment.SetEnvironmentVariable("Jwt__Key", TestKey);
        Environment.SetEnvironmentVariable("Jwt__Issuer", "GestaoViaturasAPI");
        Environment.SetEnvironmentVariable("Jwt__Audience", "GestaoViaturasAPI.Client");
        connection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureLogging(logging => logging.ClearProviders().AddConsole());
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<AppDbContext>>();
            services.RemoveAll<AppDbContext>();
            services.AddDbContext<AppDbContext>(options => options.UseSqlite(connection));
        });
    }

    public void InitializeDatabase()
    {
        using var scope = Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        db.Database.EnsureCreated();
        var usuario = new Usuario { NomeUtilizador = "TESTE" };
        usuario.PalavraPasseHash = scope.ServiceProvider.GetRequiredService<IPasswordHasher<Usuario>>()
            .HashPassword(usuario, TestPassword);
        db.Usuarios.Add(usuario);
        db.SaveChanges();
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) connection.Dispose();
    }
}

public class ApiTests : IDisposable
{
    private readonly ApiFactory factory = new();
    private readonly HttpClient client;
    public ApiTests()
    {
        client = factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost") });
        factory.InitializeDatabase();
    }
    public void Dispose() { client.Dispose(); factory.Dispose(); }

    private async Task Login()
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { nomeUtilizador = "teste", palavraPasse = ApiFactory.TestPassword });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", body.GetProperty("token").GetString());
    }

    private static object Cadastro(string placa = "ABC1D23") => new
        { placa, modelo = "SUV", anoFabricacao = 2024, quilometragemInicial = 100 };

    private static object Atualizacao(string estado = "Disponível", int km = 100, string placa = "ABC1D23") => new
        { placa, modelo = "SUV atualizado", anoFabricacao = 2024, quilometragemAtual = km, estado };

    private async Task<int> Criar(string placa = "ABC1D23")
    {
        var response = await client.PostAsJsonAsync("/api/viaturas", Cadastro(placa));
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        return body.GetProperty("id").GetInt32();
    }

    [Fact]
    public async Task CrudPersisteELista()
    {
        await Login();
        var id = await Criar();
        var lista = await client.GetFromJsonAsync<JsonElement>("/api/viaturas");
        Assert.Equal(1, lista.GetArrayLength());
        Assert.Equal(100, lista[0].GetProperty("quilometragemInicial").GetInt32());
        Assert.Equal(2024, lista[0].GetProperty("anoFabricacao").GetInt32());
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync($"/api/viaturas/{id}", Atualizacao(km: 150))).StatusCode);
        var atual = await client.GetFromJsonAsync<JsonElement>($"/api/viaturas/{id}");
        Assert.Equal(150, atual.GetProperty("quilometragemAtual").GetInt32());
        Assert.Equal(100, atual.GetProperty("quilometragemInicial").GetInt32());
        Assert.Equal(HttpStatusCode.NoContent, (await client.DeleteAsync($"/api/viaturas/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await client.GetAsync($"/api/viaturas/{id}")).StatusCode);
    }

    [Fact]
    public async Task TodasOperacoesExigemAutenticacao()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/viaturas")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/viaturas/1")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/viaturas", Cadastro())).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PutAsJsonAsync("/api/viaturas/1", Atualizacao())).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.DeleteAsync("/api/viaturas/1")).StatusCode);
    }

    [Fact]
    public async Task LoginInvalidoRetorna401()
    {
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login",
            new { nomeUtilizador = "teste", palavraPasse = "senha-incorreta" })).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.PostAsJsonAsync("/api/auth/login",
            new { nomeUtilizador = "inexistente", palavraPasse = ApiFactory.TestPassword })).StatusCode);
    }

    [Fact]
    public async Task LoginVazioRetorna400()
        => Assert.Equal(HttpStatusCode.BadRequest, (await client.PostAsJsonAsync("/api/auth/login",
            new { nomeUtilizador = "", palavraPasse = "" })).StatusCode);

    [Theory]
    [InlineData("issuer")]
    [InlineData("audience")]
    [InlineData("expired")]
    [InlineData("signature")]
    public async Task TokenInvalidoRetorna401(string defeito)
    {
        var expires = defeito == "expired" ? DateTime.UtcNow.AddMinutes(-5) : DateTime.UtcNow.AddMinutes(5);
        var token = new JwtSecurityToken(
            issuer: defeito == "issuer" ? "incorreto" : "GestaoViaturasAPI",
            audience: defeito == "audience" ? "incorreto" : "GestaoViaturasAPI.Client",
            notBefore: DateTime.UtcNow.AddHours(-1), expires: expires,
            signingCredentials: new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                defeito == "signature" ? new string('x', 64) : ApiFactory.TestKey)), SecurityAlgorithms.HmacSha256));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", new JwtSecurityTokenHandler().WriteToken(token));
        Assert.Equal(HttpStatusCode.Unauthorized, (await client.GetAsync("/api/viaturas")).StatusCode);
    }

    [Fact]
    public async Task CadastroEAtualizacaoInvalidosRetornam400()
    {
        await Login();
        var id = await Criar();
        var response = await client.PostAsJsonAsync("/api/viaturas", Cadastro("1234567"));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True((await response.Content.ReadFromJsonAsync<JsonElement>()).TryGetProperty("errors", out _));
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"/api/viaturas/{id}", Atualizacao(km: -1))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await client.PutAsJsonAsync($"/api/viaturas/{id}", Atualizacao(estado: "Outro"))).StatusCode);
    }

    [Fact]
    public async Task PlacaDuplicadaRetorna409()
    {
        await Login();
        await Criar();
        Assert.Equal(HttpStatusCode.Conflict, (await client.PostAsJsonAsync("/api/viaturas", Cadastro("abc1d23"))).StatusCode);
        var outro = await Criar("XYZ1234");
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/api/viaturas/{outro}", Atualizacao())).StatusCode);
    }

    [Fact]
    public async Task AtualizacaoInexistenteRetorna404()
    {
        await Login();
        Assert.Equal(HttpStatusCode.NotFound, (await client.PutAsJsonAsync("/api/viaturas/999", Atualizacao())).StatusCode);
    }

    [Fact]
    public async Task RegrasDeNegocioRetornam409SemAlterarBanco()
    {
        await Login();
        var id = await Criar();
        Assert.Equal(HttpStatusCode.Conflict, (await client.PutAsJsonAsync($"/api/viaturas/{id}", Atualizacao(km: 99))).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await client.PutAsJsonAsync($"/api/viaturas/{id}", Atualizacao(Viatura.Manutencao))).StatusCode);
        var response = await client.PutAsJsonAsync($"/api/viaturas/{id}", Atualizacao(Viatura.EmPatrulha));
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        var v = await client.GetFromJsonAsync<JsonElement>($"/api/viaturas/{id}");
        Assert.Equal(Viatura.Manutencao, v.GetProperty("estado").GetString());
        Assert.Equal(100, v.GetProperty("quilometragemAtual").GetInt32());
    }

    [Fact]
    public async Task ErroInesperadoNaoExpoeExcecao()
    {
        await Login();
        using (var scope = factory.Services.CreateScope())
            await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.ExecuteSqlRawAsync("DROP TABLE Viaturas");
        var response = await client.GetAsync("/api/viaturas");
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        var body = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain("SqliteException", body);
        Assert.DoesNotContain("no such table", body);
        Assert.Contains("traceId", body);
    }

    [Fact]
    public void AlteracoesSimultaneasNaoSobrescrevemQuilometragem()
    {
        using var scope1 = factory.Services.CreateScope();
        using var scope2 = factory.Services.CreateScope();
        var db1 = scope1.ServiceProvider.GetRequiredService<AppDbContext>();
        var db2 = scope2.ServiceProvider.GetRequiredService<AppDbContext>();
        var v1 = Viatura.Criar("ABC1234", "Modelo", 2024, 100);
        db1.Viaturas.Add(v1);
        db1.SaveChanges();
        var v2 = db2.Viaturas.Single();
        v1.Atualizar("ABC1234", "Modelo", 2024, 200, Viatura.Disponivel);
        db1.SaveChanges();
        v2.Atualizar("ABC1234", "Modelo", 2024, 150, Viatura.Disponivel);
        Assert.Throws<DbUpdateConcurrencyException>(() => db2.SaveChanges());
    }
}
