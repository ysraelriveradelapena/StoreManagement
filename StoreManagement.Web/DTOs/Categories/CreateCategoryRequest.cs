using System.ComponentModel.DataAnnotations;

namespace StoreManagement.Web.DTOs.Categories;

public class CreateCategoryRequest
{
    [Required(ErrorMessage = "Category name is required.")]
    [MaxLength(100, ErrorMessage = "Category name cannot exceed 100 characters.")]
    public string Name { get; set; } = string.Empty;
}