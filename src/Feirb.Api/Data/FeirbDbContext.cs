using Feirb.Api.Data.Entities;
using Microsoft.EntityFrameworkCore;

namespace Feirb.Api.Data;

public class FeirbDbContext(DbContextOptions<FeirbDbContext> options) : DbContext(options)
{
    public DbSet<User> Users => Set<User>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<SmtpSettings> SmtpSettings => Set<SmtpSettings>();
    public DbSet<Mailbox> Mailboxes => Set<Mailbox>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Username).IsUnique();
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Username).HasMaxLength(100);
            entity.Property(e => e.Email).HasMaxLength(256);
            entity.Property(e => e.PasswordHash).HasMaxLength(256);
        });

        modelBuilder.Entity<SmtpSettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Host).HasMaxLength(256);
            entity.Property(e => e.Username).HasMaxLength(256);
            entity.Property(e => e.EncryptedPassword).HasMaxLength(1024);
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Token).IsUnique();
            entity.Property(e => e.Token).HasMaxLength(128);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Mailbox>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.Property(e => e.Name).HasMaxLength(256);
            entity.Property(e => e.EmailAddress).HasMaxLength(256);
            entity.Property(e => e.DisplayName).HasMaxLength(256);
            entity.Property(e => e.ImapHost).HasMaxLength(256);
            entity.Property(e => e.ImapUsername).HasMaxLength(256);
            entity.Property(e => e.ImapEncryptedPassword).HasMaxLength(1024);
            entity.Property(e => e.SmtpHost).HasMaxLength(256);
            entity.Property(e => e.SmtpUsername).HasMaxLength(256);
            entity.Property(e => e.SmtpEncryptedPassword).HasMaxLength(1024);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
