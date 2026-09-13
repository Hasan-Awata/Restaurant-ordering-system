using Microsoft.EntityFrameworkCore;
using OrderingSystem.Application.DTOs;
using OrderingSystem.Application.Interfaces.TableSessionInterfaces;
using OrderingSystem.Application.Interfaces.TaxesInterfaces;
using OrderingSystem.Application.Mappers;
using OrderingSystem.Domain.Entities;
using OrderingSystem.Domain.Enums;
using OrderingSystem.Infrastructure.Data;

namespace OrderingSystem.Infrastructure.Queries
{
    public class TableSessionQuery : ITableSessionQuery
    {
        private readonly OrderingSystemDbContext _context;
        private readonly ITaxCalculationService _taxCalculationService;

        public TableSessionQuery(OrderingSystemDbContext context, ITaxCalculationService taxCalculationService)
        {
            _context = context;
            _taxCalculationService = taxCalculationService;
        }

        public async Task<TableSessionResponse?> GetActiveSessionByTableAsync(int tableId)
        {
            // Bypasses entity tracking and maps directly from SQL to your DTO.
            // This eliminates the need for your SessionsMappers.cs on the read path entirely.
            return await _context.TableSessions
                    .AsNoTracking()
                    .Where(s => s.TableId == tableId && s.ClosedAt == null)
                    .Select(s => new TableSessionResponse(
                        s.TableSessionId,
                        s.Table.TableNumber,
                        s.Status,
                        s.CreatedAt
                    ))
                    .FirstOrDefaultAsync();
        }

        public async Task<BillSummaryResponse?> GetBillSummaryAsync(Guid tableSessionId)
        {
            var session = await _context.TableSessions
                .AsNoTracking()
                .Include(s => s.Orders.Where(o => o.OrderStatus == enOrderStatus.Preparing || o.OrderStatus == enOrderStatus.Served))
                    .ThenInclude(o => o.OrderItems)
                        .ThenInclude(oi => oi.MenuItem)
                .Include(s => s.Devices)
                .FirstOrDefaultAsync(s => s.TableSessionId == tableSessionId);

            if (session == null) return null;

            var activeTaxes = await _context.Taxes
                .AsNoTracking()
                .Where(t => t.IsActive && !t.IsDeleted)
                .ToListAsync();

            decimal totalSubTotal = 0;
            var guestBills = new List<GuestBillResponse>();
            int totalItemsCount = 0;

            foreach (var device in session.Devices)
            {
                var deviceOrders = session.Orders.Where(o => o.DeviceSessionId == device.DeviceSessionId).ToList();
                if (!deviceOrders.Any()) continue;

                decimal guestSubTotal = 0;

                var billItems = deviceOrders.SelectMany(o => o.OrderItems)
                    .GroupBy(oi => oi.MenuItemId)
                    .Select(g =>
                    {
                        var first = g.First();
                        var qty = g.Sum(i => i.Quantity);
                        var itemTotal = qty * first.UnitPrice;
                        guestSubTotal += itemTotal;
                        totalItemsCount += qty;

                        return new BillItemResponse(
                            first.MenuItemId,
                            first.MenuItem?.NameEn ?? "Deleted",
                            first.MenuItem?.NameAr ?? "محذوف",
                            qty,
                            first.UnitPrice,
                            itemTotal
                        );
                    }).ToList();

                totalSubTotal += guestSubTotal;
                guestBills.Add(new GuestBillResponse(device.DeviceSessionId, device.Role, billItems, guestSubTotal));
            }

            var uniqueGuests = session.Devices.Count;

            var taxResult = _taxCalculationService.CalculateTaxes(totalSubTotal, uniqueGuests, totalItemsCount, activeTaxes);
            var grandTotal = totalSubTotal + taxResult.TotalTaxAmount;

            return new BillSummaryResponse(
                tableSessionId,
                guestBills,
                totalSubTotal, 
                taxResult.AppliedTaxes, 
                grandTotal 
            );
        }
        public async Task<SessionPollingResponse?> GetSessionPollingStatusAsync(Guid tableSessionId, Guid deviceSessionId)
        {
            return await _context.SessionDevices
                .AsNoTracking()
                .Where(d => d.DeviceSessionId == deviceSessionId && d.TableSessionId == tableSessionId)
                .Select(d => new SessionPollingResponse(
                    d.TableSession.Status,
                    d.IsApproved
                ))
                .FirstOrDefaultAsync();
        }
    }
}