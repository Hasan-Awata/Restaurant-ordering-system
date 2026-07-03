using Microsoft.AspNetCore.Mvc;
using OrderingSystem.Application.DTOs;
using OrderingSystem.Application.DTOs.Paged;
using OrderingSystem.Application.Interfaces.MenueItem;
using OrderingSystem.Domain.Enums;
using System.Threading.Tasks;

namespace OrderingSystem.WebApi.Controllers
{
    [ApiController]
    [Route("api/menu-items")]
    public class MenuItemsController : ControllerBase
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
            if (!result.IsSuccess)
            {
                return result.ErrorType == enErrorType.Validation
                    ? BadRequest(result.ErrorMessage)
                    : StatusCode(500, result.ErrorMessage);
            }

           
            return CreatedAtAction(nameof(GetItemById), new { id = result.Value!.MenuItemId }, result.Value);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateMenuItem([FromBody] MenuRecords.UpdateMenuItemRequest request)
        {
            var result = await _menuItemCommandService.UpdateMenuItemAsync(request);
            if (!result.IsSuccess)
            {
                if (result.ErrorType == enErrorType.NotFound) return NotFound(result.ErrorMessage);
                if (result.ErrorType == enErrorType.Validation) return BadRequest(result.ErrorMessage);
                return StatusCode(500, result.ErrorMessage);
            }
            return Ok(result.Value);
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteMenuItem([FromBody] MenuRecords.DeleteMenuItemRequest request)
        {
            var result = await _menuItemCommandService.DeleteMenuItemAsync(request);
            if (!result.IsSuccess)
            {
                if (result.ErrorType == enErrorType.NotFound) return NotFound(result.ErrorMessage);
                if (result.ErrorType == enErrorType.Validation) return BadRequest(result.ErrorMessage);
                return StatusCode(500, result.ErrorMessage);
            }
            return NoContent();
        }

       

        [HttpGet("{id}")]
        public async Task<IActionResult> GetItemById(int id)
        {
            var result = await _menuItemQueryService.GetItemByIdAsync(id);
            if (!result.IsSuccess)
            {
                return result.ErrorType == enErrorType.NotFound ? NotFound(result.ErrorMessage) : StatusCode(500, result.ErrorMessage);
            }
            return Ok(result.Value);
        }

       
        [HttpGet("details/{id}")]
        public async Task<IActionResult> GetMenuItem(int id)
        {
            var result = await _menuItemQueryService.GetMenuItemAsync(id);
            if (!result.IsSuccess)
            {
                return result.ErrorType == enErrorType.NotFound ? NotFound(result.ErrorMessage) : StatusCode(500, result.ErrorMessage);
            }
            return Ok(result.Value);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllMenuItems([FromQuery] PageDTO page)
        {
            var result = await _menuItemQueryService.GetAllMenuItemsAsync(page);
            if (!result.IsSuccess)
            {
                return StatusCode(500, result.ErrorMessage);
            }
            return Ok(result.Value);
        }

        [HttpGet("category/{categoryId}")]
        public async Task<IActionResult> GetAllMenuItemsByCategory(int categoryId, [FromQuery] PageDTO page)
        {
            var result = await _menuItemQueryService.GetAllMenuItemsByCategoryAsync(categoryId, page);
            if (!result.IsSuccess)
            {
                if (result.ErrorType == enErrorType.NotFound) return NotFound(result.ErrorMessage);
                return StatusCode(500, result.ErrorMessage);
            }
            return Ok(result.Value);
        }

        [HttpGet("available")]
        public async Task<IActionResult> GetAllAvailableMenuItems([FromQuery] PageDTO page)
        {
            var result = await _menuItemQueryService.GetAllAvailableMenuItemsAsync(page);
            if (!result.IsSuccess)
            {
                return StatusCode(500, result.ErrorMessage);
            }
            return Ok(result.Value);
        }

        
    }
}