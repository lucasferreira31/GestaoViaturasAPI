using System.ComponentModel.DataAnnotations;
namespace GestaoViaturasAPI;
public class LoginDTO
{
    [Required, StringLength(100)]
    public string NomeUtilizador { get; set; } = string.Empty;
    [Required, StringLength(256)]
    public string PalavraPasse { get; set; } = string.Empty;
}
