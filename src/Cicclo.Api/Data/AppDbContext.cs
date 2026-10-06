using Microsoft.EntityFrameworkCore;

namespace Cicclo.Api.Data;

public sealed class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<UserAccount> Users => Set<UserAccount>();
    public DbSet<LaundryService> Services => Set<LaundryService>();
    public DbSet<WalletEntry> Entries => Set<WalletEntry>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();

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

        modelBuilder.Entity<WalletEntry>(entity =>
        {
            entity.ToTable("wallet_entries", table =>
            {
                table.HasCheckConstraint("ck_wallet_entries_amount_positive", "amount_cents > 0");
            });
            entity.HasKey(entry => entry.Id);
            entity.Property(entry => entry.Id).HasColumnName("id");
            entity.Property(entry => entry.UserId).HasColumnName("user_id");
            entity.Property(entry => entry.Kind).HasColumnName("kind").HasMaxLength(32).IsRequired();
            entity.Property(entry => entry.AmountCents).HasColumnName("amount_cents");
            entity.Property(entry => entry.ServiceId).HasColumnName("service_id");
            entity.Property(entry => entry.ServiceName).HasColumnName("service_name").HasMaxLength(80);
            entity.Property(entry => entry.ReversesEntryId).HasColumnName("reverses_entry_id");
            entity.Property(entry => entry.CreatedAt).HasColumnName("created_at");
            entity.Property(entry => entry.Cancelled).HasColumnName("cancelled");
            entity.HasIndex(entry => entry.UserId);
            entity.HasOne<UserAccount>().WithMany().HasForeignKey(entry => entry.UserId);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.ToTable("refresh_tokens");
            entity.HasKey(token => token.Id);
            entity.Property(token => token.Id).HasColumnName("id");
            entity.Property(token => token.UserId).HasColumnName("user_id");
            entity.Property(token => token.TokenHash).HasColumnName("token_hash").HasMaxLength(128).IsRequired();
            entity.HasIndex(token => token.TokenHash).IsUnique();
            entity.Property(token => token.ExpiresAt).HasColumnName("expires_at");
            entity.Property(token => token.RevokedAt).HasColumnName("revoked_at");
            entity.HasOne<UserAccount>().WithMany().HasForeignKey(token => token.UserId);
        });
    }
}
