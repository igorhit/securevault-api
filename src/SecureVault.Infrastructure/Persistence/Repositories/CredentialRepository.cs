using Microsoft.EntityFrameworkCore;
using SecureVault.Domain.Entities;
using SecureVault.Domain.Interfaces;

namespace SecureVault.Infrastructure.Persistence.Repositories;

public class CredentialRepository : ICredentialRepository
{
    private readonly AppDbContext _context;

    public CredentialRepository(AppDbContext context)
    {
        _context = context;
    }

    public Task<Credential?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        => _context.Credentials.FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

    public Task<Credential?> GetByIdAndUserIdAsync(Guid id, Guid userId, CancellationToken cancellationToken = default)
        => _context.Credentials.FirstOrDefaultAsync(c => c.Id == id && c.UserId == userId, cancellationToken);

    public async Task<IEnumerable<Credential>> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
        => await _context.Credentials
            .Where(c => c.UserId == userId)
            .OrderBy(c => c.Title)
            .ToListAsync(cancellationToken);

    public async Task<IEnumerable<Credential>> SearchByUserIdAsync(Guid userId, string query, CancellationToken cancellationToken = default)
        => await _context.Credentials
            .Where(c => c.UserId == userId &&
                (c.Title.ToLower().Contains(query.ToLower()) ||
                 (c.Url != null && c.Url.ToLower().Contains(query.ToLower()))))
            .OrderBy(c => c.Title)
            .ToListAsync(cancellationToken);

    public async Task AddAsync(Credential credential, CancellationToken cancellationToken = default)
        => await _context.Credentials.AddAsync(credential, cancellationToken);

    public Task UpdateAsync(Credential credential, CancellationToken cancellationToken = default)
    {
        _context.Credentials.Update(credential);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Credential credential, CancellationToken cancellationToken = default)
    {
        _context.Credentials.Remove(credential);
        return Task.CompletedTask;
    }
}
