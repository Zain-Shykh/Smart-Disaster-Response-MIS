using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Database_Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Database_Backend.Tests;

public class ApiHardeningIntegrationTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;
    private readonly TestWebApplicationFactory _factory;

    public ApiHardeningIntegrationTests(TestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task InvalidLoginPayload_ReturnsValidationProblemDetails()
    {
        var response = await _client.PostAsJsonAsync("/api/Auth/login", new { });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.Equal("https://httpstatuses.com/400", GetString(document.RootElement, "type"));
        Assert.Equal("Request validation failed", GetString(document.RootElement, "title"));
        Assert.Equal(400, GetInt32(document.RootElement, "status"));
        Assert.True(document.RootElement.TryGetProperty("errors", out var errors));
        Assert.True(errors.TryGetProperty("UsernameOrEmail", out _));
        Assert.True(errors.TryGetProperty("Password", out _));
    }

    [Fact]
    public async Task MissingRoute_ReturnsProblemDetails404()
    {
        var response = await _client.GetAsync("/api/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.Equal("https://httpstatuses.com/404", GetString(document.RootElement, "type"));
        Assert.Equal("Not Found", GetString(document.RootElement, "title"));
        Assert.Equal(404, GetInt32(document.RootElement, "status"));
    }

    [Fact]
    public async Task UnauthorizedReportsRequest_ReturnsProblemDetails401()
    {
        var response = await _client.GetAsync("/api/Reports/overview");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.Equal("https://httpstatuses.com/401", GetString(document.RootElement, "type"));
        Assert.Equal("Unauthorized", GetString(document.RootElement, "title"));
        Assert.Equal(401, GetInt32(document.RootElement, "status"));
    }

    [Fact]
    public async Task InvalidDonationPayload_ReturnsValidationProblemDetails()
    {
        var token = await LoginAndGetTokenAsync("admin1", "hash_admin1");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var payload = new
        {
            donorId = 1,
            eventId = 1,
            amount = -12.5,
            paymentMethod = "Cash",
            status = "Pending"
        };

        var response = await _client.PostAsJsonAsync("/api/DonationFinance/donations", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.Equal("Request validation failed", GetString(document.RootElement, "title"));
        Assert.True(document.RootElement.TryGetProperty("errors", out var errors));
        Assert.True(errors.TryGetProperty("Amount", out _));
    }

    [Fact]
    public async Task RbacRolesEndpoint_EnforcesAdministratorRole()
    {
        var adminToken = await LoginAndGetTokenAsync("admin1", "hash_admin1");
        var opsToken = await LoginAndGetTokenAsync("ops2", "hash_ops2");

        using var adminRequest = new HttpRequestMessage(HttpMethod.Get, "/api/Rbac/roles");
        adminRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);
        var adminResponse = await _client.SendAsync(adminRequest);

        using var opsRequest = new HttpRequestMessage(HttpMethod.Get, "/api/Rbac/roles");
        opsRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", opsToken);
        var opsResponse = await _client.SendAsync(opsRequest);

        Assert.Equal(HttpStatusCode.OK, adminResponse.StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, opsResponse.StatusCode);
    }

    private async Task<string> LoginAndGetTokenAsync(string usernameOrEmail, string password)
    {
        var response = await _client.PostAsJsonAsync("/api/Auth/login", new
        {
            usernameOrEmail,
            password
        });

        response.EnsureSuccessStatusCode();

        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var token = GetString(document.RootElement, "accessToken");
        Assert.False(string.IsNullOrWhiteSpace(token));
        return token;
    }

    private static string GetString(JsonElement element, string propertyName)
    {
        Assert.True(element.TryGetProperty(propertyName, out var value));
        return value.GetString() ?? string.Empty;
    }

    private static int GetInt32(JsonElement element, string propertyName)
    {
        Assert.True(element.TryGetProperty(propertyName, out var value));
        return value.GetInt32();
    }

    [Fact]
    public async Task Login_WithLegacySeedHash_UpgradesUserPasswordHashToPbkdf2()
    {
        var token = await LoginAndGetTokenAsync("admin1", "hash_admin1");
        Assert.False(string.IsNullOrWhiteSpace(token));

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DatabaseProjectContext>();

        var user = await context.Users.FirstAsync(item => item.Username == "admin1");

        Assert.StartsWith("pbkdf2$", user.PasswordHash, StringComparison.OrdinalIgnoreCase);
        Assert.NotEqual("hash_admin1", user.PasswordHash);
    }

    [Fact]
    public async Task CreateUser_StoresPasswordUsingPbkdf2Format()
    {
        var adminToken = await LoginAndGetTokenAsync("admin1", "hash_admin1");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var username = $"pbkdf2_user_{suffix}";
        var email = $"{username}@example.com";
        var plainPassword = "SecurePass123";

        var createResponse = await _client.PostAsJsonAsync("/api/User", new
        {
            username,
            email,
            password = plainPassword,
            firstName = "Pbkdf2",
            lastName = "User",
            roleIds = new[] { 2 }
        });
        createResponse.EnsureSuccessStatusCode();

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<DatabaseProjectContext>();
        var user = await context.Users.FirstAsync(item => item.Username == username);

        Assert.StartsWith("pbkdf2$", user.PasswordHash, StringComparison.OrdinalIgnoreCase);
        Assert.NotEqual(plainPassword, user.PasswordHash);

        var loginResponse = await _client.PostAsJsonAsync("/api/Auth/login", new
        {
            usernameOrEmail = username,
            password = plainPassword
        });

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
    }

    // ===== User Management Tests =====

    [Fact]
    public async Task CreateUser_WithValidPayload_ReturnsCreated()
    {
        var token = await LoginAndGetTokenAsync("admin1", "hash_admin1");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var payload = new
        {
            username = "newuser1",
            email = "newuser1@example.com",
            password = "SecurePass123",
            firstName = "John",
            lastName = "Doe",
            roleIds = new[] { 2 } // EmergencyOperator role
        };

        var response = await _client.PostAsJsonAsync("/api/User", payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.Equal("newuser1", GetString(document.RootElement, "username"));
        Assert.Equal("newuser1@example.com", GetString(document.RootElement, "email"));
        Assert.Equal("John", GetString(document.RootElement, "firstName"));
        Assert.Equal("Doe", GetString(document.RootElement, "lastName"));
        Assert.True(document.RootElement.TryGetProperty("roles", out _));
    }

    [Fact]
    public async Task CreateUser_WithDuplicateUsername_ReturnsBadRequest()
    {
        var token = await LoginAndGetTokenAsync("admin1", "hash_admin1");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var payload = new
        {
            username = "admin1", // Already exists
            email = "newemail@example.com",
            password = "SecurePass123",
            firstName = "Test",
            lastName = "User",
            roleIds = new int[] { }
        };

        var response = await _client.PostAsJsonAsync("/api/User", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_WithDuplicateEmail_ReturnsBadRequest()
    {
        var token = await LoginAndGetTokenAsync("admin1", "hash_admin1");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var payload = new
        {
            username = "newuser2",
            email = "admin1@example.com", // Already exists
            password = "SecurePass123",
            firstName = "Test",
            lastName = "User",
            roleIds = new int[] { }
        };

        var response = await _client.PostAsJsonAsync("/api/User", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateUser_WithInvalidEmail_ReturnsBadRequest()
    {
        var token = await LoginAndGetTokenAsync("admin1", "hash_admin1");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var payload = new
        {
            username = "newuser3",
            email = "not-an-email",
            password = "SecurePass123",
            firstName = "Test",
            lastName = "User",
            roleIds = new int[] { }
        };

        var response = await _client.PostAsJsonAsync("/api/User", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task CreateUser_WithoutAdminRole_ReturnsForbidden()
    {
        var token = await LoginAndGetTokenAsync("ops2", "hash_ops2");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var payload = new
        {
            username = "newuser4",
            email = "newuser4@example.com",
            password = "SecurePass123",
            firstName = "Test",
            lastName = "User",
            roleIds = new int[] { }
        };

        var response = await _client.PostAsJsonAsync("/api/User", payload);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetUsers_WithAdminRole_ReturnsUsersList()
    {
        var token = await LoginAndGetTokenAsync("admin1", "hash_admin1");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/User?pageNumber=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.True(document.RootElement.TryGetProperty("users", out var users));
        Assert.Equal(JsonValueKind.Array, users.ValueKind);
        Assert.True(document.RootElement.TryGetProperty("pageNumber", out _));
        Assert.True(document.RootElement.TryGetProperty("totalCount", out _));
    }

    [Fact]
    public async Task GetUsers_WithoutAdminRole_ReturnsForbidden()
    {
        var token = await LoginAndGetTokenAsync("ops2", "hash_ops2");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/User");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetUserById_WithValidId_ReturnsUserDetails()
    {
        var token = await LoginAndGetTokenAsync("admin1", "hash_admin1");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/User/1");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.Equal(1, GetInt32(document.RootElement, "userId"));
        Assert.Equal("admin1", GetString(document.RootElement, "username"));
    }

    [Fact]
    public async Task GetUserById_WithInvalidId_ReturnsNotFound()
    {
        var token = await LoginAndGetTokenAsync("admin1", "hash_admin1");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/User/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateUser_WithValidPayload_ReturnsUpdatedUser()
    {
        var token = await LoginAndGetTokenAsync("admin1", "hash_admin1");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var createPayload = new
        {
            username = $"upduser_{suffix}",
            email = $"upd_{suffix}@example.com",
            password = "SecurePass123",
            firstName = "Before",
            lastName = "Update",
            roleIds = new[] { 2 }
        };
        var createResponse = await _client.PostAsJsonAsync("/api/User", createPayload);
        createResponse.EnsureSuccessStatusCode();
        using var createDocument = await JsonDocument.ParseAsync(await createResponse.Content.ReadAsStreamAsync());
        var userId = GetInt32(createDocument.RootElement, "userId");

        var payload = new
        {
            firstName = "UpdatedFirst",
            lastName = "UpdatedLast",
            email = "updated@example.com"
        };

        var response = await _client.PutAsJsonAsync($"/api/User/{userId}", payload);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.Equal("UpdatedFirst", GetString(document.RootElement, "firstName"));
        Assert.Equal("UpdatedLast", GetString(document.RootElement, "lastName"));
        Assert.Equal("updated@example.com", GetString(document.RootElement, "email"));
    }

    [Fact]
    public async Task DeleteUser_WithValidId_ReturnNoContent()
    {
        var token = await LoginAndGetTokenAsync("admin1", "hash_admin1");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var createPayload = new
        {
            username = $"deluser_{suffix}",
            email = $"del_{suffix}@example.com",
            password = "SecurePass123",
            firstName = "To",
            lastName = "Delete",
            roleIds = new[] { 2 }
        };
        var createResponse = await _client.PostAsJsonAsync("/api/User", createPayload);
        createResponse.EnsureSuccessStatusCode();
        using var createDocument = await JsonDocument.ParseAsync(await createResponse.Content.ReadAsStreamAsync());
        var userId = GetInt32(createDocument.RootElement, "userId");

        var response = await _client.DeleteAsync($"/api/User/{userId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        // Verify user is marked as inactive
        var getResponse = await _client.GetAsync($"/api/User/{userId}");
        using var document = await JsonDocument.ParseAsync(await getResponse.Content.ReadAsStreamAsync());
        Assert.False(document.RootElement.GetProperty("isActive").GetBoolean());
    }

    // ===== Role Management Tests =====

    [Fact]
    public async Task CreateRole_WithValidPayload_ReturnsCreated()
    {
        var token = await LoginAndGetTokenAsync("admin1", "hash_admin1");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var suffix = Guid.NewGuid().ToString("N")[..8];

        var payload = new
        {
            roleName = $"CustomRole_{suffix}",
            description = "A custom test role"
        };

        var response = await _client.PostAsJsonAsync("/api/Role", payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.Equal($"CustomRole_{suffix}", GetString(document.RootElement, "roleName"));
        Assert.Equal("A custom test role", GetString(document.RootElement, "description"));
    }

    [Fact]
    public async Task CreateRole_WithDuplicateName_ReturnsBadRequest()
    {
        var token = await LoginAndGetTokenAsync("admin1", "hash_admin1");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var payload = new
        {
            roleName = "Administrator", // Already exists
            description = "Duplicate role"
        };

        var response = await _client.PostAsJsonAsync("/api/Role", payload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetRoles_WithAdminRole_ReturnsRolesList()
    {
        var token = await LoginAndGetTokenAsync("admin1", "hash_admin1");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/Role?pageNumber=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.True(document.RootElement.TryGetProperty("roles", out var roles));
        Assert.Equal(JsonValueKind.Array, roles.ValueKind);
    }

    [Fact]
    public async Task UpdateRole_WithValidPayload_ReturnsUpdatedRole()
    {
        var token = await LoginAndGetTokenAsync("admin1", "hash_admin1");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var createResponse = await _client.PostAsJsonAsync("/api/Role", new
        {
            roleName = $"RoleToUpdate_{suffix}",
            description = "Before update"
        });
        createResponse.EnsureSuccessStatusCode();
        using var createDocument = await JsonDocument.ParseAsync(await createResponse.Content.ReadAsStreamAsync());
        var roleId = GetInt32(createDocument.RootElement, "roleId");

        var payload = new
        {
            roleName = $"UpdatedRole_{suffix}",
            description = "Updated description"
        };

        var response = await _client.PutAsJsonAsync($"/api/Role/{roleId}", payload);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.Equal($"UpdatedRole_{suffix}", GetString(document.RootElement, "roleName"));
    }

    // ===== Permission Management Tests =====

    [Fact]
    public async Task CreatePermission_WithValidPayload_ReturnsCreated()
    {
        var token = await LoginAndGetTokenAsync("admin1", "hash_admin1");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var suffix = Guid.NewGuid().ToString("N")[..8];

        var payload = new
        {
            permissionName = $"ViewReports_{suffix}",
            module = "Reports",
            action = "View"
        };

        var response = await _client.PostAsJsonAsync("/api/Permission", payload);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.Equal($"ViewReports_{suffix}", GetString(document.RootElement, "permissionName"));
        Assert.Equal("Reports", GetString(document.RootElement, "module"));
    }

    [Fact]
    public async Task GetPermissions_WithAdminRole_ReturnsPermissionsList()
    {
        var token = await LoginAndGetTokenAsync("admin1", "hash_admin1");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.GetAsync("/api/Permission?pageNumber=1&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var document = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        Assert.True(document.RootElement.TryGetProperty("permissions", out var permissions));
        Assert.Equal(JsonValueKind.Array, permissions.ValueKind);
    }

    [Fact]
    public async Task MapPermissionToRole_WithValidPayload_ReturnsOk()
    {
        var token = await LoginAndGetTokenAsync("admin1", "hash_admin1");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var roleResponse = await _client.PostAsJsonAsync("/api/Role", new
        {
            roleName = $"MapRole_{suffix}",
            description = "Role for map test"
        });
        roleResponse.EnsureSuccessStatusCode();
        using var roleDocument = await JsonDocument.ParseAsync(await roleResponse.Content.ReadAsStreamAsync());
        var roleId = GetInt32(roleDocument.RootElement, "roleId");

        var permissionResponse = await _client.PostAsJsonAsync("/api/Permission", new
        {
            permissionName = $"MapPermission_{suffix}",
            module = "Testing",
            action = "Map"
        });
        permissionResponse.EnsureSuccessStatusCode();
        using var permissionDocument = await JsonDocument.ParseAsync(await permissionResponse.Content.ReadAsStreamAsync());
        var permissionId = GetInt32(permissionDocument.RootElement, "permissionId");

        var payload = new
        {
            roleId,
            permissionId
        };

        var response = await _client.PostAsJsonAsync("/api/Rbac/role-permission", payload);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task UnmapPermissionFromRole_WithValidPayload_ReturnsNoContent()
    {
        var token = await LoginAndGetTokenAsync("admin1", "hash_admin1");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var roleResponse = await _client.PostAsJsonAsync("/api/Role", new
        {
            roleName = $"UnmapRole_{suffix}",
            description = "Role for unmap test"
        });
        roleResponse.EnsureSuccessStatusCode();
        using var roleDocument = await JsonDocument.ParseAsync(await roleResponse.Content.ReadAsStreamAsync());
        var roleId = GetInt32(roleDocument.RootElement, "roleId");

        var permissionResponse = await _client.PostAsJsonAsync("/api/Permission", new
        {
            permissionName = $"UnmapPermission_{suffix}",
            module = "Testing",
            action = "Unmap"
        });
        permissionResponse.EnsureSuccessStatusCode();
        using var permissionDocument = await JsonDocument.ParseAsync(await permissionResponse.Content.ReadAsStreamAsync());
        var permissionId = GetInt32(permissionDocument.RootElement, "permissionId");

        // First map  it
        await _client.PostAsJsonAsync("/api/Rbac/role-permission", new { roleId, permissionId });

        // Then unmap it
        var response = await _client.DeleteAsync($"/api/Rbac/role-permission/{roleId}/{permissionId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task RoleAndPermissionApis_WithoutAdminRole_ReturnsForbidden()
    {
        var token = await LoginAndGetTokenAsync("ops2", "hash_ops2");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var rolePayload = new { roleName = "TestRole", description = "Test" };
        var roleResponse = await _client.PostAsJsonAsync("/api/Role", rolePayload);
        Assert.Equal(HttpStatusCode.Forbidden, roleResponse.StatusCode);

        var permPayload = new { permissionName = "Test", module = "Test", action = "Test" };
        var permResponse = await _client.PostAsJsonAsync("/api/Permission", permPayload);
        Assert.Equal(HttpStatusCode.Forbidden, permResponse.StatusCode);
    }

    // ===== Team Activity Tests =====

    [Fact]
    public async Task CreateTeamActivity_WithEmergencyOperator_ReturnsCreated()
    {
        var suffix = Guid.NewGuid().ToString("N")[..8];

        var adminToken = await LoginAndGetTokenAsync("admin1", "hash_admin1");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var teamCreateResponse = await _client.PostAsJsonAsync("/api/RescueTeam", new
        {
            teamName = $"Team_{suffix}",
            teamType = "Rescue",
            street = "Street 1",
            area = "Area 1",
            city = "City 1",
            province = "Province 1",
            availabilityStatus = "Available",
            capacity = 10
        });
        teamCreateResponse.EnsureSuccessStatusCode();
        using var teamDocument = await JsonDocument.ParseAsync(await teamCreateResponse.Content.ReadAsStreamAsync());
        var teamId = GetInt32(teamDocument.RootElement, "teamId");

        var opsToken = await LoginAndGetTokenAsync("ops2", "hash_ops2");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", opsToken);

        var activityResponse = await _client.PostAsJsonAsync("/api/TeamActivity", new
        {
            teamId,
            activityType = "FieldCheck",
            startTime = DateTime.UtcNow.AddMinutes(-15),
            endTime = DateTime.UtcNow,
            notes = "Reached location",
            outcome = "Completed"
        });

        Assert.Equal(HttpStatusCode.Created, activityResponse.StatusCode);
        using var activityDocument = await JsonDocument.ParseAsync(await activityResponse.Content.ReadAsStreamAsync());
        Assert.Equal(teamId, GetInt32(activityDocument.RootElement, "teamId"));
        Assert.Equal("FieldCheck", GetString(activityDocument.RootElement, "activityType"));
    }

    [Fact]
    public async Task CreateTeamActivity_WithAdministrator_ReturnsForbidden()
    {
        var token = await LoginAndGetTokenAsync("admin1", "hash_admin1");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/api/TeamActivity", new
        {
            teamId = 1,
            activityType = "FieldCheck",
            startTime = DateTime.UtcNow.AddMinutes(-15)
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateTeamActivity_WithInvalidTeam_ReturnsNotFound()
    {
        var token = await LoginAndGetTokenAsync("ops2", "hash_ops2");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/api/TeamActivity", new
        {
            teamId = 999999,
            activityType = "FieldCheck",
            startTime = DateTime.UtcNow.AddMinutes(-15)
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    // ===== Phone Management Tests =====

    [Fact]
    public async Task UserPhone_OwnerCanAddListUpdateDelete()
    {
        var token = await LoginAndGetTokenAsync("ops2", "hash_ops2");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var addResponse = await _client.PostAsJsonAsync("/api/User/2/Phone", new
        {
            phoneNumber = "+15550000001"
        });
        Assert.Equal(HttpStatusCode.Created, addResponse.StatusCode);

        var listResponse = await _client.GetAsync("/api/User/2/Phone");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        using var listDocument = await JsonDocument.ParseAsync(await listResponse.Content.ReadAsStreamAsync());
        Assert.Equal(JsonValueKind.Array, listDocument.RootElement.ValueKind);
        Assert.True(listDocument.RootElement.GetArrayLength() >= 1);

        var updateResponse = await _client.PutAsJsonAsync("/api/User/2/Phone/+15550000001", new
        {
            newPhoneNumber = "+15550000002"
        });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var deleteResponse = await _client.DeleteAsync("/api/User/2/Phone/+15550000002");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task UserPhone_NonOwnerNonAdmin_IsForbidden()
    {
        var token = await LoginAndGetTokenAsync("ops2", "hash_ops2");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/api/User/1/Phone", new
        {
            phoneNumber = "+15550000111"
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UserPhone_InvalidPhone_ReturnsBadRequest()
    {
        var token = await LoginAndGetTokenAsync("admin1", "hash_admin1");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await _client.PostAsJsonAsync("/api/User/1/Phone", new
        {
            phoneNumber = "not-a-phone"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task DonorPhone_AdminCanAddListUpdateDelete_AndOpsForbidden()
    {
        var adminToken = await LoginAndGetTokenAsync("admin1", "hash_admin1");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var donorCreateResponse = await _client.PostAsJsonAsync("/api/DonationFinance/donors", new
        {
            firstName = "Donor",
            lastName = $"Phone{suffix}",
            donorType = "Individual",
            email = $"donor_{suffix}@example.com",
            street = "Street 1",
            area = "Area 1",
            city = "City 1",
            province = "Province 1"
        });
        donorCreateResponse.EnsureSuccessStatusCode();
        using var donorDocument = await JsonDocument.ParseAsync(await donorCreateResponse.Content.ReadAsStreamAsync());
        var donorId = GetInt32(donorDocument.RootElement, "donorId");

        var addResponse = await _client.PostAsJsonAsync($"/api/Donor/{donorId}/Phone", new
        {
            phoneNumber = "+16660000001"
        });
        Assert.Equal(HttpStatusCode.Created, addResponse.StatusCode);

        var listResponse = await _client.GetAsync($"/api/Donor/{donorId}/Phone");
        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);

        var updateResponse = await _client.PutAsJsonAsync($"/api/Donor/{donorId}/Phone/+16660000001", new
        {
            newPhoneNumber = "+16660000002"
        });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var deleteResponse = await _client.DeleteAsync($"/api/Donor/{donorId}/Phone/+16660000002");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var opsToken = await LoginAndGetTokenAsync("ops2", "hash_ops2");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", opsToken);
        var forbiddenResponse = await _client.PostAsJsonAsync($"/api/Donor/{donorId}/Phone", new
        {
            phoneNumber = "+16660000003"
        });
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);
    }

    // ===== Inventory History Tests =====

    [Fact]
    public async Task InventoryHistory_AdminCanReadAndExportCsv()
    {
        var adminToken = await LoginAndGetTokenAsync("admin1", "hash_admin1");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var suffix = Guid.NewGuid().ToString("N")[..8];

        var resourceResponse = await _client.PostAsJsonAsync("/api/ResourceLogistics/resources", new
        {
            resourceName = $"WaterKit_{suffix}",
            resourceType = "Water",
            unit = "Box",
            description = "History test resource"
        });
        resourceResponse.EnsureSuccessStatusCode();
        using var resourceDocument = await JsonDocument.ParseAsync(await resourceResponse.Content.ReadAsStreamAsync());
        var resourceId = GetInt32(resourceDocument.RootElement, "resourceId");

        var warehouseResponse = await _client.PostAsJsonAsync("/api/ResourceLogistics/warehouses", new
        {
            warehouseName = $"WH_{suffix}",
            street = "Street 1",
            area = "Area 1",
            city = "City 1",
            province = "Province 1",
            capacity = 100,
            managerId = 1,
            contactPhone = "+10000000001",
            contactEmail = $"wh_{suffix}@example.com"
        });
        warehouseResponse.EnsureSuccessStatusCode();
        using var warehouseDocument = await JsonDocument.ParseAsync(await warehouseResponse.Content.ReadAsStreamAsync());
        var warehouseId = GetInt32(warehouseDocument.RootElement, "warehouseId");

        var inventoryResponse = await _client.PostAsJsonAsync("/api/ResourceLogistics/inventories", new
        {
            warehouseId,
            resourceId,
            quantity = 500m,
            minThreshold = 50m,
            maxCapacity = 1000m
        });
        inventoryResponse.EnsureSuccessStatusCode();
        using var inventoryDocument = await JsonDocument.ParseAsync(await inventoryResponse.Content.ReadAsStreamAsync());
        var inventoryId = GetInt32(inventoryDocument.RootElement, "inventoryId");

        var eventResponse = await _client.PostAsJsonAsync("/api/DisasterEvent", new
        {
            eventName = $"Flood_{suffix}",
            disasterType = "Flood",
            startTime = DateTime.UtcNow.AddHours(-4),
            street = "Street 2",
            area = "Area 2",
            city = "City 2",
            province = "Province 2",
            status = "Active",
            affectedPopulation = 100
        });
        eventResponse.EnsureSuccessStatusCode();
        using var eventDocument = await JsonDocument.ParseAsync(await eventResponse.Content.ReadAsStreamAsync());
        var eventId = GetInt32(eventDocument.RootElement, "eventId");

        var allocationResponse = await _client.PostAsJsonAsync("/api/ResourceLogistics/allocations", new
        {
            inventoryId,
            eventId,
            requestedBy = 1,
            quantity = 25m,
            requestTime = DateTime.UtcNow.AddHours(-2),
            status = "Dispatched",
            dispatchedAt = DateTime.UtcNow.AddHours(-1)
        });
        allocationResponse.EnsureSuccessStatusCode();

        var historyResponse = await _client.GetAsync($"/api/InventoryHistory/inventory/{inventoryId}/history");
        Assert.Equal(HttpStatusCode.OK, historyResponse.StatusCode);
        using var historyDocument = await JsonDocument.ParseAsync(await historyResponse.Content.ReadAsStreamAsync());
        Assert.Equal(JsonValueKind.Array, historyDocument.RootElement.ValueKind);
        Assert.True(historyDocument.RootElement.GetArrayLength() >= 1);

        var warehouseHistoryResponse = await _client.GetAsync($"/api/InventoryHistory/warehouse/{warehouseId}/history");
        Assert.Equal(HttpStatusCode.OK, warehouseHistoryResponse.StatusCode);

        var exportResponse = await _client.GetAsync($"/api/InventoryHistory/inventory/{inventoryId}/history/export?format=csv");
        Assert.Equal(HttpStatusCode.OK, exportResponse.StatusCode);
        Assert.Equal("text/csv", exportResponse.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task InventoryHistory_NonAdminForbidden_AndDateRangeValidation()
    {
        var opsToken = await LoginAndGetTokenAsync("ops2", "hash_ops2");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", opsToken);

        var forbiddenResponse = await _client.GetAsync("/api/InventoryHistory/inventory/1/history");
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);

        var adminToken = await LoginAndGetTokenAsync("admin1", "hash_admin1");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var badDateResponse = await _client.GetAsync("/api/InventoryHistory/inventory/1/history?startDate=2026-01-02&endDate=2026-01-01");
        Assert.Equal(HttpStatusCode.BadRequest, badDateResponse.StatusCode);
    }

    // ===== Hospital Specialization Routing Tests =====

    [Fact]
    public async Task HospitalSearch_BySpecialization_ReturnsMatchingHospitals()
    {
        var adminToken = await LoginAndGetTokenAsync("admin1", "hash_admin1");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var suffix = Guid.NewGuid().ToString("N")[..8];

        var hospitalResponse = await _client.PostAsJsonAsync("/api/HospitalPatient/hospitals", new
        {
            hospitalName = $"RouteHospital_{suffix}",
            street = "Street 1",
            area = "Area 1",
            city = "City 1",
            province = "Province 1",
            totalBeds = 120,
            availableBeds = 40
        });
        hospitalResponse.EnsureSuccessStatusCode();
        using var hospitalDocument = await JsonDocument.ParseAsync(await hospitalResponse.Content.ReadAsStreamAsync());
        var hospitalId = GetInt32(hospitalDocument.RootElement, "hospitalId");

        var specializationResponse = await _client.PostAsJsonAsync($"/api/HospitalPatient/hospitals/{hospitalId}/specializations", new
        {
            specialization = "Trauma"
        });
        specializationResponse.EnsureSuccessStatusCode();

        var searchResponse = await _client.GetAsync("/api/HospitalPatient/hospitals/search?specialization=Trauma&bedRequirement=1");

        Assert.Equal(HttpStatusCode.OK, searchResponse.StatusCode);
        using var searchDocument = await JsonDocument.ParseAsync(await searchResponse.Content.ReadAsStreamAsync());
        Assert.Equal(JsonValueKind.Array, searchDocument.RootElement.ValueKind);
        Assert.True(searchDocument.RootElement.GetArrayLength() >= 1);
    }

    [Fact]
    public async Task HospitalSearch_WithUnknownSpecialization_ReturnsBadRequest()
    {
        var adminToken = await LoginAndGetTokenAsync("admin1", "hash_admin1");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var response = await _client.GetAsync("/api/HospitalPatient/hospitals/search?specialization=UnknownSpecialization");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task RoutePatientToHospital_WithMatchingSpecialization_ReturnsCreated()
    {
        var adminToken = await LoginAndGetTokenAsync("admin1", "hash_admin1");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var suffix = Guid.NewGuid().ToString("N")[..8];

        var hospitalResponse = await _client.PostAsJsonAsync("/api/HospitalPatient/hospitals", new
        {
            hospitalName = $"RouteHospital2_{suffix}",
            street = "Street 2",
            area = "Area 2",
            city = "City 2",
            province = "Province 2",
            totalBeds = 80,
            availableBeds = 20
        });
        hospitalResponse.EnsureSuccessStatusCode();
        using var hospitalDocument = await JsonDocument.ParseAsync(await hospitalResponse.Content.ReadAsStreamAsync());
        var hospitalId = GetInt32(hospitalDocument.RootElement, "hospitalId");

        var specializationResponse = await _client.PostAsJsonAsync($"/api/HospitalPatient/hospitals/{hospitalId}/specializations", new
        {
            specialization = "Cardiac"
        });
        specializationResponse.EnsureSuccessStatusCode();

        var patientResponse = await _client.PostAsJsonAsync("/api/HospitalPatient/patients", new
        {
            firstName = "Pat",
            lastName = $"Route{suffix}",
            age = 34,
            bloodType = "O+",
            contactPhone = "+17770000001"
        });
        patientResponse.EnsureSuccessStatusCode();
        using var patientDocument = await JsonDocument.ParseAsync(await patientResponse.Content.ReadAsStreamAsync());
        var patientId = GetInt32(patientDocument.RootElement, "patientId");

        var routeResponse = await _client.PostAsJsonAsync($"/api/HospitalPatient/hospitals/{hospitalId}/route-patient", new
        {
            patientId,
            requiredSpecialization = "Cardiac",
            condition = "Serious",
            status = "Admitted"
        });

        Assert.Equal(HttpStatusCode.Created, routeResponse.StatusCode);
        using var routeDocument = await JsonDocument.ParseAsync(await routeResponse.Content.ReadAsStreamAsync());
        Assert.Equal(hospitalId, GetInt32(routeDocument.RootElement, "hospitalId"));
        Assert.Equal(patientId, GetInt32(routeDocument.RootElement, "patientId"));
    }

    [Fact]
    public async Task RoutePatientToHospital_WithoutRequiredSpecialization_ReturnsNotFound()
    {
        var adminToken = await LoginAndGetTokenAsync("admin1", "hash_admin1");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var suffix = Guid.NewGuid().ToString("N")[..8];

        var hospitalResponse = await _client.PostAsJsonAsync("/api/HospitalPatient/hospitals", new
        {
            hospitalName = $"RouteHospital3_{suffix}",
            street = "Street 3",
            area = "Area 3",
            city = "City 3",
            province = "Province 3",
            totalBeds = 60,
            availableBeds = 15
        });
        hospitalResponse.EnsureSuccessStatusCode();
        using var hospitalDocument = await JsonDocument.ParseAsync(await hospitalResponse.Content.ReadAsStreamAsync());
        var hospitalId = GetInt32(hospitalDocument.RootElement, "hospitalId");

        var patientResponse = await _client.PostAsJsonAsync("/api/HospitalPatient/patients", new
        {
            firstName = "Pat",
            lastName = $"NoSpec{suffix}",
            age = 29,
            bloodType = "A+",
            contactPhone = "+17770000002"
        });
        patientResponse.EnsureSuccessStatusCode();
        using var patientDocument = await JsonDocument.ParseAsync(await patientResponse.Content.ReadAsStreamAsync());
        var patientId = GetInt32(patientDocument.RootElement, "patientId");

        // First create specialization globally on another hospital so specialization exists in system.
        var anotherHospitalResponse = await _client.PostAsJsonAsync("/api/HospitalPatient/hospitals", new
        {
            hospitalName = $"SpecSource_{suffix}",
            street = "Street 4",
            area = "Area 4",
            city = "City 4",
            province = "Province 4",
            totalBeds = 40,
            availableBeds = 10
        });
        anotherHospitalResponse.EnsureSuccessStatusCode();
        using var anotherHospitalDocument = await JsonDocument.ParseAsync(await anotherHospitalResponse.Content.ReadAsStreamAsync());
        var anotherHospitalId = GetInt32(anotherHospitalDocument.RootElement, "hospitalId");

        var addSpecResponse = await _client.PostAsJsonAsync($"/api/HospitalPatient/hospitals/{anotherHospitalId}/specializations", new
        {
            specialization = "Pediatric"
        });
        addSpecResponse.EnsureSuccessStatusCode();

        var routeResponse = await _client.PostAsJsonAsync($"/api/HospitalPatient/hospitals/{hospitalId}/route-patient", new
        {
            patientId,
            requiredSpecialization = "Pediatric",
            condition = "Stable",
            status = "Admitted"
        });

        Assert.Equal(HttpStatusCode.NotFound, routeResponse.StatusCode);
    }

    [Fact]
    public async Task AutoRoutePatient_UsesProvinceFallback_WhenCityHasNoCapacity()
    {
        var adminToken = await LoginAndGetTokenAsync("admin1", "hash_admin1");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var suffix = Guid.NewGuid().ToString("N")[..8];

        var cityHospitalResponse = await _client.PostAsJsonAsync("/api/HospitalPatient/hospitals", new
        {
            hospitalName = $"CityHospital_{suffix}",
            street = "Street 1",
            area = "Area 1",
            city = "CityFallback",
            province = "ProvinceFallback",
            totalBeds = 100,
            availableBeds = 0
        });
        cityHospitalResponse.EnsureSuccessStatusCode();
        using var cityHospitalDocument = await JsonDocument.ParseAsync(await cityHospitalResponse.Content.ReadAsStreamAsync());
        var cityHospitalId = GetInt32(cityHospitalDocument.RootElement, "hospitalId");

        var citySpecResponse = await _client.PostAsJsonAsync($"/api/HospitalPatient/hospitals/{cityHospitalId}/specializations", new
        {
            specialization = "Trauma"
        });
        citySpecResponse.EnsureSuccessStatusCode();

        var provinceHospitalResponse = await _client.PostAsJsonAsync("/api/HospitalPatient/hospitals", new
        {
            hospitalName = $"ProvinceHospital_{suffix}",
            street = "Street 2",
            area = "Area 2",
            city = "OtherCity",
            province = "ProvinceFallback",
            totalBeds = 80,
            availableBeds = 12
        });
        provinceHospitalResponse.EnsureSuccessStatusCode();
        using var provinceHospitalDocument = await JsonDocument.ParseAsync(await provinceHospitalResponse.Content.ReadAsStreamAsync());
        var provinceHospitalId = GetInt32(provinceHospitalDocument.RootElement, "hospitalId");

        var provinceSpecResponse = await _client.PostAsJsonAsync($"/api/HospitalPatient/hospitals/{provinceHospitalId}/specializations", new
        {
            specialization = "Trauma"
        });
        provinceSpecResponse.EnsureSuccessStatusCode();

        var patientResponse = await _client.PostAsJsonAsync("/api/HospitalPatient/patients", new
        {
            firstName = "Auto",
            lastName = $"Patient{suffix}",
            age = 31,
            bloodType = "B+",
            contactPhone = "+17770001001"
        });
        patientResponse.EnsureSuccessStatusCode();
        using var patientDocument = await JsonDocument.ParseAsync(await patientResponse.Content.ReadAsStreamAsync());
        var patientId = GetInt32(patientDocument.RootElement, "patientId");

        var reportResponse = await _client.PostAsJsonAsync("/api/EmergencyReport", new
        {
            citizenId = 1,
            eventId = 1,
            street = "Route Street",
            area = "Route Area",
            city = "CityFallback",
            province = "ProvinceFallback",
            disasterType = "Flood",
            severityLevel = "High",
            status = "Pending",
            source = "Mobile"
        });
        reportResponse.EnsureSuccessStatusCode();
        using var reportDocument = await JsonDocument.ParseAsync(await reportResponse.Content.ReadAsStreamAsync());
        var reportId = GetInt32(reportDocument.RootElement, "reportId");

        var autoRouteResponse = await _client.PostAsJsonAsync("/api/HospitalPatient/hospitals/route-patient/auto", new
        {
            patientId,
            reportId,
            requiredSpecialization = "Trauma",
            bedRequirement = 1,
            condition = "Serious",
            status = "Admitted"
        });

        Assert.Equal(HttpStatusCode.Created, autoRouteResponse.StatusCode);
        using var autoRouteDocument = await JsonDocument.ParseAsync(await autoRouteResponse.Content.ReadAsStreamAsync());
        Assert.Equal(provinceHospitalId, GetInt32(autoRouteDocument.RootElement, "selectedHospitalId"));
        Assert.Equal("Province", GetString(autoRouteDocument.RootElement, "routingTierUsed"));
        Assert.True(autoRouteDocument.RootElement.GetProperty("fallbackApplied").GetBoolean());
    }

    [Fact]
    public async Task AutoRoutePatient_NoCapacity_ReturnsEscalationConflict()
    {
        var adminToken = await LoginAndGetTokenAsync("admin1", "hash_admin1");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var suffix = Guid.NewGuid().ToString("N")[..8];

        var hospitalResponse = await _client.PostAsJsonAsync("/api/HospitalPatient/hospitals", new
        {
            hospitalName = $"EscalationHospital_{suffix}",
            street = "Street E",
            area = "Area E",
            city = "EscalationCity",
            province = "EscalationProvince",
            totalBeds = 50,
            availableBeds = 0
        });
        hospitalResponse.EnsureSuccessStatusCode();
        using var hospitalDocument = await JsonDocument.ParseAsync(await hospitalResponse.Content.ReadAsStreamAsync());
        var hospitalId = GetInt32(hospitalDocument.RootElement, "hospitalId");

        var specResponse = await _client.PostAsJsonAsync($"/api/HospitalPatient/hospitals/{hospitalId}/specializations", new
        {
            specialization = "Burn"
        });
        specResponse.EnsureSuccessStatusCode();

        var patientResponse = await _client.PostAsJsonAsync("/api/HospitalPatient/patients", new
        {
            firstName = "Esc",
            lastName = $"Patient{suffix}",
            age = 45,
            bloodType = "O+",
            contactPhone = "+17770001002"
        });
        patientResponse.EnsureSuccessStatusCode();
        using var patientDocument = await JsonDocument.ParseAsync(await patientResponse.Content.ReadAsStreamAsync());
        var patientId = GetInt32(patientDocument.RootElement, "patientId");

        var reportResponse = await _client.PostAsJsonAsync("/api/EmergencyReport", new
        {
            citizenId = 1,
            eventId = 1,
            street = "Esc Street",
            area = "Esc Area",
            city = "EscalationCity",
            province = "EscalationProvince",
            disasterType = "Fire",
            severityLevel = "Critical",
            status = "Pending",
            source = "Mobile"
        });
        reportResponse.EnsureSuccessStatusCode();
        using var reportDocument = await JsonDocument.ParseAsync(await reportResponse.Content.ReadAsStreamAsync());
        var reportId = GetInt32(reportDocument.RootElement, "reportId");

        var autoRouteResponse = await _client.PostAsJsonAsync("/api/HospitalPatient/hospitals/route-patient/auto", new
        {
            patientId,
            reportId,
            requiredSpecialization = "Burn",
            bedRequirement = 1,
            condition = "Critical",
            status = "Admitted"
        });

        Assert.Equal(HttpStatusCode.Conflict, autoRouteResponse.StatusCode);
        using var autoRouteDocument = await JsonDocument.ParseAsync(await autoRouteResponse.Content.ReadAsStreamAsync());
        Assert.False(autoRouteDocument.RootElement.GetProperty("routed").GetBoolean());
        Assert.True(autoRouteDocument.RootElement.GetProperty("escalationRequired").GetBoolean());
        Assert.Equal("Regional", GetString(autoRouteDocument.RootElement, "escalationLevel"));
    }

    // ===== Incident Prioritization Tests =====

    [Fact]
    public async Task EmergencyReport_RecalculatePriority_ReturnsPriorityPayload()
    {
        var token = await LoginAndGetTokenAsync("ops2", "hash_ops2");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createResponse = await _client.PostAsJsonAsync("/api/EmergencyReport", new
        {
            citizenId = 1,
            eventId = 1,
            street = "Report Street",
            area = "Report Area",
            city = "Report City",
            province = "Report Province",
            disasterType = "Flood",
            severityLevel = "Critical",
            status = "Pending",
            source = "Mobile",
            description = "Priority calculation test"
        });
        createResponse.EnsureSuccessStatusCode();
        using var createDocument = await JsonDocument.ParseAsync(await createResponse.Content.ReadAsStreamAsync());
        var reportId = GetInt32(createDocument.RootElement, "reportId");

        var recalcResponse = await _client.PutAsync($"/api/EmergencyReport/{reportId}/priority", null);

        Assert.Equal(HttpStatusCode.OK, recalcResponse.StatusCode);
        using var recalcDocument = await JsonDocument.ParseAsync(await recalcResponse.Content.ReadAsStreamAsync());

        var priorityLevel = GetInt32(recalcDocument.RootElement, "priorityLevel");
        Assert.True(priorityLevel >= 1 && priorityLevel <= 4);
        Assert.True(recalcDocument.RootElement.TryGetProperty("priorityScore", out _));
        Assert.True(recalcDocument.RootElement.TryGetProperty("estimatedResponseMinutes", out _));
    }

    [Fact]
    public async Task Reports_PrioritizedIncidents_ReturnsSortedByPriority()
    {
        var token = await LoginAndGetTokenAsync("ops2", "hash_ops2");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var criticalResponse = await _client.PostAsJsonAsync("/api/EmergencyReport", new
        {
            citizenId = 1,
            eventId = 1,
            street = "Critical Street",
            area = "Critical Area",
            city = "Priority City",
            province = "Priority Province",
            disasterType = "Flood",
            severityLevel = "Critical",
            status = "Pending",
            source = "Mobile"
        });
        criticalResponse.EnsureSuccessStatusCode();

        var lowResponse = await _client.PostAsJsonAsync("/api/EmergencyReport", new
        {
            citizenId = 1,
            eventId = 1,
            street = "Low Street",
            area = "Low Area",
            city = "Priority City",
            province = "Priority Province",
            disasterType = "Flood",
            severityLevel = "Low",
            status = "Pending",
            source = "Mobile"
        });
        lowResponse.EnsureSuccessStatusCode();

        var prioritizedResponse = await _client.GetAsync("/api/Reports/incidents/prioritized?limit=10");

        Assert.Equal(HttpStatusCode.OK, prioritizedResponse.StatusCode);
        using var prioritizedDocument = await JsonDocument.ParseAsync(await prioritizedResponse.Content.ReadAsStreamAsync());
        Assert.Equal(JsonValueKind.Array, prioritizedDocument.RootElement.ValueKind);
        Assert.True(prioritizedDocument.RootElement.GetArrayLength() >= 2);

        var first = prioritizedDocument.RootElement[0];
        var second = prioritizedDocument.RootElement[1];

        Assert.True(first.GetProperty("priorityLevel").GetInt32() <= second.GetProperty("priorityLevel").GetInt32());
    }

    // ===== Approval-Gating Enforcement Tests =====

    [Fact]
    public async Task Allocation_DispatchBlockedUntilApproved()
    {
        var token = await LoginAndGetTokenAsync("admin1", "hash_admin1");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var suffix = Guid.NewGuid().ToString("N")[..8];

        var resourceResponse = await _client.PostAsJsonAsync("/api/ResourceLogistics/resources", new
        {
            resourceName = $"GateResource_{suffix}",
            resourceType = "Water",
            unit = "Box"
        });
        resourceResponse.EnsureSuccessStatusCode();
        using var resourceDocument = await JsonDocument.ParseAsync(await resourceResponse.Content.ReadAsStreamAsync());
        var resourceId = GetInt32(resourceDocument.RootElement, "resourceId");

        var warehouseResponse = await _client.PostAsJsonAsync("/api/ResourceLogistics/warehouses", new
        {
            warehouseName = $"GateWarehouse_{suffix}",
            street = "Street 1",
            area = "Area 1",
            city = "City 1",
            province = "Province 1",
            capacity = 100,
            managerId = 1
        });
        warehouseResponse.EnsureSuccessStatusCode();
        using var warehouseDocument = await JsonDocument.ParseAsync(await warehouseResponse.Content.ReadAsStreamAsync());
        var warehouseId = GetInt32(warehouseDocument.RootElement, "warehouseId");

        var inventoryResponse = await _client.PostAsJsonAsync("/api/ResourceLogistics/inventories", new
        {
            warehouseId,
            resourceId,
            quantity = 100m,
            minThreshold = 10m,
            maxCapacity = 500m
        });
        inventoryResponse.EnsureSuccessStatusCode();
        using var inventoryDocument = await JsonDocument.ParseAsync(await inventoryResponse.Content.ReadAsStreamAsync());
        var inventoryId = GetInt32(inventoryDocument.RootElement, "inventoryId");

        var eventResponse = await _client.PostAsJsonAsync("/api/DisasterEvent", new
        {
            eventName = $"GateEvent_{suffix}",
            disasterType = "Flood",
            startTime = DateTime.UtcNow.AddHours(-2),
            street = "Street E",
            area = "Area E",
            city = "City E",
            province = "Province E",
            status = "Active",
            affectedPopulation = 50
        });
        eventResponse.EnsureSuccessStatusCode();
        using var eventDocument = await JsonDocument.ParseAsync(await eventResponse.Content.ReadAsStreamAsync());
        var eventId = GetInt32(eventDocument.RootElement, "eventId");

        var allocationResponse = await _client.PostAsJsonAsync("/api/ResourceLogistics/allocations", new
        {
            inventoryId,
            eventId,
            requestedBy = 1,
            quantity = 10m,
            status = "Pending",
            requiresApproval = true,
            approvalRequestedBy = 1
        });
        allocationResponse.EnsureSuccessStatusCode();
        using var allocationDocument = await JsonDocument.ParseAsync(await allocationResponse.Content.ReadAsStreamAsync());
        var allocationId = GetInt32(allocationDocument.RootElement, "allocationId");

        var blockedDispatch = await _client.PatchAsJsonAsync($"/api/ResourceLogistics/allocations/{allocationId}/status", new { status = "Dispatched" });
        Assert.Equal(HttpStatusCode.BadRequest, blockedDispatch.StatusCode);

        var requestsResponse = await _client.GetAsync("/api/ApprovalWorkflow/requests?requestType=ResourceDistribution&status=Pending");
        requestsResponse.EnsureSuccessStatusCode();
        using var requestsDocument = await JsonDocument.ParseAsync(await requestsResponse.Content.ReadAsStreamAsync());
        var requestId = requestsDocument.RootElement
            .EnumerateArray()
            .First(item => item.GetProperty("allocationId").GetInt32() == allocationId)
            .GetProperty("requestId")
            .GetInt32();

        var approveResponse = await _client.PatchAsJsonAsync($"/api/ApprovalWorkflow/requests/{requestId}/decision", new
        {
            decision = "Approved",
            actionBy = 1
        });
        approveResponse.EnsureSuccessStatusCode();

        var allowedDispatch = await _client.PatchAsJsonAsync($"/api/ResourceLogistics/allocations/{allocationId}/status", new { status = "Dispatched" });
        Assert.Equal(HttpStatusCode.OK, allowedDispatch.StatusCode);
    }

    [Fact]
    public async Task Expense_PaidBlockedUntilApproved()
    {
        var token = await LoginAndGetTokenAsync("admin1", "hash_admin1");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var suffix = Guid.NewGuid().ToString("N")[..8];

        var eventResponse = await _client.PostAsJsonAsync("/api/DisasterEvent", new
        {
            eventName = $"ExpenseEvent_{suffix}",
            disasterType = "Flood",
            startTime = DateTime.UtcNow.AddHours(-3),
            street = "Street EX",
            area = "Area EX",
            city = "City EX",
            province = "Province EX",
            status = "Active",
            affectedPopulation = 75
        });
        eventResponse.EnsureSuccessStatusCode();
        using var eventDocument = await JsonDocument.ParseAsync(await eventResponse.Content.ReadAsStreamAsync());
        var eventId = GetInt32(eventDocument.RootElement, "eventId");

        var expenseResponse = await _client.PostAsJsonAsync("/api/DonationFinance/expenses", new
        {
            eventId,
            approvedBy = 1,
            category = "Operations",
            amount = 500m,
            paymentStatus = "Pending",
            requiresApproval = true,
            approvalRequestedBy = 1
        });
        expenseResponse.EnsureSuccessStatusCode();
        using var expenseDocument = await JsonDocument.ParseAsync(await expenseResponse.Content.ReadAsStreamAsync());
        var expenseId = GetInt32(expenseDocument.RootElement, "expenseId");

        var blockedPaid = await _client.PatchAsJsonAsync($"/api/DonationFinance/expenses/{expenseId}/payment-status", new { paymentStatus = "Paid" });
        Assert.Equal(HttpStatusCode.BadRequest, blockedPaid.StatusCode);

        var requestsResponse = await _client.GetAsync("/api/ApprovalWorkflow/requests?requestType=Financial&status=Pending");
        requestsResponse.EnsureSuccessStatusCode();
        using var requestsDocument = await JsonDocument.ParseAsync(await requestsResponse.Content.ReadAsStreamAsync());
        var requestId = requestsDocument.RootElement
            .EnumerateArray()
            .First(item => item.GetProperty("expenseId").GetInt32() == expenseId)
            .GetProperty("requestId")
            .GetInt32();

        var approveResponse = await _client.PatchAsJsonAsync($"/api/ApprovalWorkflow/requests/{requestId}/decision", new
        {
            decision = "Approved",
            actionBy = 1
        });
        approveResponse.EnsureSuccessStatusCode();

        var allowedPaid = await _client.PatchAsJsonAsync($"/api/DonationFinance/expenses/{expenseId}/payment-status", new { paymentStatus = "Paid" });
        Assert.Equal(HttpStatusCode.OK, allowedPaid.StatusCode);
    }

    [Fact]
    public async Task Assignment_StatusChangeBlockedUntilApproved()
    {
        var token = await LoginAndGetTokenAsync("admin1", "hash_admin1");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var suffix = Guid.NewGuid().ToString("N")[..8];

        var teamResponse = await _client.PostAsJsonAsync("/api/RescueTeam", new
        {
            teamName = $"GateTeam_{suffix}",
            teamType = "Rescue",
            street = "Street T",
            area = "Area T",
            city = "City T",
            province = "Province T",
            availabilityStatus = "Available",
            capacity = 8
        });
        teamResponse.EnsureSuccessStatusCode();
        using var teamDocument = await JsonDocument.ParseAsync(await teamResponse.Content.ReadAsStreamAsync());
        var teamId = GetInt32(teamDocument.RootElement, "teamId");

        var reportResponse = await _client.PostAsJsonAsync("/api/EmergencyReport", new
        {
            citizenId = 1,
            eventId = 1,
            street = "Assign Street",
            area = "Assign Area",
            city = "Assign City",
            province = "Assign Province",
            disasterType = "Flood",
            severityLevel = "High",
            status = "Pending",
            source = "Mobile"
        });
        reportResponse.EnsureSuccessStatusCode();
        using var reportDocument = await JsonDocument.ParseAsync(await reportResponse.Content.ReadAsStreamAsync());
        var reportId = GetInt32(reportDocument.RootElement, "reportId");

        var assignmentResponse = await _client.PostAsJsonAsync($"/api/RescueTeam/{teamId}/assignments", new
        {
            reportId,
            assignedBy = 1,
            status = "Assigned",
            requiresApproval = true,
            approvalRequestedBy = 1
        });
        assignmentResponse.EnsureSuccessStatusCode();
        using var assignmentDocument = await JsonDocument.ParseAsync(await assignmentResponse.Content.ReadAsStreamAsync());
        var assignmentId = GetInt32(assignmentDocument.RootElement, "assignmentId");

        var blockedStatus = await _client.PatchAsJsonAsync($"/api/RescueTeam/{teamId}/assignments/{assignmentId}/status", new { status = "EnRoute" });
        Assert.Equal(HttpStatusCode.BadRequest, blockedStatus.StatusCode);

        var requestsResponse = await _client.GetAsync("/api/ApprovalWorkflow/requests?requestType=RescueDeployment&status=Pending");
        requestsResponse.EnsureSuccessStatusCode();
        using var requestsDocument = await JsonDocument.ParseAsync(await requestsResponse.Content.ReadAsStreamAsync());
        var requestId = requestsDocument.RootElement
            .EnumerateArray()
            .First(item => item.GetProperty("assignmentId").GetInt32() == assignmentId)
            .GetProperty("requestId")
            .GetInt32();

        var approveResponse = await _client.PatchAsJsonAsync($"/api/ApprovalWorkflow/requests/{requestId}/decision", new
        {
            decision = "Approved",
            actionBy = 1
        });
        approveResponse.EnsureSuccessStatusCode();

        var allowedStatus = await _client.PatchAsJsonAsync($"/api/RescueTeam/{teamId}/assignments/{assignmentId}/status", new { status = "EnRoute" });
        Assert.Equal(HttpStatusCode.OK, allowedStatus.StatusCode);
    }

    [Fact]
    public async Task Allocation_CreateWithInvalidApprovalRequester_ReturnsNotFoundAndNoAllocationCreated()
    {
        var token = await LoginAndGetTokenAsync("admin1", "hash_admin1");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var suffix = Guid.NewGuid().ToString("N")[..8];

        var resourceResponse = await _client.PostAsJsonAsync("/api/ResourceLogistics/resources", new
        {
            resourceName = $"TxnResource_{suffix}",
            resourceType = "Water",
            unit = "Box"
        });
        resourceResponse.EnsureSuccessStatusCode();
        using var resourceDocument = await JsonDocument.ParseAsync(await resourceResponse.Content.ReadAsStreamAsync());
        var resourceId = GetInt32(resourceDocument.RootElement, "resourceId");

        var warehouseResponse = await _client.PostAsJsonAsync("/api/ResourceLogistics/warehouses", new
        {
            warehouseName = $"TxnWarehouse_{suffix}",
            street = "Street TX",
            area = "Area TX",
            city = "City TX",
            province = "Province TX",
            capacity = 100,
            managerId = 1
        });
        warehouseResponse.EnsureSuccessStatusCode();
        using var warehouseDocument = await JsonDocument.ParseAsync(await warehouseResponse.Content.ReadAsStreamAsync());
        var warehouseId = GetInt32(warehouseDocument.RootElement, "warehouseId");

        var inventoryResponse = await _client.PostAsJsonAsync("/api/ResourceLogistics/inventories", new
        {
            warehouseId,
            resourceId,
            quantity = 50m,
            minThreshold = 5m,
            maxCapacity = 100m
        });
        inventoryResponse.EnsureSuccessStatusCode();
        using var inventoryDocument = await JsonDocument.ParseAsync(await inventoryResponse.Content.ReadAsStreamAsync());
        var inventoryId = GetInt32(inventoryDocument.RootElement, "inventoryId");

        var eventResponse = await _client.PostAsJsonAsync("/api/DisasterEvent", new
        {
            eventName = $"TxnEvent_{suffix}",
            disasterType = "Flood",
            startTime = DateTime.UtcNow.AddHours(-2),
            street = "Street TX",
            area = "Area TX",
            city = "City TX",
            province = "Province TX",
            status = "Active",
            affectedPopulation = 25
        });
        eventResponse.EnsureSuccessStatusCode();
        using var eventDocument = await JsonDocument.ParseAsync(await eventResponse.Content.ReadAsStreamAsync());
        var eventId = GetInt32(eventDocument.RootElement, "eventId");

        var createAllocationResponse = await _client.PostAsJsonAsync("/api/ResourceLogistics/allocations", new
        {
            inventoryId,
            eventId,
            requestedBy = 1,
            quantity = 5m,
            status = "Pending",
            requiresApproval = true,
            approvalRequestedBy = 999999
        });

        Assert.Equal(HttpStatusCode.NotFound, createAllocationResponse.StatusCode);

        var allocationsResponse = await _client.GetAsync($"/api/ResourceLogistics/allocations?eventId={eventId}");
        allocationsResponse.EnsureSuccessStatusCode();
        using var allocationsDocument = await JsonDocument.ParseAsync(await allocationsResponse.Content.ReadAsStreamAsync());
        Assert.Equal(0, allocationsDocument.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task Expense_CreateWithMissingApprovalRequester_ReturnsBadRequestAndNoExpenseCreated()
    {
        var token = await LoginAndGetTokenAsync("admin1", "hash_admin1");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var eventResponse = await _client.PostAsJsonAsync("/api/DisasterEvent", new
        {
            eventName = $"TxnExpenseEvent_{suffix}",
            disasterType = "Flood",
            startTime = DateTime.UtcNow.AddHours(-1),
            street = "Street EX",
            area = "Area EX",
            city = "City EX",
            province = "Province EX",
            status = "Active",
            affectedPopulation = 40
        });
        eventResponse.EnsureSuccessStatusCode();
        using var eventDocument = await JsonDocument.ParseAsync(await eventResponse.Content.ReadAsStreamAsync());
        var eventId = GetInt32(eventDocument.RootElement, "eventId");

        var createExpenseResponse = await _client.PostAsJsonAsync("/api/DonationFinance/expenses", new
        {
            eventId,
            category = "Operations",
            amount = 120m,
            paymentStatus = "Pending",
            requiresApproval = true
        });

        Assert.Equal(HttpStatusCode.BadRequest, createExpenseResponse.StatusCode);

        var expensesResponse = await _client.GetAsync($"/api/DonationFinance/expenses?eventId={eventId}");
        expensesResponse.EnsureSuccessStatusCode();
        using var expensesDocument = await JsonDocument.ParseAsync(await expensesResponse.Content.ReadAsStreamAsync());
        Assert.Equal(0, expensesDocument.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task Assignment_CreateWithInvalidApprovalRequester_ReturnsNotFoundAndNoAssignmentCreated()
    {
        var token = await LoginAndGetTokenAsync("admin1", "hash_admin1");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var teamResponse = await _client.PostAsJsonAsync("/api/RescueTeam", new
        {
            teamName = $"TxnTeam_{suffix}",
            teamType = "Rescue",
            street = "Street AT",
            area = "Area AT",
            city = "City AT",
            province = "Province AT",
            availabilityStatus = "Available",
            capacity = 6
        });
        teamResponse.EnsureSuccessStatusCode();
        using var teamDocument = await JsonDocument.ParseAsync(await teamResponse.Content.ReadAsStreamAsync());
        var teamId = GetInt32(teamDocument.RootElement, "teamId");

        var reportResponse = await _client.PostAsJsonAsync("/api/EmergencyReport", new
        {
            citizenId = 1,
            eventId = 1,
            street = "TxnAssign Street",
            area = "TxnAssign Area",
            city = "TxnAssign City",
            province = "TxnAssign Province",
            disasterType = "Flood",
            severityLevel = "High",
            status = "Pending",
            source = "Mobile"
        });
        reportResponse.EnsureSuccessStatusCode();
        using var reportDocument = await JsonDocument.ParseAsync(await reportResponse.Content.ReadAsStreamAsync());
        var reportId = GetInt32(reportDocument.RootElement, "reportId");

        var createAssignmentResponse = await _client.PostAsJsonAsync($"/api/RescueTeam/{teamId}/assignments", new
        {
            reportId,
            assignedBy = 1,
            status = "Assigned",
            requiresApproval = true,
            approvalRequestedBy = 999999
        });

        Assert.Equal(HttpStatusCode.NotFound, createAssignmentResponse.StatusCode);

        var assignmentsResponse = await _client.GetAsync($"/api/RescueTeam/{teamId}/assignments");
        assignmentsResponse.EnsureSuccessStatusCode();
        using var assignmentsDocument = await JsonDocument.ParseAsync(await assignmentsResponse.Content.ReadAsStreamAsync());
        Assert.Equal(0, assignmentsDocument.RootElement.GetArrayLength());
    }

    [Fact]
    public async Task RescueTeam_Recommendations_SortsByProximityAndSeverity()
    {
        var token = await LoginAndGetTokenAsync("admin1", "hash_admin1");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var suffix = Guid.NewGuid().ToString("N")[..8];

        var nearTeamResponse = await _client.PostAsJsonAsync("/api/RescueTeam", new
        {
            teamName = $"RecoNear_{suffix}",
            teamType = "Rescue",
            street = "Near Street",
            area = "Near Area",
            city = "Near City",
            province = "Near Province",
            latitude = 24.8608,
            longitude = 67.0011,
            availabilityStatus = "Available",
            capacity = 10
        });
        nearTeamResponse.EnsureSuccessStatusCode();
        using var nearTeamDocument = await JsonDocument.ParseAsync(await nearTeamResponse.Content.ReadAsStreamAsync());
        var nearTeamId = GetInt32(nearTeamDocument.RootElement, "teamId");

        var farTeamResponse = await _client.PostAsJsonAsync("/api/RescueTeam", new
        {
            teamName = $"RecoFar_{suffix}",
            teamType = "Rescue",
            street = "Far Street",
            area = "Far Area",
            city = "Far City",
            province = "Far Province",
            latitude = 25.2048,
            longitude = 55.2708,
            availabilityStatus = "Available",
            capacity = 10
        });
        farTeamResponse.EnsureSuccessStatusCode();

        var reportResponse = await _client.PostAsJsonAsync("/api/EmergencyReport", new
        {
            citizenId = 1,
            eventId = 1,
            street = "Reco Street",
            area = "Reco Area",
            city = "Reco City",
            province = "Reco Province",
            latitude = 24.8600,
            longitude = 67.0000,
            disasterType = "Flood",
            severityLevel = "Critical",
            status = "Pending",
            source = "Mobile"
        });
        reportResponse.EnsureSuccessStatusCode();
        using var reportDocument = await JsonDocument.ParseAsync(await reportResponse.Content.ReadAsStreamAsync());
        var reportId = GetInt32(reportDocument.RootElement, "reportId");

        var recommendationsResponse = await _client.GetAsync($"/api/RescueTeam/recommendations?reportId={reportId}&limit=2");

        Assert.Equal(HttpStatusCode.OK, recommendationsResponse.StatusCode);
        using var recommendationsDocument = await JsonDocument.ParseAsync(await recommendationsResponse.Content.ReadAsStreamAsync());
        Assert.Equal(JsonValueKind.Array, recommendationsDocument.RootElement.ValueKind);
        Assert.Equal(2, recommendationsDocument.RootElement.GetArrayLength());

        var first = recommendationsDocument.RootElement[0];
        var second = recommendationsDocument.RootElement[1];

        Assert.Equal(nearTeamId, first.GetProperty("teamId").GetInt32());
        Assert.True(first.GetProperty("priorityScore").GetDouble() >= second.GetProperty("priorityScore").GetDouble());
        Assert.Equal("Rescue", first.GetProperty("preferredTeamType").GetString());
    }

    [Fact]
    public async Task RescueTeam_Recommendations_ReportWithoutCoordinates_ReturnsBadRequest()
    {
        var token = await LoginAndGetTokenAsync("admin1", "hash_admin1");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var reportResponse = await _client.PostAsJsonAsync("/api/EmergencyReport", new
        {
            citizenId = 1,
            eventId = 1,
            street = "NoGeo Street",
            area = "NoGeo Area",
            city = "NoGeo City",
            province = "NoGeo Province",
            disasterType = "Flood",
            severityLevel = "High",
            status = "Pending",
            source = "Mobile"
        });
        reportResponse.EnsureSuccessStatusCode();
        using var reportDocument = await JsonDocument.ParseAsync(await reportResponse.Content.ReadAsStreamAsync());
        var reportId = GetInt32(reportDocument.RootElement, "reportId");

        var response = await _client.GetAsync($"/api/RescueTeam/recommendations?reportId={reportId}&limit=5");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task DisasterEvent_UpdateWithStaleVersionToken_ReturnsConflict()
    {
        var token = await LoginAndGetTokenAsync("admin1", "hash_admin1");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var suffix = Guid.NewGuid().ToString("N")[..8];
        var createResponse = await _client.PostAsJsonAsync("/api/DisasterEvent", new
        {
            eventName = $"ConcurrencyEvent_{suffix}",
            disasterType = "Flood",
            startTime = DateTime.UtcNow.AddHours(-1),
            street = "Street C",
            area = "Area C",
            city = "City C",
            province = "Province C",
            status = "Active",
            affectedPopulation = 20
        });
        createResponse.EnsureSuccessStatusCode();
        using var createDocument = await JsonDocument.ParseAsync(await createResponse.Content.ReadAsStreamAsync());
        var eventId = GetInt32(createDocument.RootElement, "eventId");
        var tokenBefore = GetString(createDocument.RootElement, "versionToken");

        var firstUpdate = await _client.PutAsJsonAsync($"/api/DisasterEvent/{eventId}", new
        {
            eventName = $"ConcurrencyEvent_{suffix}",
            disasterType = "Flood",
            startTime = DateTime.UtcNow.AddHours(-1),
            street = "Street C",
            area = "Area C",
            city = "City C",
            province = "Province C",
            status = "Contained",
            affectedPopulation = 30,
            versionToken = tokenBefore
        });
        firstUpdate.EnsureSuccessStatusCode();

        var staleUpdate = await _client.PutAsJsonAsync($"/api/DisasterEvent/{eventId}", new
        {
            eventName = $"ConcurrencyEvent_{suffix}",
            disasterType = "Flood",
            startTime = DateTime.UtcNow.AddHours(-1),
            street = "Street C",
            area = "Area C",
            city = "City C",
            province = "Province C",
            status = "Resolved",
            affectedPopulation = 40,
            versionToken = tokenBefore
        });

        Assert.Equal(HttpStatusCode.Conflict, staleUpdate.StatusCode);
    }

    [Fact]
    public async Task ApprovalDecision_WithStaleVersionToken_ReturnsConflict()
    {
        var token = await LoginAndGetTokenAsync("admin1", "hash_admin1");
        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var createRequest = await _client.PostAsJsonAsync("/api/ApprovalWorkflow/requests", new
        {
            requestedBy = 1,
            requestType = "Financial",
            description = "Concurrency approval test",
            expenseId = (int?)null,
            allocationId = (int?)null,
            assignmentId = (int?)null
        });
        // requestType requires one target; create a valid target by creating expense first.
        if (createRequest.StatusCode == HttpStatusCode.BadRequest)
        {
            var eventResponse = await _client.PostAsJsonAsync("/api/DisasterEvent", new
            {
                eventName = "ApprovalConcurrencyEvent",
                disasterType = "Flood",
                startTime = DateTime.UtcNow.AddHours(-2),
                street = "Street A",
                area = "Area A",
                city = "City A",
                province = "Province A",
                status = "Active",
                affectedPopulation = 10
            });
            eventResponse.EnsureSuccessStatusCode();
            using var eventDoc = await JsonDocument.ParseAsync(await eventResponse.Content.ReadAsStreamAsync());
            var eventId = GetInt32(eventDoc.RootElement, "eventId");

            var expenseResponse = await _client.PostAsJsonAsync("/api/DonationFinance/expenses", new
            {
                eventId,
                approvedBy = 1,
                category = "Operations",
                amount = 10m,
                paymentStatus = "Pending"
            });
            expenseResponse.EnsureSuccessStatusCode();
            using var expenseDoc = await JsonDocument.ParseAsync(await expenseResponse.Content.ReadAsStreamAsync());
            var expenseId = GetInt32(expenseDoc.RootElement, "expenseId");

            createRequest = await _client.PostAsJsonAsync("/api/ApprovalWorkflow/requests", new
            {
                requestedBy = 1,
                requestType = "Financial",
                description = "Concurrency approval test",
                expenseId
            });
        }

        createRequest.EnsureSuccessStatusCode();
        using var createDocument = await JsonDocument.ParseAsync(await createRequest.Content.ReadAsStreamAsync());
        var requestId = GetInt32(createDocument.RootElement, "requestId");
        var versionToken = GetString(createDocument.RootElement, "versionToken");

        var firstDecision = await _client.PatchAsJsonAsync($"/api/ApprovalWorkflow/requests/{requestId}/decision", new
        {
            decision = "Approved",
            actionBy = 1,
            versionToken
        });
        firstDecision.EnsureSuccessStatusCode();

        var staleDecision = await _client.PatchAsJsonAsync($"/api/ApprovalWorkflow/requests/{requestId}/decision", new
        {
            decision = "Rejected",
            actionBy = 1,
            versionToken
        });

        Assert.Equal(HttpStatusCode.Conflict, staleDecision.StatusCode);
    }
}
