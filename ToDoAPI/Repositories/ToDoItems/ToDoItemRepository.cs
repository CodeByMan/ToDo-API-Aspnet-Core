using ToDoAPI_ASPNET.Data;
using Microsoft.EntityFrameworkCore;
using ToDoAPI_ASPNET.Models.Entities;

namespace ToDoAPI_ASPNET.Repositories.ToDoItems;

public class ToDoItemRepository(
    DataContext context
) : IToDoItemRepository
{
    public async Task<ToDoItem> CreateAsync(ToDoItem toDoItem)
    {
        await context.AddAsync(toDoItem);
        await context.SaveChangesAsync();

        return toDoItem;
    }

    public async Task<ToDoItem?> GetByIdAsync(int id)
    {
        return await context.ToDoItems
            .FirstOrDefaultAsync(t => t.Id == id);
    }

    public async Task<IReadOnlyList<ToDoItem>> GetByIdsAsync(IEnumerable<int> ids)
    {
        var itemIds = ids.Distinct().ToList();

        return await context.ToDoItems
            .Where(t => itemIds.Contains(t.Id))
            .ToListAsync();
    }

    public async Task<IEnumerable<ToDoItem>> GetAllAsync(int userId)
    {
        return await context.ToDoItems
            .Where(t => t.UserId == userId)
            .ToListAsync();
    }

    public async Task<bool> UpdateAsync(ToDoItem toDoItem)
    {
        context.ToDoItems.Update(toDoItem);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateRangeAsync(IEnumerable<ToDoItem> toDoItems)
    {
        var items = toDoItems.ToList();
        if (items.Count == 0)
            return false;

        context.ToDoItems.UpdateRange(items);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteAsync(ToDoItem toDoItem)
    {
        context.ToDoItems.Remove(toDoItem);
        await context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> DeleteRangeAsync(IEnumerable<ToDoItem> toDoItems)
    {
        var items = toDoItems.ToList();
        if (items.Count == 0)
            return false;

        context.ToDoItems.RemoveRange(items);
        await context.SaveChangesAsync();
        return true;
    }
}
