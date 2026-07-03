using OrderingSystem.Application.DTOs;
using OrderingSystem.Application.Interfaces.MenueItem;
using OrderingSystem.Application.Mappers;
using OrderingSystem.Domain.Common;
using OrderingSystem.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Text;

namespace OrderingSystem.Application.Services
{
    public class MenueItemCommandService : IMenueItemCommandService

    {

        private Result<MenuRecords.MenuItemResponse> ValidateAddMenuItemRequest(int categoryId,string nameEn,string nameAr,string imageUrl,decimal price,string description)
        {
           
            if (categoryId <= 0)
            {
                return Result<MenuRecords.MenuItemResponse>.Failure("Invalid CategoryId.");
            }
            if (string.IsNullOrEmpty(imageUrl))
            {
                return Result<MenuRecords.MenuItemResponse>.Failure("ImageUrl cannot be null or empty.");
            }
            if (string.IsNullOrEmpty(nameAr) || string.IsNullOrEmpty(nameEn))
            {
                return Result<MenuRecords.MenuItemResponse>.Failure("NameAr and NameEn cannot be null or empty.");
            }
            if (price <= 0)
            {
                return Result<MenuRecords.MenuItemResponse>.Failure("Price must be greater than zero.");
            }
            if (string.IsNullOrEmpty(description))
            {
                return Result<MenuRecords.MenuItemResponse>.Failure("Description cannot be null or empty.");
            }

           
            return Result<MenuRecords.MenuItemResponse>.Success(null);
        }
        private readonly IMenueItemRepository _menuItemRepository;
   
        public MenueItemCommandService(IMenueItemRepository menuItemRepository)
        {
            _menuItemRepository = menuItemRepository;
            
        }
        public async Task<Result<MenuRecords.MenuItemResponse>> AddMenuItemAsync(MenuRecords.AddMenuItemRequest request)
        {
            if (request == null)
            {
                return Result<MenuRecords.MenuItemResponse>.Failure("Request cannot be null.");
            }
            var validationResult = ValidateAddMenuItemRequest( request.CategoryId, request.NameEn, request.NameAr, request.ImageUrl, request.Price, request.Description);

        
            if (!validationResult.IsSuccess)
            {
                return Result<MenuRecords.MenuItemResponse>.Failure(validationResult.ErrorMessage);
              
            }
            if (await _menuItemRepository.ItemIsExistsByNameAsync(request.NameEn,request.NameAr, request.CategoryId ))
            {
                return Result<MenuRecords.MenuItemResponse>.Failure(
                   $"A menu item with the name '{request.NameAr}' or '{request.NameEn}' already exists in this category.",
                   enErrorType.Validation
               );
            }


            var menuItem = MenuItemsMappers.ToEntity(request);

            if (menuItem == null)
            {
                return Result<MenuRecords.MenuItemResponse>.Failure("Failed to map request to entity.");
            }
           await _menuItemRepository.AddMenuItemAsync(menuItem);
            var response = menuItem.ToResponse();
            return Result<MenuRecords.MenuItemResponse>.Success(response);

        }
        public async Task<Result<MenuRecords.MenuItemResponse>> UpdateMenuItemAsync(MenuRecords.UpdateMenuItemRequest request)
        {
            if (request == null)
            {
                return Result<MenuRecords.MenuItemResponse>.Failure("Request cannot be null.");
            }
           
            if(ValidateAddMenuItemRequest(request.CategoryId, request.NameEn, request.NameAr, request.ImageUrl, request.Price, request.Description).IsSuccess == false)
            {
                return Result<MenuRecords.MenuItemResponse>.Failure("Invalid request data.");
            }
        
            if(request.MenuItemId <= 0)
            {
                return Result<MenuRecords.MenuItemResponse>.Failure("Invalid MenuItemId.");
            }
        
             var existingMenuItem = await _menuItemRepository.GetMenuItemByIdAsync(request.MenuItemId);
            if (existingMenuItem == null)
            {
                return Result<MenuRecords.MenuItemResponse>.Failure($"Menu item with ID {request.MenuItemId} not found.");
            }
            existingMenuItem.CategoryId = request.CategoryId;
            existingMenuItem.NameAr = request.NameAr;
            existingMenuItem.NameEn = request.NameEn;
            existingMenuItem.Description = request.Description;
            existingMenuItem.Price = request.Price;
            existingMenuItem.ImageUrl = request.ImageUrl;
            existingMenuItem.IsAvailable = request.IsAvailable;
           await _menuItemRepository.UpdateMenuItemAsync(existingMenuItem);
            var response = existingMenuItem.ToResponse();
            return Result<MenuRecords.MenuItemResponse>.Success(response);

        }
        public async Task<Result<bool>> DeleteMenuItemAsync(MenuRecords.DeleteMenuItemRequest request)
        {
            if (request == null)
            {
                return Result<bool>.Failure("Request cannot be null.");
            }
            var existingMenuItem = await _menuItemRepository.GetMenuItemByIdAsync(request.MenuItemId);
            if (existingMenuItem == null)
            {
                return Result<bool>.Failure($"Menu item with ID {request.MenuItemId} not found.");
            }
            await _menuItemRepository.DeleteMenuItemAsync(existingMenuItem);
            return Result<bool>.Success(true);
        }
        
    }
}

