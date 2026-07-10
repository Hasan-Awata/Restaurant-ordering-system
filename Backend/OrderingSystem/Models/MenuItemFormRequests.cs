using Microsoft.AspNetCore.Http;

namespace OrderingSystem.WebApi.Models
{
    /// <summary>
    /// DTO used by the WebApi layer to accept multi-part form data during menu item creation.
    /// </summary>
    public class AddMenuItemFormRequest
    {
        public int CategoryId { get; set; }
        public required string NameAr { get; set; }
        public  required string NameEn { get; set; }
        public required string Description { get; set; }
        public decimal Price { get; set; }
        public bool IsAvailable { get; set; }

        // This accepts the raw binary file from the HTTP request
        public required IFormFile ImageFile { get; set; }
    }

    /// <summary>
    /// DTO used by the WebApi layer to accept multi-part form data during menu item updates.
    /// </summary>
    public class UpdateMenuItemFormRequest
    {
        public int MenuItemId { get; set; }
        public int CategoryId { get; set; }
        public required string NameAr { get; set; }
        public required string NameEn { get; set; }
        public required string Description { get; set; }
        public decimal Price { get; set; }
        public bool IsAvailable { get; set; }

        public IFormFile? ImageFile { get; set; }

        public string? ExistingImageUrl { get; set; }
    }
}