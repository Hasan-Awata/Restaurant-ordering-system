using OrderingSystem.Application.DTOs;
using OrderingSystem.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OrderingSystem.Application.Interfaces.Category
{
    public interface ICategoryCommandService
    {
        public Task<Result<CategoriesRecords.CategoryResponse>> AddCategoryAsync(CategoriesRecords.AddCategoryRequest request);
        public Task<Result<CategoriesRecords.CategoryResponse>> UpdateCategoryAsync(CategoriesRecords.UpdateCategoryRequest request);
        public Task<Result<bool>> DeleteCategoryAsync(CategoriesRecords.DeleteCategoryRequest request);
    }
}