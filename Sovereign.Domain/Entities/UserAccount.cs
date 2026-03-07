namespace Sovereign.Domain.Entities;

public sealed class UserAccount
{
    private UserAccount()
    {
        Email = string.Empty;
        PasswordHash = string.Empty;
        TenantId = string.Empty;
    }

    public UserAccount(Guid id, string email, string passwordHash, string tenantId)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentException("Email is required.", nameof(email));
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new ArgumentException("PasswordHash is required.", nameof(passwordHash));
        if (string.IsNullOrWhiteSpace(tenantId))
            throw new ArgumentException("TenantId is required.", nameof(tenantId));

        Id = id;
        Email = email;
        PasswordHash = passwordHash;
        TenantId = tenantId;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public string Email { get; private set; }
    public string PasswordHash { get; private set; }
    public string TenantId { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
}
