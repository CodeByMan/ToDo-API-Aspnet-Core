using System.Security.Cryptography;
using AutoMapper;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using ToDoAPI_ASPNET.Data;
using ToDoAPI_ASPNET.MappingProfiles;
using ToDoAPI_ASPNET.Models.Config;

namespace ToDoAPI_ASPNET.Tests;

internal sealed class TestDatabase : IDisposable
{
    private readonly SqliteConnection connection;

    public TestDatabase()
    {
        connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<DataContext>()
            .UseSqlite(connection)
            .Options;

        Context = new DataContext(options);
        Context.Database.EnsureCreated();
    }

    public DataContext Context { get; }

    public void Dispose()
    {
        Context.Dispose();
        connection.Dispose();
    }
}

internal static class TestSupport
{
    public static IMapper CreateMapper()
    {
        var configuration = new MapperConfiguration(config =>
        {
            config.AddProfile<AuthProfile>();
            config.AddProfile<ToDoItemProfile>();
            config.AddProfile<UserProfile>();
        });

        return configuration.CreateMapper();
    }

    public static JWTSettings CreateJwtSettings()
    {
        return new JWTSettings
        {
            Key = Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(32)),
            Issuer = "ToDoAPI.Tests",
            Audience = "ToDoAPI.Tests.Client",
            AccessTokenExpiry = 1,
            RefreshTokenExpiry = 30
        };
    }
}
