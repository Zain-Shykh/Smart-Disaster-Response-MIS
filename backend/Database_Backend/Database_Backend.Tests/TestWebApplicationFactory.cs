using Database_Backend.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Database_Backend.Tests;

public class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName = $"Database_Backend_Tests_{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DatabaseProjectContext>();
            services.RemoveAll<DbContextOptions<DatabaseProjectContext>>();

            var sqlServerServices = services
                .Where(descriptor => descriptor.ServiceType.Namespace is not null
                    && descriptor.ServiceType.Namespace.StartsWith("Microsoft.EntityFrameworkCore.SqlServer", StringComparison.Ordinal))
                .ToList();

            foreach (var descriptor in sqlServerServices)
            {
                services.Remove(descriptor);
            }

            var inMemoryProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            services.AddDbContext<DatabaseProjectContext>(options =>
                options
                    .UseInMemoryDatabase(_databaseName)
                    .UseInternalServiceProvider(inMemoryProvider));

            using var scope = services.BuildServiceProvider().CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<DatabaseProjectContext>();
            dbContext.Database.EnsureCreated();
            SeedData(dbContext);
        });
    }

    private static void SeedData(DatabaseProjectContext context)
    {
        if (context.Users.Any())
        {
            return;
        }

        var adminRole = new Role
        {
            RoleId = 1,
            RoleName = "Administrator",
            Description = "System administrator"
        };

        var opsRole = new Role
        {
            RoleId = 2,
            RoleName = "EmergencyOperator",
            Description = "Emergency operations user"
        };

        var admin = new User
        {
            UserId = 1,
            FirstName = "Admin",
            LastName = "User",
            Username = "admin1",
            PasswordHash = "hash_admin1",
            Email = "admin1@example.com",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var operatorUser = new User
        {
            UserId = 2,
            FirstName = "Ops",
            LastName = "User",
            Username = "ops2",
            PasswordHash = "hash_ops2",
            Email = "ops2@example.com",
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        var citizen = new Citizen
        {
            CitizenId = 1,
            FirstName = "Seed",
            LastName = "Citizen",
            NationalId = "CITIZEN-0001",
            Email = "citizen1@example.com",
            Street = "Seed Street",
            Area = "Seed Area",
            City = "Seed City",
            Province = "Seed Province",
            TotalReports = 0
        };

        var disasterEvent = new DisasterEvent
        {
            EventId = 1,
            EventName = "Seed Event",
            DisasterType = "Flood",
            StartTime = DateTime.UtcNow.AddHours(-6),
            Street = "Seed Event Street",
            Area = "Seed Event Area",
            City = "Seed Event City",
            Province = "Seed Event Province",
            Status = "Active",
            AffectedPopulation = 250,
            TotalReports = 0
        };

        context.Roles.AddRange(adminRole, opsRole);
        context.Users.AddRange(admin, operatorUser);
        context.Citizens.Add(citizen);
        context.DisasterEvents.Add(disasterEvent);
        context.UserRoles.AddRange(
            new UserRole
            {
                UserId = 1,
                RoleId = 1,
                AssignedAt = DateTime.UtcNow
            },
            new UserRole
            {
                UserId = 2,
                RoleId = 2,
                AssignedAt = DateTime.UtcNow
            });

        context.SaveChanges();
    }
}
