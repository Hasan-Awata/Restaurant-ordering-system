using OrderingSystem.Application.DTOs;
using OrderingSystem.Domain.Entities;
using OrderingSystem.Domain.Enums;
using System;

namespace OrderingSystem.Application.Mappers
{
    public static class SessionsMappers
    {
        public static TableSessionResponse ToResponse(this TableSession entity)
        {
            return new TableSessionResponse(
                entity.TableSessionId,
                entity.Table?.TableNumber ?? 0,
                entity.Status,
                entity.CreatedAt
            );
        }

        public static DeviceSessionResponse ToResponse(this DeviceSession entity)
        {
            return new DeviceSessionResponse(
                entity.DeviceSessionId,  
                entity.Role,
                entity.IsApproved
            );
        }

        public static SessionResponse ToResponse(this TableSession table, DeviceSession? device = null)
        {
            return new SessionResponse(table.ToResponse(), device?.ToResponse());
        }
    }
}