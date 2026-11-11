using Domain.Common;
using Domain.Entities.Catalog;
using Domain.Entities.Customers;
using Domain.Entities.Sales;
using Domain.Entities.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.ToTable("Categories");
        builder.Property(x => x.Name).HasMaxLength(FieldLengths.Name).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(FieldLengths.Slug).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(FieldLengths.Description);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => x.Slug).IsUnique();

        builder.HasOne(x => x.Parent)
            .WithMany(x => x.Children)
            .HasForeignKey(x => x.ParentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        builder.Property(x => x.Name).HasMaxLength(FieldLengths.Name).IsRequired();
        builder.Property(x => x.Slug).HasMaxLength(FieldLengths.Slug).IsRequired();
        builder.Property(x => x.Sku).HasMaxLength(FieldLengths.Sku).IsRequired();
        builder.Property(x => x.Description).HasMaxLength(FieldLengths.Description);
        builder.Property(x => x.ImageUrl).HasMaxLength(FieldLengths.Url);
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => x.Slug).IsUnique();
        builder.HasIndex(x => x.Sku).IsUnique();
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasOne(x => x.Category)
            .WithMany(x => x.Products)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Attributes)
            .WithOne(x => x.Product)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Reviews)
            .WithOne(x => x.Product)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ProductAttributeConfiguration : IEntityTypeConfiguration<ProductAttribute>
{
    public void Configure(EntityTypeBuilder<ProductAttribute> builder)
    {
        builder.ToTable("ProductAttributes");
        builder.Property(x => x.Name).HasMaxLength(FieldLengths.Name).IsRequired();
        builder.Property(x => x.Value).HasMaxLength(FieldLengths.Value).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => new { x.ProductId, x.Name }).IsUnique();
    }
}

public class ReviewConfiguration : IEntityTypeConfiguration<Review>
{
    public void Configure(EntityTypeBuilder<Review> builder)
    {
        builder.ToTable("Reviews");
        builder.Property(x => x.Title).HasMaxLength(FieldLengths.Title);
        builder.Property(x => x.Comment).HasMaxLength(FieldLengths.Comment).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => new { x.ProductId, x.CustomerId }).IsUnique();
    }
}

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.Property(x => x.UserId).HasMaxLength(FieldLengths.UserId).IsRequired();
        builder.Property(x => x.FullName).HasMaxLength(FieldLengths.Name).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => x.UserId).IsUnique();

        builder.HasOne(x => x.User)
            .WithOne(x => x.Customer)
            .HasForeignKey<Customer>(x => x.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Addresses)
            .WithOne(x => x.Customer)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Orders)
            .WithOne(x => x.Customer)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(x => x.Reviews)
            .WithOne(x => x.Customer)
            .HasForeignKey(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class AddressConfiguration : IEntityTypeConfiguration<Address>
{
    public void Configure(EntityTypeBuilder<Address> builder)
    {
        builder.ToTable("Addresses");
        builder.Property(x => x.Title).HasMaxLength(FieldLengths.Title).IsRequired();
        builder.Property(x => x.ReceiverName).HasMaxLength(FieldLengths.Name).IsRequired();
        builder.Property(x => x.ReceiverPhone).HasMaxLength(FieldLengths.Phone).IsRequired();
        builder.Property(x => x.Province).HasMaxLength(FieldLengths.Province).IsRequired();
        builder.Property(x => x.City).HasMaxLength(FieldLengths.City).IsRequired();
        builder.Property(x => x.Street).HasMaxLength(FieldLengths.Street).IsRequired();
        builder.Property(x => x.Plaque).HasMaxLength(FieldLengths.Plaque);
        builder.Property(x => x.Unit).HasMaxLength(FieldLengths.Plaque);
        builder.Property(x => x.PostalCode).HasMaxLength(FieldLengths.PostalCode).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => new { x.CustomerId, x.IsDefault })
            .IsUnique()
            .HasFilter("[IsDefault] = 1 AND [IsDeleted] = 0");
    }
}

