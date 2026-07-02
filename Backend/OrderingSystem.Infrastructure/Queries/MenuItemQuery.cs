using Microsoft.EntityFrameworkCore;
using OrderingSystem.Application.DTOs;
using OrderingSystem.Application.DTOs.Paged;
using OrderingSystem.Application.Interfaces.MenueItem;
using OrderingSystem.Domain.Common;
using OrderingSystem.Domain.Enums;
using OrderingSystem.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace OrderingSystem.Infrastructure.Queries
{
    public class MenuItemQuery : IMenueItemQuery
    {
        private readonly OrderingSystemDbContext _context;

        public MenuItemQuery(OrderingSystemDbContext context)
        {
            _context = context;
        }

        // جلب عنصر واحد بواسطة الـ ID
        public async Task<Result<MenuRecords.MenuItemResponse>> GetMenuItemAsync(int menuItemId)
        {
            var menuItem = await _context.MenuItems
                .AsNoTracking()
                .Where(m => m.MenuItemId == menuItemId)
                .Select(m => new MenuRecords.MenuItemResponse
                (
                    m.MenuItemId,
                    m.CategoryId,
                    m.NameAr,
                    m.NameEn,
                    m.Description,
                    m.Price,
                    m.ImageUrl,
                    m.IsAvailable
                ))
                .FirstOrDefaultAsync();

            if (menuItem == null)
            {
                return Result<MenuRecords.MenuItemResponse>.Failure("Menu item not found.", enErrorType.NotFound);
            }

            return Result<MenuRecords.MenuItemResponse>.Success(menuItem);
        }

        
        public async Task<Result<PagedResponse<MenuRecords.MenuItemResponse>>> GetAllMenuItemsByCategoryAsync(int categoryId, PageDTO page)
        {
            var query = _context.MenuItems
                .AsNoTracking()
                .Where(m => m.CategoryId == categoryId);

            var totalRecords = await query.CountAsync();

            var items = await query
                .Skip((page.PageNumber - 1) * page.PageSize)
                .Take(page.PageSize)
                .Select(m => new MenuRecords.MenuItemResponse
                (
                    m.MenuItemId,
                    m.CategoryId,
                    m.NameAr,
                    m.NameEn,
                    m.Description,
                    m.Price,
                    m.ImageUrl,
                    m.IsAvailable
                ))
                .ToListAsync();

            var pagedResponse = new PagedResponse<MenuRecords.MenuItemResponse>(items, totalRecords, page.PageNumber, page.PageSize);
            return Result<PagedResponse<MenuRecords.MenuItemResponse>>.Success(pagedResponse);
        }

     
        public async Task<Result<PagedResponse<MenuRecords.MenuItemResponse>>> GetAllMenuItemsAsync(PageDTO page)
        {
            var query = _context.MenuItems.AsNoTracking();
            var totalRecords = await query.CountAsync();

            var items = await query
                .Skip((page.PageNumber - 1) * page.PageSize)
                .Take(page.PageSize)
                .Select(m => new MenuRecords.MenuItemResponse
                (
                    m.MenuItemId,
                    m.CategoryId,
                    m.NameAr,
                    m.NameEn,
                    m.Description,
                    m.Price,
                    m.ImageUrl,
                    m.IsAvailable
                ))
                .ToListAsync();

            var pagedResponse = new PagedResponse<MenuRecords.MenuItemResponse>(items, totalRecords, page.PageNumber, page.PageSize);
            return Result<PagedResponse<MenuRecords.MenuItemResponse>>.Success(pagedResponse);
        }

    
        public async Task<Result<PagedResponse<MenuRecords.MenuItemResponse>>> GetAllAvailableMenuItemsAsync(PageDTO page)
        {
            var query = _context.MenuItems
                .AsNoTracking()
                .Where(m => m.IsAvailable == true);

            var totalRecords = await query.CountAsync();

            var items = await query
                .Skip((page.PageNumber - 1) * page.PageSize)
                .Take(page.PageSize)
                .Select(m => new MenuRecords.MenuItemResponse
                (
                    m.MenuItemId,
                    m.CategoryId,
                    m.NameAr,
                    m.NameEn,
                    m.Description,
                    m.Price,
                    m.ImageUrl,
                    m.IsAvailable
                ))
                .ToListAsync();

            var pagedResponse = new PagedResponse<MenuRecords.MenuItemResponse>(items, totalRecords, page.PageNumber, page.PageSize);
            return Result<PagedResponse<MenuRecords.MenuItemResponse>>.Success(pagedResponse);
        }

        // بما أن هذا التابع يؤدي نفس وظيفة دالة GetMenuItemAsync، يمكننا استدعاؤها مباشرة للاختصار ومنع التكرار
        public async Task<Result<MenuRecords.MenuItemResponse>> GetItemByIdAsync(int menuItemId)
        {
            return await GetMenuItemAsync(menuItemId);
        }
    }
}