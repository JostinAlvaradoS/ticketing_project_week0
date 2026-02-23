namespace Identity.Domain.Entities
{
    public class User
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        // Otros campos pueden agregarse en fases posteriores
    }
}
