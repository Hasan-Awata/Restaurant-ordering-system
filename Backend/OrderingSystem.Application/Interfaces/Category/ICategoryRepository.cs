using OrderingSystem.Application.DTOs;
using OrderingSystem.Domain.Common;
using OrderingSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace OrderingSystem.Application.Interfaces.Category
{
    public interface ICategoryRepository
    {
        public Task AddCategoryAsync(Domain.Entities.Category category);
        public Task UpdateCategoryAsync(Domain.Entities.Category category);
        public Task<bool> DeleteCategoryAsync(Domain.Entities.Category category);
        public Task<Domain.Entities.Category?> GetCategoryByIdAsync(int categoryId);
        public Task<bool> GetCategoryExistsAsync(int categoryId);
         public Task<bool> CategoryIsExistsByNameAsync(string nameEn, string nameAr);
    }
}