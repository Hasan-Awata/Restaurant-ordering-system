using Microsoft.EntityFrameworkCore;
using OrderingSystem.Application.DTOs;
using OrderingSystem.Application.Interfaces.TableSessionInterfaces;
using OrderingSystem.Application.Mappers;
using OrderingSystem.Domain.Entities;
using OrderingSystem.Domain.Enums;
using OrderingSystem.Infrastructure.Data;

namespace OrderingSystem.Infrastructure.Queries
{
    public class TableSessionQuery : ITableSessionQuery
    {
        private readonly OrderingSystemDbContext _context;

        public TableSessionQuery(OrderingSystemDbContext context)
        {
            _context = context;
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
                .Include(s => s.Devices)
                .Include(s => s.Orders.Where(o => o.OrderStatus != enOrderStatus.Cancelled))
                    .ThenInclude(o => o.OrderItems)
                        .ThenInclude(oi => oi.MenuItem)
                .FirstOrDefaultAsync(s => s.TableSessionId == tableSessionId);

            if (session == null) return null;

            var guestBills = new List<GuestBillResponse>();
            decimal totalSubTotal = 0;

            foreach (var device in session.Devices)
            {
                var deviceOrders = session.Orders.Where(o => o.DeviceSessionId == device.DeviceSessionId).ToList();
                if (!deviceOrders.Any()) continue;

                // Flatten items for this device, group by MenuItemId to sum quantities of identical items
                var groupedItems = deviceOrders
                    .SelectMany(o => o.OrderItems)
                    .GroupBy(oi => oi.MenuItemId)
                    .Select(g => new BillItemResponse(
                        g.Key,
                        g.First().MenuItem?.NameEn ?? "Deleted Item",
                        g.First().MenuItem?.NameAr ?? "عنصر محذوف",
                        g.Sum(oi => oi.Quantity),
                        g.First().UnitPrice, // Using the historical price saved on the OrderItem
                        g.Sum(oi => oi.Quantity * oi.UnitPrice)
                    )).ToList();

                decimal subTotal = groupedItems.Sum(i => i.TotalPrice);
                totalSubTotal += subTotal;

                guestBills.Add(new GuestBillResponse(device.DeviceSessionId, device.Role, groupedItems, subTotal));
            }

            // TODO: Replace this with an actual database fetch from the future Taxes table
            decimal taxRate = 0.15m;
            decimal taxAmount = totalSubTotal * taxRate;
            decimal grandTotal = totalSubTotal + taxAmount;

            return new BillSummaryResponse(
                session.TableSessionId,
                guestBills,
                totalSubTotal,
                taxAmount,
                grandTotal
            );
        }
    }
}