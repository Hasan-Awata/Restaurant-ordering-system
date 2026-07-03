using Microsoft.AspNetCore.Mvc;
using OrderingSystem.Application.DTOs;
using OrderingSystem.Application.DTOs.Paged;
using OrderingSystem.Application.Interfaces.Category;
using OrderingSystem.Domain.Enums;
using System.Threading.Tasks;

namespace OrderingSystem.WebApi.Controllers
{
    [ApiController]
    [Route("api/categories")]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoryCommandService _categoryCommandService;
        private readonly ICategoryQuery _categoryQueryService;

        public CategoriesController(
            ICategoryCommandService categoryCommandService,
            ICategoryQuery categoryQueryService)
        {
            _categoryCommandService = categoryCommandService;
            _categoryQueryService = categoryQueryService;
        }

        [HttpPost]
        public async Task<IActionResult> AddCategory([FromBody] CategoriesRecords.AddCategoryRequest request)
        {
            var result = await _categoryCommandService.AddCategoryAsync(request);
            if (!result.IsSuccess)
            {
                return result.ErrorType == enErrorType.Validation
                    ? BadRequest(result.ErrorMessage)
                    : StatusCode(500, result.ErrorMessage);
            }

             return CreatedAtAction(nameof(GetCategoryById), new { id = result.Value!.CategoryId }, result.Value);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateCategory([FromBody] CategoriesRecords.UpdateCategoryRequest request)
        {
            var result = await _categoryCommandService.UpdateCategoryAsync(request);
            if (!result.IsSuccess)
            {
                if (result.ErrorType == enErrorType.NotFound) return NotFound(result.ErrorMessage);
                if (result.ErrorType == enErrorType.Validation) return BadRequest(result.ErrorMessage);
                return StatusCode(500, result.ErrorMessage);
            }
            return Ok(result.Value);
        }

        [HttpDelete]
        public async Task<IActionResult> DeleteCategory([FromBody] CategoriesRecords.DeleteCategoryRequest request)
        {
            var result = await _categoryCommandService.DeleteCategoryAsync(request);
            if (!result.IsSuccess)
            {
                if (result.ErrorType == enErrorType.NotFound) return NotFound(result.ErrorMessage);
                if (result.ErrorType == enErrorType.Validation) return BadRequest(result.ErrorMessage);
                return StatusCode(500, result.ErrorMessage);
            }
            return NoContent();
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCategoryById(int id)
        {
            var result = await _categoryQueryService.GetCategoryByIdAsync(id);
            if (!result.IsSuccess)
            {
                return result.ErrorType == enErrorType.NotFound ? NotFound(result.ErrorMessage) : StatusCode(500, result.ErrorMessage);
            }
            return Ok(result.Value);
        }

        [HttpGet("details/{id}")]
        public async Task<IActionResult> GetCategory(int id)
        {
            var result = await _categoryQueryService.GetCategoryAsync(id);
            if (!result.IsSuccess)
            {
                return result.ErrorType == enErrorType.NotFound ? NotFound(result.ErrorMessage) : StatusCode(500, result.ErrorMessage);
            }
            return Ok(result.Value);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllCategories([FromQuery] PageDTO page)
        {
            var result = await _categoryQueryService.GetAllCategoriesAsync(page);
            if (!result.IsSuccess)
            {
                return StatusCode(500, result.ErrorMessage);
            }
            return Ok(result.Value);
        }

        [HttpGet("available")]
        public async Task<IActionResult> GetAllAvailableCategories([FromQuery] PageDTO page)
        {
            var result = await _categoryQueryService.GetAllAvailableCategoriesAsync(page);
            if (!result.IsSuccess)
            {
                return StatusCode(500, result.ErrorMessage);
            }
            return Ok(result.Value);
        }
    }
}