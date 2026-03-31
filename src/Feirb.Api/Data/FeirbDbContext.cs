using Feirb.Api.Data.Entities;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Feirb.Api.Data;

public class FeirbDbContext(DbContextOptions<FeirbDbContext> options) : DbContext(options), IDataProtectionKeyContext
{
    public DbSet<User> Users => Set<User>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<SmtpSettings> SmtpSettings => Set<SmtpSettings>();
    public DbSet<Mailbox> Mailboxes => Set<Mailbox>();
    public DbSet<CachedMessage> CachedMessages => Set<CachedMessage>();
    public DbSet<CachedAttachment> CachedAttachments => Set<CachedAttachment>();
    public DbSet<DashboardLayout> DashboardLayouts => Set<DashboardLayout>();
    public DbSet<WidgetConfig> WidgetConfigs => Set<WidgetConfig>();
    public DbSet<Label> Labels => Set<Label>();
    public DbSet<JobSettings> JobSettings => Set<JobSettings>();
    public DbSet<JobExecution> JobExecutions => Set<JobExecution>();
    public DbSet<Avatar> Avatars => Set<Avatar>();
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();

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
            entity.Property(e => e.SecurityStamp).HasMaxLength(64);
            entity.Property(e => e.TimeZone).HasMaxLength(64);
            entity.Property(e => e.Theme).HasMaxLength(32).HasDefaultValue("green-light");
        });

        modelBuilder.Entity<SmtpSettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Host).HasMaxLength(256);
            entity.Property(e => e.Username).HasMaxLength(256);
            entity.Property(e => e.EncryptedPassword).HasMaxLength(1024);
            entity.Property(e => e.FromAddress).HasMaxLength(256);
            entity.Property(e => e.FromName).HasMaxLength(256);
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
            entity.Property(e => e.BadgeColor).HasMaxLength(9);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CachedMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.MailboxId, e.MessageId }).IsUnique();
            entity.HasIndex(e => e.Date);
            entity.Property(e => e.MessageId).HasMaxLength(512);
            entity.Property(e => e.Subject).HasMaxLength(1024);
            entity.Property(e => e.From).HasMaxLength(512);
            entity.Property(e => e.ReplyTo).HasMaxLength(512);
            entity.Property(e => e.To).HasMaxLength(2048);
            entity.Property(e => e.Cc).HasMaxLength(2048);
            entity.HasOne(e => e.Mailbox)
                .WithMany(m => m.CachedMessages)
                .HasForeignKey(e => e.MailboxId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<CachedAttachment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Filename).HasMaxLength(512);
            entity.Property(e => e.MimeType).HasMaxLength(256);
            entity.HasOne(e => e.CachedMessage)
                .WithMany(m => m.Attachments)
                .HasForeignKey(e => e.CachedMessageId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DashboardLayout>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId).IsUnique();
            entity.Property(e => e.LayoutJson).HasColumnType("jsonb");
            entity.HasOne(e => e.User)
                .WithOne()
                .HasForeignKey<DashboardLayout>(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<WidgetConfig>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.UserId, e.WidgetInstanceId }).IsUnique();
            entity.Property(e => e.WidgetInstanceId).HasMaxLength(256);
            entity.Property(e => e.ConfigValue).HasMaxLength(4096);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Label>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.UserId);
            entity.Property(e => e.Name).HasMaxLength(50);
            entity.Property(e => e.Color).HasMaxLength(7);
            entity.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<JobSettings>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.JobName).IsUnique();
            entity.HasIndex(e => new { e.JobType, e.ResourceId });
            entity.Property(e => e.JobName).HasMaxLength(100);
            entity.Property(e => e.JobType).HasMaxLength(50);
            entity.Property(e => e.Description).HasMaxLength(500);
            entity.Property(e => e.Cron).HasMaxLength(100);
            entity.Property(e => e.ResourceType).HasMaxLength(500);
            entity.Property(e => e.LastStatus)
                .HasConversion<string>()
                .HasMaxLength(20);
            entity.Property(e => e.RowVersion).IsConcurrencyToken();
            entity.HasOne<User>()
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.SetNull);
            entity.HasData(new JobSettings
            {
                Id = new Guid("a1b2c3d4-e5f6-7890-abcd-ef1234567890"),
                JobName = "Classification",
                JobType = "classification",
                Description = "Classifies new mail messages using AI-powered label detection.",
                Cron = "0 * * * * ?",
                Enabled = false,
                RowVersion = new Guid("00000000-0000-0000-0000-000000000001"),
            });
        });

        modelBuilder.Entity<JobExecution>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.JobSettingsId);
            entity.HasIndex(e => e.StartedAt);
            entity.Property(e => e.Status)
                .HasConversion<string>()
                .HasMaxLength(20);
            entity.Property(e => e.Error).HasMaxLength(4096);
            entity.HasOne(e => e.JobSettings)
                .WithMany(j => j.Executions)
                .HasForeignKey(e => e.JobSettingsId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Avatar>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.EmailHash).IsUnique();
            entity.Property(e => e.EmailHash).HasMaxLength(32);
            entity.Property(e => e.Email).HasMaxLength(256);
        });
    }
}
