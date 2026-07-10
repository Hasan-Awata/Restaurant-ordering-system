using OrderingSystem.Application.DTOs;
using OrderingSystem.Application.Interfaces.TableInterfaces;
using OrderingSystem.Domain.Common;
using OrderingSystem.Domain.Entities;
using OrderingSystem.Domain.Enums;

namespace OrderingSystem.Application.Services
{
    public class TableCommandService : ITableCommandService
    {
        private readonly ITableRepository _tableRepository;

        public TableCommandService(ITableRepository tableRepository)
        {
            _tableRepository = tableRepository;
        }

        public async Task<Result<TableResponse>> AddTableAsync(AddTableRequest request)
        {
            // Business Rule: Check if table number on that floor already exists
            if (await _tableRepository.ExistsAsync(request.TableNumber, request.FloorNumber))
            {
                return Result<TableResponse>.Failure($"Table {request.TableNumber} already exists on Floor {request.FloorNumber}.", enErrorType.Validation);
            }

            // Generate unique QR Code payload
            string uniqueSegment = Guid.CreateVersion7().ToString()[..8];
            string generatedQrCode = $"TBL-F{request.FloorNumber}-N{request.TableNumber}-{uniqueSegment}".ToUpper();

            var newTable = new Table
            {
                TableNumber = request.TableNumber,
                FloorNumber = request.FloorNumber,
                QrCode = generatedQrCode,
                Status = enTableStatus.Available
            };

            await _tableRepository.AddTableAsync(newTable);

            var response = new TableResponse(newTable.TableId, newTable.TableNumber, newTable.FloorNumber, newTable.QrCode, newTable.Status);
            
            return Result<TableResponse>.Success(response);
        }

        public async Task<Result<TableResponse>> UpdateTableAsync(UpdateTableRequest request)
        {
            var table = await _tableRepository.GetTableByIdAsync(request.TableId);
            if (table == null)
            {
                return Result<TableResponse>.Failure("Table not found.", enErrorType.NotFound);
            }

            // If changing floor/number, ensure no conflict
            if ((table.TableNumber != request.TableNumber || table.FloorNumber != request.FloorNumber) &&
                await _tableRepository.ExistsAsync(request.TableNumber, request.FloorNumber))
            {
                return Result<TableResponse>.Failure("A table with this number already exists on this floor.", enErrorType.Validation);
            }

            table.TableNumber = request.TableNumber;
            table.FloorNumber = request.FloorNumber;
            table.Status = request.Status;

            // Note: We deliberately do NOT update the QR code here, as the physical QR sticker on the table shouldn't break.

            await _tableRepository.UpdateTableAsync(table);

            var response = new TableResponse(table.TableId, table.TableNumber, table.FloorNumber, table.QrCode, table.Status);
            return Result<TableResponse>.Success(response);
        }

        public async Task<Result> DeleteTableAsync(int tableId)
        {
            var table = await _tableRepository.GetTableByIdAsync(tableId);
            if (table == null)
            {
                return Result.Failure("Table not found.", enErrorType.NotFound);
            }

            // Optional Business Rule: Check if table has active sessions before deleting
            if (table.Status != enTableStatus.Available)
            {
                return Result.Failure("Cannot delete a table that is currently occupied or billing.", enErrorType.Validation);
            }

            await _tableRepository.DeleteTableAsync(table);
            return Result.Success();
        }
    }
}