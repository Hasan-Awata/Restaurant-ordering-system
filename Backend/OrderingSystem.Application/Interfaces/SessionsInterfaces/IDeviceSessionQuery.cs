using System;
using System.Collections.Generic;
using System.Text;
using OrderingSystem.Domain.Entities;

namespace OrderingSystem.Application.Interfaces.SessionsInterfaces
{
    public interface IDeviceSessionQuery
    {
        public Task<DeviceSession?> GetDeviceSessionByIdAsync(Guid deviceSessionId);
    }
}
