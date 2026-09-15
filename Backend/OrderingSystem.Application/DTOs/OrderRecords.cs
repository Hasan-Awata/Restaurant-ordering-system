using OrderingSystem.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace OrderingSystem.Application.DTOs
{
    public class OrderRecords
    {
        public record CreateOrderRequest(
            [Required] int TableNumber,
            [Required] Guid TableSessionId,
            [Required] List<OrderItemRequest> Items);

        public record OrderItemRequest(
            [Required] int MenuItemId,
            [Required, Range(1, 100, ErrorMessage = "Quantity must be a positive integer less than 100.")] int Quantity,
            [StringLength(400, ErrorMessage = "Notes must not exceed 400 characters.")] string Notes);

        public record OrderResponse(int OrderId, int TableNumber, decimal TotalAmount, enOrderStatus OrderStatus, DateTime CreatedAt, List<OrderItemResponse> Items);
        public record OrderItemResponse(int MenuItemId, string NameEn, string NameAr, int Quantity, decimal UnitPrice, string Notes);

        public record HistoricalBillResponse(
            int OrderId,
            int TableNumber,
            string TableSessionId,
            string Status,
            DateTime CreatedAt,
            decimal TotalSubTotal,
            decimal GrandTotal,
            List<BillItemResponse> Items,
            List<AppliedTaxResponse> AppliedTaxes
        );

        public record BillItemResponse(
            int MenuItemId,
            string NameAr,
            string NameEn,
            int Quantity,
            decimal UnitPrice,
            decimal TotalPrice
        );

    }

}