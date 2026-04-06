namespace SecureVault.Domain.Entities;

public class Credential
{
    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string? Url { get; private set; }

    // Campos armazenados criptografados com AES-256-GCM
    public string EncryptedUsername { get; private set; } = string.Empty;
    public string EncryptedPassword { get; private set; } = string.Empty;
    public string? EncryptedNotes { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    public User User { get; private set; } = null!;

    private Credential() { }

    public static Credential Create(
        Guid userId,
        string title,
        string encryptedUsername,
        string encryptedPassword,
        string? url = null,
        string? encryptedNotes = null)
    {
        return new Credential
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Title = title.Trim(),
            Url = url?.Trim(),
            EncryptedUsername = encryptedUsername,
            EncryptedPassword = encryptedPassword,
            EncryptedNotes = encryptedNotes,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void Update(
        string title,
        string encryptedUsername,
        string encryptedPassword,
        string? url = null,
        string? encryptedNotes = null)
    {
        Title = title.Trim();
        Url = url?.Trim();
        EncryptedUsername = encryptedUsername;
        EncryptedPassword = encryptedPassword;
        EncryptedNotes = encryptedNotes;
        UpdatedAt = DateTime.UtcNow;
    }
}
