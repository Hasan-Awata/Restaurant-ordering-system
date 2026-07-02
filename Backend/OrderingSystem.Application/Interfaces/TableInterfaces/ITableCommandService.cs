using OrderingSystem.Application.DTOs;
using OrderingSystem.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderingSystem.Application.Interfaces.TableInterfaces
{
    public interface ITableCommandService
    {
        Task<Result<TableResponse>> AddTableAsync(AddTableRequest request);
        Task<Result<TableResponse>> UpdateTableAsync(UpdateTableRequest request);
        Task<Result> DeleteTableAsync(int tableId);
    }
}
