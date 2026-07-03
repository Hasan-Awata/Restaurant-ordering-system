using OrderingSystem.Application.DTOs;
using OrderingSystem.Application.DTOs.Paged;
using OrderingSystem.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OrderingSystem.Application.Interfaces.Category
{
    public interface ICategoryQuery
    {
        public Task<Result<CategoriesRecords.CategoryResponse>> GetCategoryAsync(int categoryId);
        public Task<Result<PagedResponse<CategoriesRecords.CategoryResponse>>> GetAllCategoriesAsync(PageDTO page);
        public Task<Result<PagedResponse<CategoriesRecords.CategoryResponse>>> GetAllAvailableCategoriesAsync(PageDTO page);
        public Task<Result<CategoriesRecords.CategoryResponse>> GetCategoryByIdAsync(int categoryId);
    }
}