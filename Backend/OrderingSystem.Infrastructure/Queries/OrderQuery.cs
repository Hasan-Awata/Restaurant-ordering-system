using Microsoft.EntityFrameworkCore;
using OrderingSystem.Application.DTOs;
using OrderingSystem.Application.DTOs.Paged;
using OrderingSystem.Application.Interfaces.OrdersInterfaces;
using OrderingSystem.Domain.Common;
using OrderingSystem.Domain.Enums;
using OrderingSystem.Infrastructure.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static OrderingSystem.Application.DTOs.OrderRecords;

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
                    o.Session.Table.TableNumber,
                    o.TotalAmount,
                    o.OrderStatus,
                    o.CreatedAt,
                    o.OrderItems.Select(oi => new OrderRecords.OrderItemResponse(
                        oi.MenuItemId,
                        oi.MenuItem != null ? oi.MenuItem.NameEn : "Deleted Item",
                        oi.MenuItem != null ? oi.MenuItem.NameAr : "عنصر محذوف",
                        oi.Quantity,
                        oi.UnitPrice,
                        oi.Notes
                    )).ToList()
                ))
                .ToListAsync();

            var pagedResponse = new PagedResponse<OrderRecords.OrderResponse>(items, totalRecords, page.PageNumber, page.PageSize);
            return Result<PagedResponse<OrderRecords.OrderResponse>>.Success(pagedResponse);
        }

        public async Task<Result<List<OrderRecords.OrderItemResponse>>> GetTopThreeItemsTodayAsync()
        {
            var today = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Utc);

            var rawItems = await _context.OrderItems
                .AsNoTracking()
                .Where(oi => oi.Order.CreatedAt.Date == today && oi.Order.OrderStatus != enOrderStatus.Cancelled)
                .Where(oi => !oi.MenuItem.IsDeleted)
                .GroupBy(oi => new
                {
                    oi.MenuItemId,
                    oi.MenuItem.NameEn,
                    oi.MenuItem.NameAr
                })
                .Select(g => new
                {
                    g.Key.MenuItemId,
                    g.Key.NameEn,
                    g.Key.NameAr,
                    TotalQuantity = g.Sum(oi => oi.Quantity)
                })
                .OrderByDescending(r => r.TotalQuantity)
                .Take(3)
                .ToListAsync();

            var itemsList = rawItems
                .Select(x => new OrderRecords.OrderItemResponse(
                    x.MenuItemId,
                    x.NameEn,
                    x.NameAr,
                    x.TotalQuantity,
                    0m,
                    string.Empty
                ))
                .ToList();

            return Result<List<OrderRecords.OrderItemResponse>>.Success(itemsList);
        }

        public async Task<Result<int>> GetCountOfPendingOrder()
        {
            try
            {
                var count = await _context.Orders
                    .Where(o => o.OrderStatus == enOrderStatus.Pending)
                    .CountAsync();

                return Result<int>.Success(count);
            }
            catch (Exception ex)
            {
                return Result<int>.Failure($"Error when fetching pending order count: {ex.Message}");
            }
        }

        public async Task<Result<int>> GetCountOfOrders()
        {
            try
            {
                // تحديد تاريخ اليوم الحالي
                var today = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Utc);

                // حساب الطلبات التي تم إنشاؤها اليوم فقط
                var count = await _context.Orders
                    .Where(o => o.CreatedAt.Date == today)
                    .CountAsync();

                return Result<int>.Success(count);
            }
            catch (Exception ex)
            {
                return Result<int>.Failure($"Error when fetching today's order count: {ex.Message}");
            }
        }

        public async Task<Result<PagedResponse<OrderRecords.OrderResponse>>> GetOrdersByDateRangeAsync(DateTime startDate, DateTime endDate, PageDTO page)
        {
            var query = _context.Orders
                .AsNoTracking()
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate);

            var totalRecords = await query.CountAsync();

            var items = await query
                .OrderByDescending(o => o.CreatedAt)
                .Skip((page.PageNumber - 1) * page.PageSize)
                .Take(page.PageSize)
                .Select(o => new OrderRecords.OrderResponse(
                    o.OrderId,
                    o.Session.Table.TableNumber,
                    o.TotalAmount,
                    o.OrderStatus,
                    o.CreatedAt,
                    o.OrderItems.Select(oi => new OrderRecords.OrderItemResponse(
                        oi.MenuItemId,
                        oi.MenuItem != null ? oi.MenuItem.NameEn : "Deleted Item",
                        oi.MenuItem != null ? oi.MenuItem.NameAr : "عنصر محذوف",
                        oi.Quantity,
                        oi.UnitPrice,
                        oi.Notes
                    )).ToList()
                ))
                .ToListAsync();

            var pagedResponse = new PagedResponse<OrderRecords.OrderResponse>(items, totalRecords, page.PageNumber, page.PageSize);
            return Result<PagedResponse<OrderRecords.OrderResponse>>.Success(pagedResponse);
        }

        public async Task<Result<PagedResponse<HistoricalBillResponse>>> GetHistoricalBillsAsync(
            DateTime startDate,
            DateTime endDate,
            PageDTO page)
        {
            // 1. تحديد الجلسات (Sessions) ضمن الفترة الزمنية المحددة (تجميع حسب TableSessionId)
            var sessionQuery = _context.Orders
                .AsNoTracking()
                .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate && o.TableSessionId != null)
                .GroupBy(o => o.TableSessionId)
                .Select(g => new
                {
                    SessionId = g.Key,
                    LastOrderDate = g.Max(o => o.CreatedAt)
                });

            // 2. حساب العدد الإجمالي للجلسات
            var totalRecords = await sessionQuery.CountAsync();

            // 3. جلب TableSessionIds للصفحة الحالية
            var pagedSessions = await sessionQuery
                .OrderByDescending(x => x.LastOrderDate)
                .Skip((page.PageNumber - 1) * page.PageSize)
                .Take(page.PageSize)
                .ToListAsync();

            var sessionIds = pagedSessions.Select(s => s.SessionId).ToList();

            // 4. جلب جميع الطلبات التي تخص جلسات هذه الصفحة
            var ordersForSessions = await _context.Orders
                .AsNoTracking()
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                .Include(o => o.Session)
                    .ThenInclude(s => s.Table)
                .Where(o => sessionIds.Contains(o.TableSessionId))
                .ToListAsync();

            // 5. تجميع المعاملات في الذاكرة لتشكيل الفواتير المجمعة
            var items = ordersForSessions
                .GroupBy(o => o.TableSessionId)
                .Select(g =>
                {
                    var firstOrder = g.OrderBy(o => o.CreatedAt).First();
                    var lastOrder = g.OrderByDescending(o => o.CreatedAt).First();

                    var groupedItems = g.SelectMany(o => o.OrderItems)
                        .GroupBy(oi => oi.MenuItemId)
                        .Select(itemGroup =>
                        {
                            var firstItem = itemGroup.First();
                            var totalQuantity = itemGroup.Sum(i => i.Quantity);

                            return new OrderRecords.BillItemResponse(
                                firstItem.MenuItemId,
                                firstItem.MenuItem != null ? firstItem.MenuItem.NameAr : "عنصر محذوف",
                                firstItem.MenuItem != null ? firstItem.MenuItem.NameEn : "Deleted Item",
                                totalQuantity,
                                firstItem.UnitPrice,
                                totalQuantity * firstItem.UnitPrice
                            );
                        }).ToList();

                    return new HistoricalBillResponse(
                        $"BILL-{firstOrder.OrderId}",
                        firstOrder.Session?.Table?.TableNumber ?? 0,
                        g.Key.ToString(),
                        lastOrder.OrderStatus.ToString().ToLower(),
                        lastOrder.CreatedAt,
                        g.Sum(o => o.TotalAmount),
                        groupedItems
                    );
                })
                .OrderByDescending(x => x.CreatedAt)
                .ToList();

            var pagedResponse = new PagedResponse<HistoricalBillResponse>(items, totalRecords, page.PageNumber, page.PageSize);
            return Result<PagedResponse<HistoricalBillResponse>>.Success(pagedResponse);
        }
    }
}