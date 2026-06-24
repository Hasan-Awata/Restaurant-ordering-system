using OrderingSystem.Application.DTOs;
using OrderingSystem.Application.Interfaces.TableSessionInterfaces;
using OrderingSystem.Application.Mappers;
using OrderingSystem.Domain.Enums;
using System;
using System.Threading.Tasks;

namespace OrderingSystem.Application.Services
{
    public class TableSessionCommandService : ITableSessionCommand
    {
        private readonly ITableSessionRepository _tableSessionRepository;

        public TableSessionCommandService(ITableSessionRepository tableSessionRepository) => _tableSessionRepository = tableSessionRepository;

        public async Task<SessionResponse> ActivateAsync(ActivateSessionRequest request)
        {
            var table = await _tableSessionRepository.GetTableWithActiveSessionAsync(request.TableId);
            if (table == null) throw new Exception("Table not found.");
            if (table.Status != enTableStatus.Available) throw new Exception("Table is already occupied or billing.");

            // Core domain rules logic execution[cite: 3]
            table.Status = enTableStatus.Occupied;
            string secureToken = Guid.NewGuid().ToString("N"); // Generate 60s pairing token boundary[cite: 3]

            // Manual mapping initiated using centralized Application extensions
            var session = request.ToEntity(secureToken);
            session.Table = table;

            await _tableSessionRepository.SaveSessionAsync(session);

            return session.ToResponse();
        }
    }
}