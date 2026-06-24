using System;
using System.Collections.Generic;
using System.Text;

namespace OrderingSystem.Application.DTOs
{
    // Command Payloads
    public record ActivateSessionRequest(int TableId, int WaiterId);

    // Query/Command Response Payloads
    public record SessionResponse(int SessionId, int TableNumber, string Status, string SessionToken, DateTime CreatedAt);
}
