using Microsoft.AspNetCore.Mvc;
using OrderingSystem.Application.DTOs;
using OrderingSystem.Application.DTOs.Paged;
using OrderingSystem.Application.Interfaces.MenueItem;
using OrderingSystem.WebApi.Controllers.Base;
using System.Threading.Tasks;

namespace OrderingSystem.WebApi.Controllers
{
    [ApiController]
    [Route("api/menu-items")]
    public class MenuItemsController : BaseController
    {
        private readonly IMenueItemCommandService _menuItemCommandService;
        private readonly IMenueItemQuery _menuItemQueryService;

        public MenuItemsController(
            IMenueItemCommandService menuItemCommandService,
            IMenueItemQuery menuItemQueryService)
        {
            _menuItemCommandService = menuItemCommandService;
            _menuItemQueryService = menuItemQueryService;
        }

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

        [HttpPut]
        public async Task<IActionResult> UpdateMenuItem([FromBody] MenuRecords.UpdateMenuItemRequest request)
        {
            var result = await _menuItemCommandService.UpdateMenuItemAsync(request);
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