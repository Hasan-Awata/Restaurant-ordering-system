using OrderingSystem.Application.DTOs;
using OrderingSystem.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderingSystem.Application.Interfaces.DeviceSessionInterfaces
{
    public interface IDeviceSessionCommand
    {
        Task<Result<TableSessionResponse>> DeactivateAsync(DeactivateSessionByAdminRequest request);
    }
}
