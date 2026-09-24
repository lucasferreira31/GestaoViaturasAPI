namespace GestaoViaturasAPI
{
    public class Usuario
    {
        public int Id { get; set; }
        public string NomeUtilizador { get; set; } = string.Empty;
        public string PalavraPasseHash { get; set; } = string.Empty;
    }
}
