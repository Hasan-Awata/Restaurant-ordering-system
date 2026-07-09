using OrderingSystem.Application.DTOs;
using OrderingSystem.Application.Interfaces.Category;
using OrderingSystem.Infrastructure.Data;
using OrderingSystem.Domain.Common;
using OrderingSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace OrderingSystem.Infrastructure.Repositories
{
    public class CategoryRepository : ICategoryRepository
    {
        private readonly OrderingSystemDbContext _dbContext;

        public CategoryRepository(OrderingSystemDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task AddCategoryAsync(Category category)
        {
            _dbContext.Categories.Add(category);
            await _dbContext.SaveChangesAsync();
        }

        public async Task UpdateCategoryAsync(Category category)
        {
            _dbContext.Categories.Update(category);
            await _dbContext.SaveChangesAsync();
        }

        public async Task<bool> DeleteCategoryAsync(Category category)
        {
            if (category == null)
            {
                return false;
            }

            _dbContext.Categories.Remove(category);
            await _dbContext.SaveChangesAsync();
            return true;
        }

        public async Task<Category?> GetCategoryByIdAsync(int categoryId)
        {
            return await _dbContext.Categories.FirstOrDefaultAsync(c => c.CategoryId == categoryId);

        }

        public async Task<bool> GetCategoryExistsAsync(int categoryId)
        {
            return await _dbContext.Categories.AnyAsync(c => c.CategoryId == categoryId);
        }

        public async Task<bool> CategoryIsExistsByNameAsync(string nameEn, string nameAr)
        {
            return await _dbContext.Categories
                .AsNoTracking()
                .AnyAsync(c => c.NameEn.ToLower() == nameEn.ToLower() ||
                               c.NameAr.ToLower() == nameAr.ToLower());
        }
    }
}