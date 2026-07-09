using Microsoft.EntityFrameworkCore;
using OrderingSystem.Application.DTOs;
using OrderingSystem.Application.DTOs.Paged;
using OrderingSystem.Application.Interfaces.OrdersInterfaces;
using OrderingSystem.Domain.Common;
using OrderingSystem.Domain.Enums;
using OrderingSystem.Infrastructure.Data;
using System.Linq;
using System.Threading.Tasks;

namespace OrderingSystem.Infrastructure.Queries
{
    public class OrderQuery : IOrderQuery
    {
        private readonly OrderingSystemDbContext _context;

        public OrderQuery(OrderingSystemDbContext context)
        {
            _context = context;
        }

        public async Task<Result<PagedResponse<OrderRecords.OrderResponse>>> GetPendingOrdersAsync(PageDTO page)
        {
            var query = _context.Orders
                .AsNoTracking()
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                .Where(o => o.OrderStatus == enOrderStatus.Pending);

            var totalRecords = await query.CountAsync();

            var items = await query
                .OrderBy(o => o.CreatedAt) 
                .Skip((page.PageNumber - 1) * page.PageSize)
                .Take(page.PageSize)
                .Select(o => new OrderRecords.OrderResponse(
                    o.OrderId,
                    o.TotalAmount,
                    o.OrderStatus,
                    o.CreatedAt,
                    o.OrderItems.Select(oi => new OrderRecords.OrderItemResponse(
                        oi.MenuItemId,
                        oi.MenuItem.NameEn,
                        oi.MenuItem.NameAr,
                        oi.Quantity,
                        oi.UnitPrice
                    )).ToList()
                ))
                .ToListAsync();

            var pagedResponse = new PagedResponse<OrderRecords.OrderResponse>(items, totalRecords, page.PageNumber, page.PageSize);
            return Result<PagedResponse<OrderRecords.OrderResponse>>.Success(pagedResponse);
        }
    }
}