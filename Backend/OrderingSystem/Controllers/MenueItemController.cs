using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderingSystem.Application.DTOs;
using OrderingSystem.Application.DTOs.Paged;
using OrderingSystem.Application.Interfaces.MenueItem;
using OrderingSystem.WebApi.Controllers.Base;

namespace OrderingSystem.WebApi.Controllers
{
    [ApiController]
    [Route("api/menu-items")]
    public class MenuItemsController : BaseController
    {
        private readonly IWebHostEnvironment _env; 
        private readonly IMenueItemCommandService _menuItemCommandService;
        private readonly IMenueItemQuery _menuItemQueryService;

        public MenuItemsController(
            IMenueItemCommandService menuItemCommandService,
            IMenueItemQuery menuItemQueryService,
            IWebHostEnvironment env) 
        {
            _menuItemCommandService = menuItemCommandService;
            _menuItemQueryService = menuItemQueryService;
            _env = env;
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpPost]
        public async Task<IActionResult> AddMenuItem([FromBody] MenuRecords.AddMenuItemRequest request)
        {
            var result = await _menuItemCommandService.AddMenuItemAsync(request);

            return HandleCreatedResult(
                result,
                nameof(GetItemById),
                new { id = result.Value?.MenuItemId }
            );
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpPut]
        public async Task<IActionResult> UpdateMenuItem([FromBody] MenuRecords.UpdateMenuItemRequest request)
        {
            var result = await _menuItemCommandService.UpdateMenuItemAsync(request);
            return HandleResult(result);
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpDelete]
        public async Task<IActionResult> DeleteMenuItem([FromBody] MenuRecords.DeleteMenuItemRequest request)
        {
            var result = await _menuItemCommandService.DeleteMenuItemAsync(request);
            return HandleResult(result);
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpPost("upload")]
        public async Task<IActionResult> UploadImage(IFormFile file)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "No file uploaded." });

            // Fallback to current directory if WebRootPath is null in certain environments
            var webRoot = _env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var uploadsFolder = Path.Combine(webRoot, "images");

            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(file.FileName);
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Returns a relative path that the frontend can use in the AddMenuItemRequest
            return Ok(new { imageUrl = $"/images/{uniqueFileName}" });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetItemById(int id)
        {
            var result = await _menuItemQueryService.GetItemByIdAsync(id);
            return HandleResult(result);
        }
        
        [HttpGet("details/{id}")]
        public async Task<IActionResult> GetMenuItem(int id)
        {
            var result = await _menuItemQueryService.GetMenuItemAsync(id);
            return HandleResult(result);
        }
       
        [HttpGet]
        public async Task<IActionResult> GetAllMenuItems([FromQuery] PageDTO page)
        {
            var result = await _menuItemQueryService.GetAllMenuItemsAsync(page);
            return HandleResult(result);
        }
        
        [HttpGet("category/{categoryId}")]
        public async Task<IActionResult> GetAllMenuItemsByCategory(int categoryId, [FromQuery] PageDTO page)
        {
            var result = await _menuItemQueryService.GetAllMenuItemsByCategoryAsync(categoryId, page);
            return HandleResult(result);
        }
        
        [HttpGet("available")]
        public async Task<IActionResult> GetAllAvailableMenuItems([FromQuery] PageDTO page)
        {
            var result = await _menuItemQueryService.GetAllAvailableMenuItemsAsync(page);
            return HandleResult(result);
        }

        [HttpGet("search")]
        public async Task<IActionResult> SearchMenuItems([FromQuery] string query, [FromQuery] PageDTO page)
        {
            var result = await _menuItemQueryService.SearchMenuItemsAsync(query, page);
            return HandleResult(result);
        }
    }
}