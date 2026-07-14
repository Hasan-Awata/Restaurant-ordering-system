using OrderingSystem.Application.DTOs;
using OrderingSystem.Application.Interfaces.TableInterfaces;
using OrderingSystem.Domain.Common;
using OrderingSystem.Domain.Entities;
using OrderingSystem.Domain.Enums;
using System.Security.Cryptography;

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
            var existingTable = await _tableRepository.GetByNumberAndFloorWithDeletedAsync(request.TableNumber, request.FloorNumber);

            if (existingTable != null)
            {
                if (!existingTable.IsDeleted)
                {
                    // The table exists and is active
                    return Result<TableResponse>.Failure(
                        $"Table {request.TableNumber} already exists on Floor {request.FloorNumber}.",
                        enErrorType.Validation);
                }

                // 2. Delegate to the private restore method
                var restoredResponse = await RestoreTableAsync(existingTable, request);
                return Result<TableResponse>.Success(restoredResponse);
            }

            // Generate unique QR Code payload
            string uniqueSegment = RandomNumberGenerator.GetHexString(4);
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

        public async Task<Result<TableResponse>> RegenerateQrCode(int tableId)
        {
            var table = await _tableRepository.GetTableByIdAsync(tableId);
            if (table == null)
            {
                return Result<TableResponse>.Failure("Table was not found.", enErrorType.NotFound);
            }

            // Regenerate the QR Code for security so old printed codes become invalid
            string newUniqueSegment = RandomNumberGenerator.GetHexString(4);
            table.QrCode = $"TBL-F{table.FloorNumber}-N{table.TableNumber}-{newUniqueSegment}".ToUpper();

            await _tableRepository.UpdateTableAsync(table);

            var response = new TableResponse(table.TableId, table.TableNumber, table.FloorNumber, table.QrCode, table.Status);
            return Result<TableResponse>.Success(response);
        }

        private async Task<TableResponse> RestoreTableAsync(Table existingTable, AddTableRequest request)
        {
            existingTable.IsDeleted = false;
            existingTable.Status = enTableStatus.Available;

            // Regenerate the QR Code for security so old printed codes become invalid
            string newUniqueSegment = RandomNumberGenerator.GetHexString(4);
            existingTable.QrCode = $"TBL-F{request.FloorNumber}-N{request.TableNumber}-{newUniqueSegment}".ToUpper();

            await _tableRepository.UpdateTableAsync(existingTable);

            return new TableResponse(
                existingTable.TableId,
                existingTable.TableNumber,
                existingTable.FloorNumber,
                existingTable.QrCode,
                existingTable.Status
            );
        }
    }
}