using Microsoft.EntityFrameworkCore;
namespace GestaoViaturasAPI.Data;
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Viatura> Viaturas => Set<Viatura>();
    public DbSet<Usuario> Usuarios => Set<Usuario>();
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var v = modelBuilder.Entity<Viatura>();
        v.Property(x => x.Matricula).HasMaxLength(7).IsRequired();
        v.HasIndex(x => x.Matricula).IsUnique();
        v.Property(x => x.Modelo).HasMaxLength(50).IsRequired();
        v.Property(x => x.Estado).HasMaxLength(20).IsRequired();
        v.Property(x => x.Versao).IsConcurrencyToken();
        var u = modelBuilder.Entity<Usuario>();
        u.Property(x => x.NomeUtilizador).HasMaxLength(100).IsRequired();
        u.HasIndex(x => x.NomeUtilizador).IsUnique();
        u.Property(x => x.PalavraPasseHash).IsRequired();
    }
}
