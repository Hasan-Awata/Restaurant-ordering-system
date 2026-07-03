using Microsoft.EntityFrameworkCore;
using OrderingSystem.Application.DTOs;
using OrderingSystem.Application.DTOs.Paged;
using OrderingSystem.Application.Interfaces.Category;
using OrderingSystem.Domain.Common;
using OrderingSystem.Domain.Enums;
using OrderingSystem.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OrderingSystem.Infrastructure.Queries
{
    public class CategoryQuery : ICategoryQuery
    {
        private readonly OrderingSystemDbContext _context;

        public CategoryQuery(OrderingSystemDbContext context)
        {
            _context = context;
        }

        public async Task<Result<CategoriesRecords.CategoryResponse>> GetCategoryAsync(int categoryId)
        {
            var category = await _context.Categories
                .AsNoTracking()
                .Where(c => c.CategoryId == categoryId)
                .Select(c => new CategoriesRecords.CategoryResponse
                (
                    c.CategoryId,
                    c.NameAr,
                    c.NameEn,
                    c.IsAvailable
                ))
                .FirstOrDefaultAsync();

            if (category == null)
            {
                return Result<CategoriesRecords.CategoryResponse>.Failure("Category not found.", enErrorType.NotFound);
            }

            return Result<CategoriesRecords.CategoryResponse>.Success(category);
        }

        public async Task<Result<PagedResponse<CategoriesRecords.CategoryResponse>>> GetAllCategoriesAsync(PageDTO page)
        {
            var query = _context.Categories.AsNoTracking();
            var totalRecords = await query.CountAsync();

            var items = await query
                .Skip((page.PageNumber - 1) * page.PageSize)
                .Take(page.PageSize)
                .Select(c => new CategoriesRecords.CategoryResponse
                (
                    c.CategoryId,
                    c.NameAr,
                    c.NameEn,
                    c.IsAvailable
                ))
                .ToListAsync();

            var pagedResponse = new PagedResponse<CategoriesRecords.CategoryResponse>(items, totalRecords, page.PageNumber, page.PageSize);
            return Result<PagedResponse<CategoriesRecords.CategoryResponse>>.Success(pagedResponse);
        }

        public async Task<Result<PagedResponse<CategoriesRecords.CategoryResponse>>> GetAllAvailableCategoriesAsync(PageDTO page)
        {
            var query = _context.Categories
                .AsNoTracking()
                .Where(c => c.IsAvailable == true);

            var totalRecords = await query.CountAsync();

            var items = await query
                .Skip((page.PageNumber - 1) * page.PageSize)
                .Take(page.PageSize)
                .Select(c => new CategoriesRecords.CategoryResponse
                (
                    c.CategoryId,
                    c.NameAr,
                    c.NameEn,
                    c.IsAvailable
                ))
                .ToListAsync();

            var pagedResponse = new PagedResponse<CategoriesRecords.CategoryResponse>(items, totalRecords, page.PageNumber, page.PageSize);
            return Result<PagedResponse<CategoriesRecords.CategoryResponse>>.Success(pagedResponse);
        }

        public async Task<Result<CategoriesRecords.CategoryResponse>> GetCategoryByIdAsync(int categoryId)
        {
            return await GetCategoryAsync(categoryId);
        }
    }
}