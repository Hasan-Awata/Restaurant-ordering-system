using OrderingSystem.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace OrderingSystem.Application.DTOs
{
    // Command Payloads
    public record AddTableRequest(
        [Required] int TableNumber, 
        [Required] int FloorNumber);
    public record UpdateTableRequest(
        [Required] int TableId, 
        [Required] int TableNumber, 
        [Required] int FloorNumber,
        [Required] enTableStatus Status);

    // Query/Command Response Payloads
    public record TableResponse(int TableId, int TableNumber, int FloorNumber, string QrCode, enTableStatus Status);
    public record PendingTableResponse(int TableId, int TableNumber, int FloorNumber, string QrCode, enTableStatus Status, Guid TableSessionId);
}
