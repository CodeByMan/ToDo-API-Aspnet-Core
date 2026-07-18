using System.ComponentModel.DataAnnotations;

namespace ToDoAPI_ASPNET.Models.Dtos.ToDoItems;

public class ToDoItemUpdateDto
{
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Title must be between 2 and 100 characters.")]
    public string? Title { get; set; } = string.Empty;

    [StringLength(100, MinimumLength = 2, ErrorMessage = "Description must be between 2 and 100 characters.")]
    public string? Description { get; set; } = string.Empty;

    public bool? IsComplete { get; set; }
    public DateTime? DueDate { get; set; }
}
