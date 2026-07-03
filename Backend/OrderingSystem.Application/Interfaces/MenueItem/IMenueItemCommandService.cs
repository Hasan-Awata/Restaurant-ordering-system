using OrderingSystem.Application.DTOs;
using OrderingSystem.Domain.Common;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Text;

namespace OrderingSystem.Application.Interfaces.MenueItem
{
    public interface IMenueItemCommandService
    {
       public   Task<Result<MenuRecords.MenuItemResponse>> AddMenuItemAsync(MenuRecords.AddMenuItemRequest request);
        public Task<Result<MenuRecords.MenuItemResponse>> UpdateMenuItemAsync(MenuRecords.UpdateMenuItemRequest request);
         public Task<Result<bool>> DeleteMenuItemAsync(MenuRecords.DeleteMenuItemRequest request);
       

    }
}
