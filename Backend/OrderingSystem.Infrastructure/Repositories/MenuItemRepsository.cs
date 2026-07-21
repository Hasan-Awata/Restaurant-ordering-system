using OrderingSystem.Application.DTOs;
using OrderingSystem.Application.Interfaces.MenueItem;
using OrderingSystem.Infrastructure.Data;
using OrderingSystem.Domain.Common;
using OrderingSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.EntityFrameworkCore;

namespace OrderingSystem.Infrastructure.Repositories
{
    public class MenuItemRepsository :IMenueItemRepository
    {
        private readonly OrderingSystemDbContext _dbContext;
        public MenuItemRepsository(OrderingSystemDbContext dbContext)
        {
            _dbContext = dbContext;
        }
        public async Task AddMenuItemAsync(MenuItem menuItem)
        {
          _dbContext.MenuItems.Add(menuItem);
            await _dbContext.SaveChangesAsync();

        }
       
        public async Task UpdateMenuItemAsync(MenuItem menuItem)
        {
            _dbContext.MenuItems.Update(menuItem);
            await _dbContext.SaveChangesAsync();
        }
        public async Task<bool> DeleteMenuItemAsync(MenuItem menuItem)
        {
            if (menuItem == null) return false;

            menuItem.IsDeleted = true;
            menuItem.IsAvailable = false;

            _dbContext.MenuItems.Update(menuItem);
            await _dbContext.SaveChangesAsync();
            return true;
        }


        public async Task<MenuItem?>GetMenuItemByIdAsync(int menuItemId)
        {
           return await _dbContext.MenuItems.FirstOrDefaultAsync(m => m.MenuItemId == menuItemId);

        }
        public Task<bool> GetItemExistsAsync(int menuItemId)
        {
          _dbContext.MenuItems.Find(menuItemId);
            var menuItem = _dbContext.MenuItems.Find(menuItemId);
            if (menuItem == null)
            {
                return Task.FromResult(false);
            }
            return Task.FromResult(true);
        }
        public async Task<bool> ItemIsExistsByNameAsync(string nameEn, string nameAr, int categoryId)
        {
            return await _dbContext.MenuItems
                .AsNoTracking()
                .AnyAsync(m => m.CategoryId == categoryId &&
                              (m.NameEn.ToLower() == nameEn.ToLower() ||
                               m.NameAr.ToLower() == nameAr.ToLower()));
        }
    }
}
