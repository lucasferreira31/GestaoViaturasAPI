using System.Text.Json.Serialization;
using System.Text.RegularExpressions;

namespace GestaoViaturasAPI;

public class RegraDeNegocioException(string message) : Exception(message);

public class Viatura
{
    public const string Disponivel = "Disponível";
    public const string EmPatrulha = "Em Patrulha";
    public const string Manutencao = "Manutenção";
    private Viatura() { } // EF Core
    public int Id { get; private set; }
    public string Matricula { get; private set; } = string.Empty;
    public string Modelo { get; private set; } = string.Empty;
    public int AnoFabricacao { get; private set; }
    public int QuilometragemInicial { get; private set; }
    public int QuilometragemAtual { get; private set; }
    public string Estado { get; private set; } = Disponivel;
    public DateTime DataRegisto { get; private set; } = DateTime.UtcNow;
    [JsonIgnore]
    public Guid Versao { get; private set; } = Guid.NewGuid();

    public static Viatura Criar(string placa, string modelo, int ano, int quilometragem)
    {
        ValidarDados(placa, modelo, ano, quilometragem);
        return new Viatura
        {
            Matricula = placa.Trim().ToUpperInvariant(), Modelo = modelo.Trim(), AnoFabricacao = ano,
            QuilometragemInicial = quilometragem, QuilometragemAtual = quilometragem
        };
    }

    public void Atualizar(string placa, string modelo, int ano, int quilometragem, string estado)
    {
        ValidarDados(placa, modelo, ano, quilometragem);
        if (quilometragem < QuilometragemAtual)
            throw new RegraDeNegocioException("A quilometragem não pode ser menor que a já registrada.");
        if (estado is not (Disponivel or EmPatrulha or Manutencao))
            throw new RegraDeNegocioException("Estado da viatura inválido.");
        if (Estado == Manutencao && estado == EmPatrulha)
            throw new RegraDeNegocioException("Conclua a manutenção antes de liberar a viatura para patrulha.");
        Matricula = placa.Trim().ToUpperInvariant();
        Modelo = modelo.Trim();
        AnoFabricacao = ano;
        QuilometragemAtual = quilometragem;
        Estado = estado;
        Versao = Guid.NewGuid();
    }

    private static void ValidarDados(string placa, string modelo, int ano, int km)
    {
        if (!PlacaValida(placa))
            throw new RegraDeNegocioException("Informe uma placa válida, sem hífen (ABC1234 ou ABC1D23).");
        if (string.IsNullOrWhiteSpace(modelo) || modelo.Trim().Length > 50)
            throw new RegraDeNegocioException("O modelo é obrigatório e deve ter até 50 caracteres.");
        if (ano <= 2000 || ano > DateTime.UtcNow.Year + 1)
            throw new RegraDeNegocioException("Ano de fabricação inválido.");
        if (km < 0) throw new RegraDeNegocioException("A quilometragem não pode ser negativa.");
    }

    public static bool PlacaValida(string? placa) => !string.IsNullOrWhiteSpace(placa)
        && Regex.IsMatch(placa.Trim().ToUpperInvariant(), @"\A[A-Z]{3}[0-9][A-Z0-9][0-9]{2}\z");
}
