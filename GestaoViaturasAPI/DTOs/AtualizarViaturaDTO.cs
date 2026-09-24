namespace GestaoViaturasAPI.DTOs;
public class AtualizarViaturaDTO
{
    public string Placa { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public int AnoFabricacao { get; set; }
    public int QuilometragemAtual { get; set; }
    public string Estado { get; set; } = string.Empty;
}
