using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderingSystem.Application.DTOs;
using OrderingSystem.Application.DTOs.Paged;
using OrderingSystem.Application.Interfaces.Category;
using OrderingSystem.WebApi.Controllers.Base;
using System.Threading.Tasks;

namespace OrderingSystem.WebApi.Controllers
{
    [ApiController]
    [Route("api/categories")]
    public class CategoriesController : BaseController
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

        [Authorize(Roles = "Admin")]
        [HttpPost]
        public async Task<IActionResult> AddCategory([FromBody] CategoriesRecords.AddCategoryRequest request)
        {
            var result = await _categoryCommandService.AddCategoryAsync(request);

            return HandleCreatedResult(
                result,
                nameof(GetCategoryById),
                new { id = result.Value?.CategoryId }
            );
        }

        [Authorize(Roles = "Admin")]
        [HttpPut]
        public async Task<IActionResult> UpdateCategory([FromBody] CategoriesRecords.UpdateCategoryRequest request)
        {
            var result = await _categoryCommandService.UpdateCategoryAsync(request);
            return HandleResult(result);
        }

        [Authorize(Roles = "Admin")]
        [HttpDelete]
        public async Task<IActionResult> DeleteCategory([FromBody] CategoriesRecords.DeleteCategoryRequest request)
        {
            var result = await _categoryCommandService.DeleteCategoryAsync(request);
            return HandleResult(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetCategoryById(int id)
        {
            var result = await _categoryQueryService.GetCategoryByIdAsync(id);
            return HandleResult(result);
        }

        [HttpGet("details/{id}")]
        public async Task<IActionResult> GetCategory(int id)
        {
            var result = await _categoryQueryService.GetCategoryAsync(id);
            return HandleResult(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllCategories([FromQuery] PageDTO page)
        {
            var result = await _categoryQueryService.GetAllCategoriesAsync(page);
            return HandleResult(result);
        }

        [HttpGet("available")]
        public async Task<IActionResult> GetAllAvailableCategories([FromQuery] PageDTO page)
        {
            var result = await _categoryQueryService.GetAllAvailableCategoriesAsync(page);
            return HandleResult(result);
        }
    }
}