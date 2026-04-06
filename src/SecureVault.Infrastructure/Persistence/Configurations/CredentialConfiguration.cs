using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SecureVault.Domain.Entities;

namespace SecureVault.Infrastructure.Persistence.Configurations;

public class CredentialConfiguration : IEntityTypeConfiguration<Credential>
{
    public void Configure(EntityTypeBuilder<Credential> builder)
    {
        builder.ToTable("credentials");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).HasColumnName("id");
        builder.Property(c => c.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(c => c.Title).HasColumnName("title").HasMaxLength(100).IsRequired();
        builder.Property(c => c.Url).HasColumnName("url").HasMaxLength(2048);
        builder.Property(c => c.EncryptedUsername).HasColumnName("encrypted_username").IsRequired();
        builder.Property(c => c.EncryptedPassword).HasColumnName("encrypted_password").IsRequired();
        builder.Property(c => c.EncryptedNotes).HasColumnName("encrypted_notes");
        builder.Property(c => c.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(c => c.UpdatedAt).HasColumnName("updated_at");

        // Índice para buscas por usuário
        builder.HasIndex(c => c.UserId);
        // Índice para buscas por título dentro do escopo do usuário
        builder.HasIndex(c => new { c.UserId, c.Title });
    }
}
