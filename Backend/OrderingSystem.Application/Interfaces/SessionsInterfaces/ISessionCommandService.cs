using OrderingSystem.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;
using OrderingSystem.Domain.Common;

namespace OrderingSystem.Application.Interfaces.TableSessionInterfaces
{
    public interface ISessionCommandService
    {
        public Task<Result<SessionResponse>> ProcessTableQrCodeAsync(string qrCode, Guid? deviceSessionId = null);
        public Task<Result<SessionResponse>> ApproveJoiningRequestAsync(ApproveJoiningSessionRequest request, Guid deviceSessionId);
        public Task<Result<TableSessionResponse>> ActivateTableSessionAsync(ActivateTableSessionRequest request);
        public Task<Result> DeactivateTableSessionAsync(Guid tableSessionId);
        public Task<Result<SessionResponse>> EndTableSessionAsync(Guid tableSessionId);
        public Task<Result> RequestBillAsync(Guid tableSessionId, Guid deviceSessionId);
        public Task<Result> ApproveBillAsync(Guid tableSessionId);
    }
}
