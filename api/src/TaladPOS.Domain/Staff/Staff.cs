namespace TaladPOS.Domain.Staff;

/// <summary>
/// A staff/cashier account (FR-007-FR-009, FR-029). Password hashing and JWT
/// issuance live outside the domain (Application layer, research.md #1) -
/// this entity only stores the already-hashed value.
/// </summary>
public class Staff
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Username { get; private set; }
    public string PasswordHash { get; private set; }
    public StaffRole Role { get; private set; }

    // EF Core materialization constructor.
    private Staff()
    {
        Name = string.Empty;
        Username = string.Empty;
        PasswordHash = string.Empty;
    }

    public Staff(string name, string username, string passwordHash, StaffRole role)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Name is required.", nameof(name));
        }

        if (string.IsNullOrWhiteSpace(username))
        {
            throw new ArgumentException("Username is required.", nameof(username));
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            throw new ArgumentException("PasswordHash is required.", nameof(passwordHash));
        }

        Id = Guid.NewGuid();
        Name = name;
        Username = username;
        PasswordHash = passwordHash;
        Role = role;
    }
}
