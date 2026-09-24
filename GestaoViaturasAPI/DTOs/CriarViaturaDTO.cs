namespace GestaoViaturasAPI.DTOs;

public class CriarViaturaDTO
{
    public string Placa { get; set; } = string.Empty;
    public string Modelo { get; set; } = string.Empty;
    public int AnoFabricacao { get; set; }
    public int QuilometragemInicial { get; set; }
}
