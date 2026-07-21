using OrderingSystem.Application.DTOs;
using OrderingSystem.Application.DTOs.Paged;
using OrderingSystem.Domain.Common;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderingSystem.Application.Interfaces.OrdersInterfaces
{
    public interface IOrderQuery
    {
        Task<Result<PagedResponse<OrderRecords.OrderResponse>>> GetPendingOrdersAsync(PageDTO page);
    }
}
