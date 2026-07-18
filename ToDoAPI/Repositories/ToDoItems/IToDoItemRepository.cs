using ToDoAPI_ASPNET.Models.Entities;

namespace ToDoAPI_ASPNET.Repositories.ToDoItems;

public interface IToDoItemRepository
{
    Task<ToDoItem> CreateAsync(ToDoItem toDoItem);
    Task<ToDoItem?> GetByIdAsync(int id);
    Task<IReadOnlyList<ToDoItem>> GetByIdsAsync(IEnumerable<int> ids);
    Task<IEnumerable<ToDoItem>> GetAllAsync(int userId);
    Task<bool> UpdateAsync(ToDoItem toDoItem);
    Task<bool> UpdateRangeAsync(IEnumerable<ToDoItem> toDoItems);
    Task<bool> DeleteAsync(ToDoItem toDoItem);
    Task<bool> DeleteRangeAsync(IEnumerable<ToDoItem> toDoItems);
}
