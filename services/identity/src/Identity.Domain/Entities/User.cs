using Identity.Domain.ValueObjects;

namespace Identity.Domain.Entities;

public class User
{
    public Guid Id { get; private set; }
    public string Email { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public Role Role { get; private set; } = Role.User;

    private User() { }

    public User(string email, string passwordHash, Role role = Role.User)
    {
        Id = Guid.NewGuid();
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
    }

    /// <summary>
    /// Determina si el usuario tiene rol de administrador
    /// </summary>
    public bool IsAdmin() => Role == Role.Admin;

    /// <summary>
    /// Otorga privilegios de administrador al usuario
    /// </summary>
    public void GrantAdminRole()
    {
        Role = Role.Admin;
    }

    /// <summary>
    /// Revoca privilegios de administrador del usuario
    /// </summary>
    public void RevokeAdminRole()
    {
        Role = Role.User;
    }
}