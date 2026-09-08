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
                .Include(s => s.Orders)
                    .ThenInclude(o => o.OrderItems)
                        .ThenInclude(oi => oi.MenuItem)
                .FirstOrDefaultAsync(s => s.TableSessionId == tableSessionId);

            if (session == null)
                return null;

            var activeTaxes = await _context.Taxes
                .AsNoTracking()
                .Where(t => t.IsActive && !t.IsDeleted)
                .ToListAsync();

            var validOrders = session.Orders.Where(o => o.OrderStatus != enOrderStatus.Cancelled).ToList();

            var guestBills = new List<GuestBillResponse>();
            decimal totalSubTotal = 0;

            foreach (var device in session.Devices)
            {
                var deviceOrders = validOrders.Where(o => o.DeviceSessionId == device.DeviceSessionId).ToList();

                var billItems = deviceOrders
                    .SelectMany(o => o.OrderItems)
                    .GroupBy(oi => oi.MenuItemId)
                    .Select(g =>
                    {
                        var first = g.First();
                        var totalQty = g.Sum(oi => oi.Quantity);
                        return new BillItemResponse(
                            first.MenuItemId,
                            first.MenuItem?.NameEn ?? "Deleted Item",
                            first.MenuItem?.NameAr ?? "Deleted Item",
                            totalQty,
                            first.UnitPrice,
                            totalQty * first.UnitPrice
                        );
                    })
                    .ToList();

                decimal guestSubTotal = billItems.Sum(i => i.TotalPrice);
                totalSubTotal += guestSubTotal;

                guestBills.Add(new GuestBillResponse(
                    device.DeviceSessionId,
                    device.Role,
                    billItems,
                    guestSubTotal
                ));
            }

            var appliedTaxes = new List<AppliedTaxResponse>();
            decimal totalTaxAmount = 0;

            var uniqueGuestsCount = session.Devices.Count;
            var totalItemsCount = validOrders.SelectMany(o => o.OrderItems).Sum(oi => oi.Quantity);

            foreach (var tax in activeTaxes)
            {
                decimal taxAmount = 0;

                if (tax.TaxType == enTaxType.Percentage)
                {
                    if (tax.TaxScope == enTaxScope.PerBill)
                        taxAmount = totalSubTotal * (tax.Amount / 100m);
                }
                else if (tax.TaxType == enTaxType.FlatRate)
                {
                    if (tax.TaxScope == enTaxScope.PerBill)
                        taxAmount = tax.Amount;
                    else if (tax.TaxScope == enTaxScope.PerGuest)
                        taxAmount = tax.Amount * uniqueGuestsCount;
                    else if (tax.TaxScope == enTaxScope.PerItem)
                        taxAmount = tax.Amount * totalItemsCount;
                }

                if (taxAmount > 0)
                {
                    appliedTaxes.Add(new AppliedTaxResponse(tax.NameEn, tax.NameAr, Math.Round(taxAmount, 2)));
                    totalTaxAmount += taxAmount;
                }
            }

            return new BillSummaryResponse(
                tableSessionId,
                guestBills,
                totalSubTotal,
                appliedTaxes,
                totalSubTotal + totalTaxAmount
            );
        }
        public async Task<SessionPollingResponse?> GetSessionPollingStatusAsync(Guid tableSessionId, Guid deviceSessionId)
        {
            // استعلام خفيف جداً ومثالي للـ Polling
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