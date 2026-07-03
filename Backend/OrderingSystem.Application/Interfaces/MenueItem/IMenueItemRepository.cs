using OrderingSystem.Application.DTOs;
using OrderingSystem.Domain.Common;
using OrderingSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderingSystem.Application.Interfaces.MenueItem
{
    public interface IMenueItemRepository 
    {
        public Task AddMenuItemAsync(MenuItem menuItem);
        public Task UpdateMenuItemAsync(MenuItem menuItem);
        public Task<bool> DeleteMenuItemAsync(MenuItem menuItemq);
          public Task<MenuItem?> GetMenuItemByIdAsync(int menuItemId);
        public Task<bool> GetItemExistsAsync(int menuItemId);
        public  Task<bool> ItemIsExistsByNameAsync(string nameEn, string nameAr, int categoryId);




    }
}
