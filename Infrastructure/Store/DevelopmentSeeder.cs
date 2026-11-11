using Domain.Entities.Catalog;
using Domain.Entities.Customers;
using Domain.Entities.Users;
using Infrastructure.DBContexts;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Raya.Auth.Configuration;

namespace Infrastructure.Store;

public static class DevelopmentSeeder
{
    public const string AdminEmail = "admin@gholam.market";
    public const string AdminPassword = "Admin@12345";
    public const string UserEmail = "user@gholam.market";
    public const string UserPassword = "User@12345";

    public static async Task SeedAsync(IServiceProvider services, CancellationToken cancellationToken = default)
    {
        var roles = services.GetRequiredService<RoleManager<Role>>();
        var users = services.GetRequiredService<UserManager<User>>();
        var db = services.GetRequiredService<GholamContext>();

        foreach (var name in DefaultRoles.All)
        {
            if (!await roles.RoleExistsAsync(name))
                await roles.CreateAsync(new Role { Name = name });
        }

        await EnsureUserAsync(users, db, AdminEmail, AdminPassword, "مدیر", "غلام", "09120000000", DefaultRoles.Admin, cancellationToken);
        await EnsureUserAsync(users, db, UserEmail, UserPassword, "کاربر", "نمونه", "09121111111", DefaultRoles.User, cancellationToken);

        if (await db.Products.AnyAsync(cancellationToken))
            return;

        var grocery = new Category("مواد غذایی", "grocery", "خشکبار، برنج و خواربار", 1);
        var dairy = new Category("لبنیات", "dairy", "شیر، ماست و پنیر", 2);
        var drinks = new Category("نوشیدنی", "drinks", "چای، آبمیوه و نوشابه", 3);
        db.Categories.AddRange(grocery, dairy, drinks);

        db.Products.AddRange(
            new Product(grocery, "برنج طارم ممتاز", "tarom-rice", 485000, 40, "GM-RICE-1", "برنج معطر یک کیلوگرمی شمال.", null),
            new Product(grocery, "روغن آفتابگردان", "sunflower-oil", 92000, 80, "GM-OIL-1", "روغن مایع ۱.۸ لیتری.", null),
            new Product(grocery, "عدس درجه یک", "lentils", 64000, 60, "GM-LENTIL-1", "عدس ۹۰۰ گرمی.", null),
            new Product(dairy, "شیر تازه کم‌چرب", "low-fat-milk", 28000, 100, "GM-MILK-1", "شیر یک لیتری روزانه.", null),
            new Product(dairy, "ماست موسیر", "shallot-yogurt", 42000, 50, "GM-YOGURT-1", "ماست چکیده ۷۵۰ گرمی.", null),
            new Product(dairy, "پنیر لیقوان", "lighvan-cheese", 155000, 25, "GM-CHEESE-1", "پنیر سنتی ۴۰۰ گرمی.", null),
            new Product(drinks, "چای ممتاز لاهیجان", "lahijan-tea", 118000, 70, "GM-TEA-1", "چای سیاه ۴۵۰ گرمی.", null),
            new Product(drinks, "آب پرتقال طبیعی", "orange-juice", 36000, 90, "GM-JUICE-1", "آبمیوه یک لیتری بدون شکر افزوده.", null));

        db.Products.Local.First(p => p.Sku == "GM-RICE-1").SetDiscount(8);
        await db.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureUserAsync(
        UserManager<User> users,
        GholamContext db,
        string email,
        string password,
        string firstName,
        string lastName,
        string phone,
        string role,
        CancellationToken cancellationToken)
    {
        var user = await users.FindByEmailAsync(email);
        if (user is null)
        {
            user = new User
            {
                UserName = email,
                Email = email,
                EmailConfirmed = true,
                PhoneNumber = phone,
                FirstName = firstName,
                LastName = lastName,
                Status = 1
            };
            var created = await users.CreateAsync(user, password);
            if (!created.Succeeded)
                return;
        }

        if (!await users.IsInRoleAsync(user, DefaultRoles.User))
            await users.AddToRoleAsync(user, DefaultRoles.User);
        if (role == DefaultRoles.Admin && !await users.IsInRoleAsync(user, DefaultRoles.Admin))
            await users.AddToRoleAsync(user, DefaultRoles.Admin);

        if (!await db.Customers.AnyAsync(c => c.UserId == user.Id, cancellationToken))
            db.Customers.Add(new Customer(user, $"{firstName} {lastName}"));

        await db.SaveChangesAsync(cancellationToken);
    }
}
 