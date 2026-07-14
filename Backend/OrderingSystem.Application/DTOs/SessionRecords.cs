using OrderingSystem.Domain.Entities;
using OrderingSystem.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace OrderingSystem.Application.DTOs
{
    // Command Payloads
    public record ProcessQrCodeRequest([Required] string qrCode);
    public record ApproveJoiningSessionRequest([Required] Guid deviceSessionId);
    public record ActivateTableSessionRequest([Required] Guid tableSessionId); 
    public record DeactivateSessionByAdminRequest([Required] int TableId);
    public record RequestBillRequest([Required] Guid TableSessionId);
    public record ApproveBillRequest([Required] Guid TableSessionId);


    // Query/Command Response Payloads
    public record DeviceSessionResponse(Guid DeviceSessionId, enDeviceRole DeviceRole, bool IsApproved);
    public record TableSessionResponse(Guid TableSessionId, int TableNumber, enSessionStatus Status, DateTime CreatedAt);
    public record SessionResponse(TableSessionResponse TableSession, DeviceSessionResponse? DeviceSession);
    public record BillItemResponse(int MenuItemId, string NameEn, string NameAr, int Quantity, decimal UnitPrice, decimal TotalPrice);
    public record GuestBillResponse(Guid DeviceSessionId, enDeviceRole Role, List<BillItemResponse> Items, decimal SubTotal);
    public record BillSummaryResponse(
        Guid TableSessionId,
        List<GuestBillResponse> GuestBills,
        decimal TotalSubTotal,
        decimal TaxAmount,
        decimal GrandTotal
    );
}
