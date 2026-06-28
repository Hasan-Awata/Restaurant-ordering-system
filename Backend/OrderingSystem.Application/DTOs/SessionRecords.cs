using OrderingSystem.Domain.Entities;
using OrderingSystem.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderingSystem.Application.DTOs
{
    // Command Payloads
    public record ProcessQrCodeRequest(string qrCode, Guid? deviceSessionId = null);
    public record ApproveJoiningSessionRequest(Guid deviceSessionId);
    public record DeactivateSessionByAdminRequest(int TableId);


    // Query/Command Response Payloads
    public record DeviceSessionResponse(Guid DeviceSessionId, enDeviceRole DeviceRole, bool IsApproved);
    public record TableSessionResponse(Guid TableSessionId, int TableNumber, enSessionStatus Status, DateTime CreatedAt);
    public record SessionResponse(TableSessionResponse TableSession, DeviceSessionResponse? DeviceSession);
}
