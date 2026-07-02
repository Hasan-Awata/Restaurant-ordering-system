using OrderingSystem.Application.DTOs;
using OrderingSystem.Domain.Entities;
using System;
using static OrderingSystem.Application.DTOs.MenuRecords;

namespace OrderingSystem.Application.Mappers
{
    public static class MenuItemsMappers
    {
        public static MenuItemResponse ToResponse(this MenuItem entity)
        {
            return new MenuItemResponse(
                entity.MenuItemId,    
                entity.CategoryId,     
                entity.NameAr,         
                entity.NameEn,        
                entity.Description,
                entity.Price,
                entity.ImageUrl,       
                entity.IsAvailable
            );
        }
        public static MenuItem ToEntity(this AddMenuItemRequest request)
        {
            return new MenuItem
            {
                MenuItemId = -1,
                CategoryId = request.CategoryId,
                NameAr = request.NameAr,
                NameEn = request.NameEn,
                Description = request.Description,
                Price = request.Price,
                ImageUrl = request.ImageUrl,
                IsAvailable = request.IsAvailable
            };
        }
    }
}