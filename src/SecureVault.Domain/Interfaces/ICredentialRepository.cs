using SecureVault.Domain.Entities;

namespace SecureVault.Domain.Interfaces;

public interface ICredentialRepository
{
    Task<Credential?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<Credential?> GetByIdAndUserIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Credential>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IEnumerable<Credential>> SearchByUserIdAsync(Guid userId, string query, CancellationToken cancellationToken = default);
    Task AddAsync(Credential credential, CancellationToken cancellationToken = default);
    Task UpdateAsync(Credential credential, CancellationToken cancellationToken = default);
    Task DeleteAsync(Credential credential, CancellationToken cancellationToken = default);
}
