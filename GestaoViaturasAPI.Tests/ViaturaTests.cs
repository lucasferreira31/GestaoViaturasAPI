using GestaoViaturasAPI;
namespace GestaoViaturasAPI.Tests;

public class ViaturaTests
{
    [Fact]
    public void CadastroNormalizaPlacaEPreservaQuilometragemInicial()
    {
        var v = Viatura.Criar(" abc1d23 ", " Modelo ", 2024, 100);
        Assert.Equal("ABC1D23", v.Matricula);
        Assert.Equal("Modelo", v.Modelo);
        Assert.Equal(100, v.QuilometragemInicial);
        Assert.Equal(100, v.QuilometragemAtual);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(99)]
    public void NaoPermiteRetrocederQuilometragem(int km)
    {
        var v = Viatura.Criar("ABC1234", "Modelo", 2024, 100);
        Assert.Throws<RegraDeNegocioException>(() => v.Atualizar("ABC1234", "Modelo", 2024, km, Viatura.Disponivel));
        Assert.Equal(100, v.QuilometragemAtual);
    }

    [Fact]
    public void ManutencaoImpedePatrulhaAteConclusao()
    {
        var v = Viatura.Criar("ABC1234", "Modelo", 2024, 100);
        v.Atualizar("ABC1234", "Modelo", 2024, 100, Viatura.Manutencao);
        Assert.Throws<RegraDeNegocioException>(() => v.Atualizar("ABC1234", "Modelo", 2024, 120, Viatura.EmPatrulha));
        Assert.Equal(100, v.QuilometragemAtual);
        v.Atualizar("ABC1234", "Modelo", 2024, 100, Viatura.Disponivel);
        v.Atualizar("ABC1234", "Modelo", 2024, 120, Viatura.EmPatrulha);
        Assert.Equal(120, v.QuilometragemAtual);
        Assert.Equal(100, v.QuilometragemInicial);
    }

    [Theory]
    [InlineData("1234567")]
    [InlineData("ABC-1234")]
    [InlineData("ABCDEFG")]
    [InlineData("")]
    public void RejeitaPlacasInvalidas(string placa)
        => Assert.Throws<RegraDeNegocioException>(() => Viatura.Criar(placa, "Modelo", 2024, 0));

    [Fact]
    public void RejeitaEstadoDesconhecido()
    {
        var v = Viatura.Criar("ABC1234", "Modelo", 2024, 0);
        Assert.Throws<RegraDeNegocioException>(() => v.Atualizar("ABC1234", "Modelo", 2024, 0, "Outro"));
    }
}
