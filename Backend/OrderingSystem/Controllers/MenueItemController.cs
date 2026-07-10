using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using OrderingSystem.Application.DTOs;
using OrderingSystem.Application.DTOs.Paged;
using OrderingSystem.Application.Interfaces.MenueItem;
using OrderingSystem.WebApi.Controllers.Base;
using OrderingSystem.WebApi.Models;
using System;
using System.IO;
using System.Threading.Tasks;

namespace OrderingSystem.WebApi.Controllers
{
    [ApiController]
    [Route("api/menu-items")]
    public class MenuItemsController : BaseController
    {
        private readonly IMenueItemCommandService _menuItemCommandService;
        private readonly IMenueItemQuery _menuItemQueryService;

        // Injecting the host environment to get the physical path of wwwroot
        private readonly IWebHostEnvironment _env;

        public MenuItemsController(
            IMenueItemCommandService menuItemCommandService,
            IMenueItemQuery menuItemQueryService,
            IWebHostEnvironment env)
        {
            _menuItemCommandService = menuItemCommandService;
            _menuItemQueryService = menuItemQueryService;
            _env = env;
        }

        [HttpPost]
        // Using [FromForm] instead of [FromBody] to handle multipart/form-data requests
        public async Task<IActionResult> AddMenuItem([FromForm] AddMenuItemFormRequest formRequest)
        {
            if (formRequest.ImageFile == null || formRequest.ImageFile.Length == 0)
            {
                return BadRequest("Please upload an image for the menu item.");
            }

            // Define the destination folder inside wwwroot
            string uploadsFolder = Path.Combine(_env.WebRootPath, "images", "menu");
            if (!Directory.Exists(uploadsFolder))
            {
                Directory.CreateDirectory(uploadsFolder);
            }

            // Generate a unique filename using GUID to prevent naming conflicts
            string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(formRequest.ImageFile.FileName);
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            // Save the uploaded file to the server disk
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await formRequest.ImageFile.CopyToAsync(stream);
            }

            // Relative URL path to be stored in the database
            string imageUrl = $"/images/menu/{uniqueFileName}";

            // Map the form request data to the core Application DTO record
            var appRequest = new MenuRecords.AddMenuItemRequest(
                formRequest.CategoryId,
                formRequest.NameAr,
                formRequest.NameEn,
                formRequest.Description,
                formRequest.Price,
                imageUrl,
                formRequest.IsAvailable
            );

            var result = await _menuItemCommandService.AddMenuItemAsync(appRequest);

            // Forwarding the result to the inherited BaseController handler
            return HandleCreatedResult(
                result,
                nameof(GetItemById),
                new { id = result.Value?.MenuItemId }
            );
        }

        [HttpPut]
        // Using [FromForm] for updates to allow optional image file modifications
        public async Task<IActionResult> UpdateMenuItem([FromForm] UpdateMenuItemFormRequest formRequest)
        {
            // Fallback to the existing image URL if no new image is provided
            string finalImageUrl = formRequest.ExistingImageUrl ?? string.Empty;

            // Process and save the new image if it was uploaded
            if (formRequest.ImageFile != null && formRequest.ImageFile.Length > 0)
            {
                string uploadsFolder = Path.Combine(_env.WebRootPath, "images", "menu");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = Guid.NewGuid().ToString() + Path.GetExtension(formRequest.ImageFile.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await formRequest.ImageFile.CopyToAsync(stream);
                }

                finalImageUrl = $"/images/menu/{uniqueFileName}";
            }

             var appRequest = new MenuRecords.UpdateMenuItemRequest(
                formRequest.MenuItemId,
                formRequest.CategoryId,
                formRequest.NameAr,
                formRequest.NameEn,
                formRequest.Description,
                formRequest.Price,
                finalImageUrl,
                formRequest.IsAvailable
            );

            var result = await _menuItemCommandService.UpdateMenuItemAsync(appRequest);
            return HandleResult(result);
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteMenuItem([FromBody] MenuRecords.DeleteMenuItemRequest request)
        {
            var result = await _menuItemCommandService.DeleteMenuItemAsync(request);
            return HandleResult(result);
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
    }
}