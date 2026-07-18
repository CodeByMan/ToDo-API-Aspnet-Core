using Microsoft.EntityFrameworkCore;
using ToDoAPI_ASPNET.Models.Dtos.ToDoItems;
using ToDoAPI_ASPNET.Models.Entities;
using ToDoAPI_ASPNET.Models.Utils;
using ToDoAPI_ASPNET.Repositories.ToDoItems;
using ToDoAPI_ASPNET.Services.ToDoItems;

namespace ToDoAPI_ASPNET.Tests;

public class ToDoItemServiceTests
{
    [Fact]
    public async Task CrudOperations_WorkForOwningUser()
    {
        using var database = new TestDatabase();
        var user = await AddUserAsync(database, "owner@example.com", "owner");
        var service = CreateService(database);

        var created = await service.CreateItemAsync(user.Id, new ToDoItemCreateDto
        {
            Title = "Prepare portfolio",
            Description = "Review the API repository",
            DueDate = DateTime.UtcNow.AddDays(2)
        });

        Assert.Equal("success", created.Status);
        Assert.NotNull(created.Data);

        var entity = await database.Context.ToDoItems.SingleAsync();
        Assert.Equal(user.Id, entity.UserId);
        Assert.Equal(entity.Id, created.Data.Id);
        Assert.True(created.Data.Id > 0);

        var retrieved = await service.GetToDoItemByIdAsync(user.Id, entity.Id);
        Assert.Equal("success", retrieved.Status);
        Assert.Equal(entity.Id, retrieved.Data!.Id);

        var updated = await service.UpdateToDoItemAsync(user.Id, entity.Id, new ToDoItemUpdateDto
        {
            Title = "Prepare ASP.NET portfolio",
            Description = "Review and document the API repository",
            IsComplete = true,
            DueDate = DateTime.UtcNow.AddDays(3)
        });

        Assert.Equal("success", updated.Status);
        Assert.Equal(entity.Id, updated.Data!.Id);
        Assert.True(updated.Data.IsComplete);

        var deleted = await service.DeleteToDoItemAsync(user.Id, entity.Id);
        Assert.Equal("success", deleted.Status);
        Assert.Empty(database.Context.ToDoItems);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsNotFoundForAnotherUsersItem()
    {
        using var database = new TestDatabase();
        var owner = await AddUserAsync(database, "owner@example.com", "owner");
        var otherUser = await AddUserAsync(database, "other@example.com", "other");
        var item = await AddItemAsync(database, owner.Id, "Private task");
        var service = CreateService(database);

        var response = await service.GetToDoItemByIdAsync(otherUser.Id, item.Id);

        Assert.Equal("error", response.Status);
        Assert.Equal(Error.ErrorType.NotFound, response.ErrorType);
    }

    [Fact]
    public async Task BatchStatusUpdate_RejectsItemsOwnedByAnotherUserWithoutPartialUpdate()
    {
        using var database = new TestDatabase();
        var owner = await AddUserAsync(database, "owner@example.com", "owner");
        var otherUser = await AddUserAsync(database, "other@example.com", "other");
        var ownerItem = await AddItemAsync(database, owner.Id, "Owner task");
        var otherItem = await AddItemAsync(database, otherUser.Id, "Other task");
        var service = CreateService(database);

        var response = await service.UpdateToDoItemsStatusAsync(owner.Id, new ToDoItemUpdateStatusDto
        {
            ItemIds = new List<int> { ownerItem.Id, otherItem.Id },
            NewStatus = true
        });

        Assert.Equal("error", response.Status);
        Assert.Equal(Error.ErrorType.NotFound, response.ErrorType);
        Assert.False((await database.Context.ToDoItems.FindAsync(ownerItem.Id))!.IsComplete);
        Assert.False((await database.Context.ToDoItems.FindAsync(otherItem.Id))!.IsComplete);
    }

    [Fact]
    public async Task BatchDelete_RejectsEmptyRequest()
    {
        using var database = new TestDatabase();
        var service = CreateService(database);

        var response = await service.DeleteToDoItemsAsync(1, new List<int>());

        Assert.Equal("error", response.Status);
        Assert.Equal(Error.ErrorType.ValidationError, response.ErrorType);
    }

    private static ToDoItemService CreateService(TestDatabase database)
    {
        return new ToDoItemService(
            new ToDoItemRepository(database.Context),
            TestSupport.CreateMapper()
        );
    }

    private static async Task<User> AddUserAsync(TestDatabase database, string email, string username)
    {
        var user = new User
        {
            Email = email,
            Username = username,
            FirstName = "Test",
            LastName = "User",
            Password = "not-used-in-service-tests"
        };

        database.Context.Users.Add(user);
        await database.Context.SaveChangesAsync();
        return user;
    }

    private static async Task<ToDoItem> AddItemAsync(TestDatabase database, int userId, string title)
    {
        var item = new ToDoItem
        {
            UserId = userId,
            Title = title,
            Description = "Test description",
            DueDate = DateTime.UtcNow.AddDays(1)
        };

        database.Context.ToDoItems.Add(item);
        await database.Context.SaveChangesAsync();
        return item;
    }
}
