using OrderingSystem.Application.DTOs;
using System;
using System.Collections.Generic;
using System.Text;
using OrderingSystem.Domain.Common;

namespace OrderingSystem.Application.Interfaces.TableSessionInterfaces
{
    public interface ISessionCommandService
    {
        public Task<Result<SessionResponse>> ProcessTableQrCodeAsync(ProcessQrCodeRequest request);
        public Task<Result<SessionResponse>> ApproveJoiningRequestAsync(ApproveJoiningSessionRequest request);
    }
}
