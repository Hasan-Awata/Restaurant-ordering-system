using OrderingSystem.Application.DTOs;
using OrderingSystem.Application.Interfaces.Category;
using OrderingSystem.Application.Mappers; 
using OrderingSystem.Domain.Common;
using OrderingSystem.Domain.Enums;
using System;
using System.Threading.Tasks;

namespace OrderingSystem.Application.Services
{
    public class CategoryCommandService : ICategoryCommandService
    {
         private Result<CategoriesRecords.CategoryResponse> ValidateCategoryRequest(string nameEn, string nameAr)
        {
            if (string.IsNullOrEmpty(nameAr) || string.IsNullOrEmpty(nameEn))
            {
                return Result<CategoriesRecords.CategoryResponse>.Failure("NameAr and NameEn cannot be null or empty.");
            }

            return Result<CategoriesRecords.CategoryResponse>.Success(null);
        }

        private readonly ICategoryRepository _categoryRepository;

        public CategoryCommandService(ICategoryRepository categoryRepository)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<Result<CategoriesRecords.CategoryResponse>> AddCategoryAsync(CategoriesRecords.AddCategoryRequest request)
        {
            if (request == null)
            {
                return Result<CategoriesRecords     .CategoryResponse>.Failure("Request cannot be null.");
            }

            var validationResult = ValidateCategoryRequest(request.NameEn, request.NameAr);

            if (!validationResult.IsSuccess)
            {
                return Result<CategoriesRecords.CategoryResponse>.Failure(validationResult.ErrorMessage);
            }

            if (await _categoryRepository.CategoryIsExistsByNameAsync(request.NameEn, request.NameAr))
            {
                return Result<CategoriesRecords.CategoryResponse>.Failure(
                   $"A category with the name '{request.NameAr}' or '{request.NameEn}' already exists.",
                   enErrorType.Validation
               );
            }
           var category = CategoryMappers.ToEntity(request);

            if (category == null)
            {
                return Result<CategoriesRecords.CategoryResponse>.Failure("Failed to map request to entity.");
            }

            await _categoryRepository.AddCategoryAsync(category);

            var response = category.ToResponse();
            return Result<CategoriesRecords.CategoryResponse>.Success(response);
        }

        public async Task<Result<CategoriesRecords.CategoryResponse>> UpdateCategoryAsync(  CategoriesRecords.UpdateCategoryRequest request)
        {
            if (request == null)
            {
                return Result<CategoriesRecords.CategoryResponse>.Failure("Request cannot be null.");
            }

            if (ValidateCategoryRequest(request.NameEn, request.NameAr).IsSuccess == false)
            {
                return Result<CategoriesRecords.CategoryResponse>.Failure("Invalid request data.");
            }

            if (request.CategoryId <= 0)
            {
                return Result<CategoriesRecords.CategoryResponse>.Failure("Invalid CategoryId.");
            }

            var existingCategory = await _categoryRepository.GetCategoryByIdAsync(request.CategoryId);
            if (existingCategory == null)
            {
                return Result<CategoriesRecords.CategoryResponse>.Failure($"Category with ID {request.CategoryId} not found.", enErrorType.NotFound);
            }

            existingCategory.NameAr = request.NameAr;
            existingCategory.NameEn = request.NameEn;
            existingCategory.IsAvailable = request.IsAvailable;

            await _categoryRepository.UpdateCategoryAsync(existingCategory);

            var response = existingCategory.ToResponse();
            return Result<CategoriesRecords.CategoryResponse>.Success(response);
        }

        public async Task<Result<bool>> DeleteCategoryAsync(CategoriesRecords.DeleteCategoryRequest request)
        {
            if (request == null)
            {
                return Result<bool>.Failure("Request cannot be null.");
            }

            var existingCategory = await _categoryRepository.GetCategoryByIdAsync(request.CategoryId);
            if (existingCategory == null)
            {
                return Result<bool>.Failure($"Category with ID {request.CategoryId} not found.", enErrorType.NotFound);
            }

            await _categoryRepository.DeleteCategoryAsync(existingCategory);
            return Result<bool>.Success(true);
        }
    }
}