public class CartConfiguration : IEntityTypeConfiguration<Cart>
{
    public void Configure(EntityTypeBuilder<Cart> builder)
    {
        builder.ToTable("Carts");
        builder.Property(x => x.CreatedAt).IsRequired();

        builder.HasOne(x => x.Customer)
            .WithOne(x => x.Cart)
            .HasForeignKey<Cart>(x => x.CustomerId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Items)
            .WithOne(x => x.Cart)
            .HasForeignKey(x => x.CartId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class CartItemConfiguration : IEntityTypeConfiguration<CartItem>
{
    public void Configure(EntityTypeBuilder<CartItem> builder)
    {
        builder.ToTable("CartItems");
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => new { x.CartId, x.ProductId }).IsUnique();

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        builder.Property(x => x.OrderNumber).HasMaxLength(FieldLengths.OrderNumber).IsRequired();
        builder.Property(x => x.CustomerNote).HasMaxLength(FieldLengths.Comment);
        builder.Property(x => x.ReceiverName).HasMaxLength(FieldLengths.Name).IsRequired();
        builder.Property(x => x.ReceiverPhone).HasMaxLength(FieldLengths.Phone).IsRequired();
        builder.Property(x => x.Province).HasMaxLength(FieldLengths.Province).IsRequired();
        builder.Property(x => x.City).HasMaxLength(FieldLengths.City).IsRequired();
        builder.Property(x => x.Street).HasMaxLength(FieldLengths.Street).IsRequired();
        builder.Property(x => x.Plaque).HasMaxLength(FieldLengths.Plaque);
        builder.Property(x => x.Unit).HasMaxLength(FieldLengths.Plaque);
        builder.Property(x => x.PostalCode).HasMaxLength(FieldLengths.PostalCode).IsRequired();
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => x.OrderNumber).IsUnique();
        builder.HasIndex(x => x.Status);

        builder.HasMany(x => x.Items)
            .WithOne(x => x.Order)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(x => x.Payments)
            .WithOne(x => x.Order)
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.Shipment)
            .WithOne(x => x.Order)
            .HasForeignKey<Shipment>(x => x.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.DiscountCode)
            .WithMany(x => x.Orders)
            .HasForeignKey(x => x.DiscountCodeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.ShippingAddress)
            .WithMany()
            .HasForeignKey(x => x.ShippingAddressId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");
        builder.Property(x => x.ProductName).HasMaxLength(FieldLengths.Name).IsRequired();
        builder.Property(x => x.Sku).HasMaxLength(FieldLengths.Sku).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => new { x.OrderId, x.ProductId }).IsUnique();

        builder.HasOne(x => x.Product)
            .WithMany()
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");
        builder.Property(x => x.Method).HasConversion<int>();
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.Reference).HasMaxLength(FieldLengths.Reference);
        builder.Property(x => x.FailureReason).HasMaxLength(FieldLengths.Reason);
        builder.Property(x => x.CreatedAt).IsRequired();
    }
}

public class ShipmentConfiguration : IEntityTypeConfiguration<Shipment>
{
    public void Configure(EntityTypeBuilder<Shipment> builder)
    {
        builder.ToTable("Shipments");
        builder.Property(x => x.Carrier).HasMaxLength(FieldLengths.Carrier);
        builder.Property(x => x.TrackingCode).HasMaxLength(FieldLengths.TrackingCode);
        builder.Property(x => x.Status).HasConversion<int>();
        builder.Property(x => x.CreatedAt).IsRequired();
    }
}

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.Property(x => x.FirstName).HasMaxLength(FieldLengths.Name);
        builder.Property(x => x.LastName).HasMaxLength(FieldLengths.Name);
    }
}

public class DiscountCodeConfiguration : IEntityTypeConfiguration<DiscountCode>
{
    public void Configure(EntityTypeBuilder<DiscountCode> builder)
    {
        builder.ToTable("DiscountCodes");
        builder.Property(x => x.Code).HasMaxLength(FieldLengths.Code).IsRequired();
        builder.Property(x => x.Type).HasConversion<int>();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.HasIndex(x => x.Code).IsUnique();
    }
}
