using Microsoft.EntityFrameworkCore;
using OrderingSystem.Application.DTOs;
using OrderingSystem.Application.DTOs.Paged;
using OrderingSystem.Application.Interfaces.OrdersInterfaces;
using OrderingSystem.Application.Interfaces.TaxesInterfaces;
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
        private readonly ITaxCalculationService _taxCalculationService;

        public OrderQuery(OrderingSystemDbContext context, ITaxCalculationService taxCalculationService)
        {
            _context = context;
            _taxCalculationService = taxCalculationService;
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
                var today = DateTime.SpecifyKind(DateTime.Today, DateTimeKind.Utc);

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
                DateTime startDate, DateTime endDate, PageDTO page)
        {
            var activeTaxes = await _context.Taxes
                .AsNoTracking()
                .Where(t => t.IsActive && !t.IsDeleted)
                .ToListAsync();

            var sessionQuery = _context.Orders
                .AsNoTracking()
                .Where(o => o.CreatedAt >= startDate && o.CreatedAt <= endDate) 
                .GroupBy(o => o.TableSessionId)
                .Select(g => new
                {
                    SessionId = g.Key,
                    LastOrderDate = g.Max(o => o.CreatedAt)
                });

            var totalRecords = await sessionQuery.CountAsync();

            var pagedSessions = await sessionQuery
                .OrderByDescending(x => x.LastOrderDate)
                .Skip((page.PageNumber - 1) * page.PageSize)
                .Take(page.PageSize)
                .ToListAsync();

            var sessionIds = pagedSessions.Select(s => s.SessionId).ToList();

            var ordersForSessions = await _context.Orders
                .AsNoTracking()
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.MenuItem)
                .Include(o => o.Session)
                    .ThenInclude(s => s.Table)
                .Where(o => sessionIds.Contains(o.TableSessionId))
                .ToListAsync();

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
                                        firstItem.MenuItem != null ? firstItem.MenuItem.NameAr : "Deleted Item",
                                        firstItem.MenuItem != null ? firstItem.MenuItem.NameEn : "Deleted Item",
                                        totalQuantity,
                                        firstItem.UnitPrice,
                                        totalQuantity * firstItem.UnitPrice
                                    );
                                }).ToList();

                            decimal subTotal = g.Sum(o => o.TotalAmount);
                            var uniqueGuests = g.Select(o => o.DeviceSessionId).Distinct().Count();
                            var totalItemsCount = groupedItems.Sum(i => i.Quantity);

                            var taxResult = _taxCalculationService.CalculateTaxes(subTotal, uniqueGuests, totalItemsCount, activeTaxes);
                            var grandTotal = subTotal + taxResult.TotalTaxAmount;

                            return new HistoricalBillResponse(
                                firstOrder.OrderId.ToString(),
                                firstOrder.Session.Table.TableNumber,
                                firstOrder.TableSessionId.ToString(),
                                firstOrder.OrderStatus.ToString(),
                                lastOrder.CreatedAt,
                                subTotal,
                                grandTotal, 
                                groupedItems,
                                taxResult.AppliedTaxes 
                            );
                        }).ToList();

            var pagedResponse = new PagedResponse<HistoricalBillResponse>(items, totalRecords, page.PageNumber, page.PageSize);
            return Result<PagedResponse<HistoricalBillResponse>>.Success(pagedResponse);
        }
    }
}