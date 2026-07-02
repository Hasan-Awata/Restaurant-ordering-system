using OrderingSystem.Application.DTOs;
using OrderingSystem.Application.DTOs.Paged;
using OrderingSystem.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderingSystem.Application.Interfaces.MenueItem
{
    public interface IMenueItemQuery
    {
       public Task<Result<MenuRecords.MenuItemResponse>> GetMenuItemAsync(int menuItemId);
        public Task<Result<PagedResponse<MenuRecords.MenuItemResponse>>> GetAllMenuItemsByCategoryAsync(int categoryId, PageDTO page);
        public Task<Result<PagedResponse<MenuRecords.MenuItemResponse>>> GetAllMenuItemsAsync(PageDTO page);
         public Task<Result<PagedResponse<MenuRecords.MenuItemResponse>>> GetAllAvailableMenuItemsAsync(PageDTO page);
        public Task<Result<MenuRecords.MenuItemResponse>> GetItemByIdAsync(int menuItemId);
       



    }
}
