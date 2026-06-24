using OrderingSystem.Application.DTOs;
using OrderingSystem.Domain.Entities;
using OrderingSystem.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderingSystem.Application.Mappers
{
    public static class SessionsMappers
    {
        // For Write Path (Maps Request DTO -> Domain Entity)
        public static TableSession ToEntity(this ActivateSessionRequest request, string secureToken)
        {
            return new TableSession
            {
                TableId = request.TableId,
                WaiterId = request.WaiterId,
                SessionToken = secureToken,
                Status = enSessionStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
        }

        // For Read Path (Maps Domain Entity -> Response Record)
        public static SessionResponse ToResponse(this TableSession entity)
        {
            return new SessionResponse(
                entity.SessionId,
                entity.Table?.TableNumber ?? 0,
                entity.Status.ToString(),
                entity.SessionToken,
                entity.CreatedAt
            );
        }
    }
}
