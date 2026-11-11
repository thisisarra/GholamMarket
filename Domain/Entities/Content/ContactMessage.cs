using Domain.Common;
using NibFramework.Model;

namespace Domain.Entities.Content;

public class ContactMessage : BaseEntity<int>
{
    public string Name { get; private set; } = null!;
    public string Email { get; private set; } = null!;
    public string Body { get; private set; } = null!;

    protected ContactMessage()
    {
    }

    public ContactMessage(string name, string email, string body)
    {
        Name = Guard.Required(name, nameof(name), FieldLengths.Name);
        Email = Guard.Required(email, nameof(email), FieldLengths.Name);
        Body = Guard.Required(body, nameof(body), FieldLengths.Comment);
        CreatedAt = DateTime.UtcNow;
    }
}
