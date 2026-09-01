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
            int totalItemsQuantity = 0; // Track this for PerItem taxes

            foreach (var device in session.Devices)
            {
                var deviceOrders = session.Orders.Where(o => o.DeviceSessionId == device.DeviceSessionId).ToList();
                if (!deviceOrders.Any()) continue;

                var groupedItems = deviceOrders
                    .SelectMany(o => o.OrderItems)
                    .GroupBy(oi => oi.MenuItemId)
                    .Select(g => new BillItemResponse(
                        g.Key,
                        g.First().MenuItem?.NameEn ?? "Deleted Item",
                        g.First().MenuItem?.NameAr ?? "عنصر محذوف",
                        g.Sum(oi => oi.Quantity),
                        g.First().UnitPrice,
                        g.Sum(oi => oi.Quantity * oi.UnitPrice)
                    )).ToList();

                decimal subTotal = groupedItems.Sum(i => i.TotalPrice);
                totalSubTotal += subTotal;
                totalItemsQuantity += groupedItems.Sum(i => i.Quantity);

                guestBills.Add(new GuestBillResponse(device.DeviceSessionId, device.Role, groupedItems, subTotal));
            }

            // 1. Fetch active taxes dynamically
            var activeTaxes = await _context.Taxes
                .AsNoTracking()
                .Where(t => t.IsActive)
                .ToListAsync();

            var appliedTaxes = new List<AppliedTaxResponse>();
            decimal totalTaxAmount = 0;

            // 2. Process each tax based on its Type and Scope
            foreach (var tax in activeTaxes)
            {
                decimal calculatedAmount = 0;

                if (tax.TaxType == enTaxType.Percentage)
                {
                    // Percentages apply to the subtotal
                    calculatedAmount = totalSubTotal * (tax.Amount / 100m);
                }
                else if (tax.TaxType == enTaxType.FlatRate)
                {
                    switch (tax.TaxScope)
                    {
                        case enTaxScope.PerBill:
                            calculatedAmount = tax.Amount;
                            break;
                        case enTaxScope.PerGuest:
                            calculatedAmount = tax.Amount * session.Devices.Count;
                            break;
                        case enTaxScope.PerItem:
                            calculatedAmount = tax.Amount * totalItemsQuantity;
                            break;
                    }
                }

                if (calculatedAmount > 0)
                {
                    appliedTaxes.Add(new AppliedTaxResponse(tax.NameEn, tax.NameAr, calculatedAmount));
                    totalTaxAmount += calculatedAmount;
                }
            }

            decimal grandTotal = totalSubTotal + totalTaxAmount;

            return new BillSummaryResponse(
                session.TableSessionId,
                guestBills,
                totalSubTotal,
                appliedTaxes, 
                grandTotal
            );
        }
    }
}