using OrderingSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderingSystem.Application.Interfaces.SessionsInterfaces
{
    public interface IDeviceSessionRepository
    {
        public Task AddSessionAsync(DeviceSession session);
        public Task UpdateDeviceSessionAsync(DeviceSession session);
    }
}
