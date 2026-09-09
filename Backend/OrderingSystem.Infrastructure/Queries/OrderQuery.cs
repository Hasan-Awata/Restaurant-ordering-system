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

        public async Task<Result<PagedResponse<HistoricalBillResponse>>> GetHistoricalBillsAsync(DateTime startDate, DateTime endDate, PageDTO page)
        {
            var query = _context.Bills
                .Include(b => b.TableSession)
                    .ThenInclude(ts => ts.Table)
                .Include(b => b.BillItems)
                .Include(b => b.BillTaxes)
                .Where(b => b.CreatedAt >= startDate && b.CreatedAt <= endDate)
                .OrderByDescending(b => b.CreatedAt);

            var totalRecords = await query.CountAsync();
            var bills = await query
                .Skip((page.PageNumber - 1) * page.PageSize)
                .Take(page.PageSize)
                .ToListAsync();

            var responseData = bills.Select(b => new HistoricalBillResponse(
                OrderId: b.BillId, 
                TableNumber: b.TableSession.Table.TableNumber,
                TableSessionId: b.TableSessionId.ToString(),
                Status: "Closed",
                CreatedAt: b.CreatedAt,
                TotalSubTotal: b.TotalSubTotal,
                GrandTotal: b.GrandTotal,
                Items: b.BillItems.Select(bi => new OrderRecords.BillItemResponse(
                    bi.MenuItemId, bi.NameAr, bi.NameEn, bi.Quantity, bi.UnitPrice, bi.TotalPrice
                )).ToList(),
                AppliedTaxes: b.BillTaxes.Select(bt => new AppliedTaxResponse(
                    bt.TaxNameEn, bt.TaxNameAr, bt.AppliedAmount
                )).ToList()
            )).ToList();

            return Result<PagedResponse<HistoricalBillResponse>>.Success(
                new PagedResponse<HistoricalBillResponse>(responseData, totalRecords, page.PageNumber, page.PageSize)
            );
        }
    }
}