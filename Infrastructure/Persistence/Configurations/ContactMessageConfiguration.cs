using Domain.Common;
using Domain.Entities.Content;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class ContactMessageConfiguration : IEntityTypeConfiguration<ContactMessage>
{
    public void Configure(EntityTypeBuilder<ContactMessage> builder)
    {
        builder.ToTable("ContactMessages");
        builder.Property(x => x.Name).HasMaxLength(FieldLengths.Name).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(FieldLengths.Name).IsRequired();
        builder.Property(x => x.Body).HasMaxLength(FieldLengths.Comment).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
    }
}
