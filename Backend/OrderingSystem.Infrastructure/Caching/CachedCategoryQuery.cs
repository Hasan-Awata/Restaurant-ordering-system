using Microsoft.Extensions.Caching.Memory;
using OrderingSystem.Application.DTOs;
using OrderingSystem.Application.DTOs.Paged;
using OrderingSystem.Application.Interfaces.Category;
using OrderingSystem.Domain.Common;
using System;
using System.Threading.Tasks;

namespace OrderingSystem.Infrastructure.Caching
{
    public class CachedCategoryQuery : ICategoryQuery
    {
        private readonly ICategoryQuery _inner;
        private readonly IMemoryCache _cache;

        // Define cache expiration globally for categories
        private readonly MemoryCacheEntryOptions _cacheOptions = new MemoryCacheEntryOptions()
            .SetAbsoluteExpiration(TimeSpan.FromHours(6));

        public CachedCategoryQuery(ICategoryQuery inner, IMemoryCache cache)
        {
            _inner = inner;
            _cache = cache;
        }

        public async Task<Result<CategoriesRecords.CategoryResponse>> GetCategoryAsync(int categoryId)
        {
            string cacheKey = $"Category_Details_{categoryId}";

            if (!_cache.TryGetValue(cacheKey, out Result<CategoriesRecords.CategoryResponse>? cachedResult))
            {
                cachedResult = await _inner.GetCategoryAsync(categoryId);

                if (cachedResult != null && cachedResult.IsSuccess)
                {
                    _cache.Set(cacheKey, cachedResult, _cacheOptions);
                }
            }

            return cachedResult!;
        }

        public async Task<Result<CategoriesRecords.CategoryResponse>> GetCategoryByIdAsync(int categoryId)
        {
            string cacheKey = $"Category_ById_{categoryId}";

            if (!_cache.TryGetValue(cacheKey, out Result<CategoriesRecords.CategoryResponse>? cachedResult))
            {
                cachedResult = await _inner.GetCategoryByIdAsync(categoryId);

                if (cachedResult != null && cachedResult.IsSuccess)
                {
                    _cache.Set(cacheKey, cachedResult, _cacheOptions);
                }
            }

            return cachedResult!;
        }

        public async Task<Result<PagedResponse<CategoriesRecords.CategoryResponse>>> GetAllCategoriesAsync(PageDTO page)
        {
            // Cache key must include pagination parameters so different pages don't overwrite each other
            string cacheKey = $"Category_All_{page.PageNumber}_{page.PageSize}";

            if (!_cache.TryGetValue(cacheKey, out Result<PagedResponse<CategoriesRecords.CategoryResponse>>? cachedResult))
            {
                cachedResult = await _inner.GetAllCategoriesAsync(page);

                if (cachedResult != null && cachedResult.IsSuccess)
                {
                    _cache.Set(cacheKey, cachedResult, _cacheOptions);
                }
            }

            return cachedResult!;
        }

        public async Task<Result<PagedResponse<CategoriesRecords.CategoryResponse>>> GetAllAvailableCategoriesAsync(PageDTO page)
        {
            string cacheKey = $"Category_Available_{page.PageNumber}_{page.PageSize}";

            if (!_cache.TryGetValue(cacheKey, out Result<PagedResponse<CategoriesRecords.CategoryResponse>>? cachedResult))
            {
                cachedResult = await _inner.GetAllAvailableCategoriesAsync(page);

                if (cachedResult != null && cachedResult.IsSuccess)
                {
                    _cache.Set(cacheKey, cachedResult, _cacheOptions);
                }
            }

            return cachedResult!;
        }
    }
}