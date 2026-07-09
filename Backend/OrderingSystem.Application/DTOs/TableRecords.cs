using OrderingSystem.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderingSystem.Application.DTOs
{
    // Command Payloads
    public record AddTableRequest(int TableNumber, int Floor);
    public record UpdateTableRequest(int TableId, int TableNumber, int Floor, enTableStatus Status);

    // Query/Command Response Payloads
    public record TableResponse(int TableId, int TableNumber, int FloorNumber, string QrCode, enTableStatus Status);
    public record PendingTableResponse(int TableId, int TableNumber, int FloorNumber, string QrCode, enTableStatus Status, Guid TableSessionId);
}
