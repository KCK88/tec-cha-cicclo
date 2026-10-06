using Microsoft.EntityFrameworkCore;

namespace Cicclo.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<UserAccount> Users => Set<UserAccount>();
    public DbSet<LaundryService> Services => Set<LaundryService>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<UserAccount>(entity =>
        {
            entity.ToTable("users", table =>
            {
                table.HasCheckConstraint("ck_users_balance_non_negative", "balance_cents >= 0");
            });
            entity.HasKey(user => user.Id);
            entity.Property(user => user.Id).HasColumnName("id");
            entity.Property(user => user.Email).HasColumnName("email").HasMaxLength(254).IsRequired();
            entity.HasIndex(user => user.Email).IsUnique();
            entity.Property(user => user.PasswordHash).HasColumnName("password_hash").IsRequired();
            entity.Property(user => user.BalanceCents).HasColumnName("balance_cents");
        });

        modelBuilder.Entity<LaundryService>(entity =>
        {
            entity.ToTable("services", table =>
            {
                table.HasCheckConstraint("ck_services_price_positive", "price_cents > 0");
            });
            entity.HasKey(service => service.Id);
            entity.Property(service => service.Id).HasColumnName("id");
            entity.Property(service => service.Name).HasColumnName("name").HasMaxLength(80).IsRequired();
            entity.Property(service => service.PriceCents).HasColumnName("price_cents");
        });
    }
}
