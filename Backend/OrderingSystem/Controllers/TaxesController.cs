using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using OrderingSystem.Application.DTOs;
using OrderingSystem.Application.DTOs.Paged;
using OrderingSystem.Application.Interfaces.TaxesInterfaces;
using OrderingSystem.WebApi.Controllers.Base;

namespace OrderingSystem.WebApi.Controllers
{
    [ApiController]
    [Route("api/taxes")]
    public class TaxesController : BaseController
    {
        private readonly ITaxCommandService _taxCommandService;
        private readonly ITaxQuery _taxQuery;

        public TaxesController(ITaxCommandService taxCommandService, ITaxQuery taxQuery)
        {
            _taxCommandService = taxCommandService;
            _taxQuery = taxQuery;
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpPost]
        public async Task<IActionResult> AddTax([FromBody] TaxRecords.AddTaxRequest request)
        {
            var result = await _taxCommandService.AddTaxAsync(request);
            return HandleCreatedResult(result, nameof(GetTaxById), new { id = result.Value?.TaxId });
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpPut]
        public async Task<IActionResult> UpdateTax([FromBody] TaxRecords.UpdateTaxRequest request)
        {
            var result = await _taxCommandService.UpdateTaxAsync(request);
            return HandleResult(result);
        }

        [Authorize(Policy = "AdminOnly")]
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTax(int id)
        {
            var result = await _taxCommandService.DeleteTaxAsync(id);
            return HandleResult(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetTaxById(int id)
        {
            var result = await _taxQuery.GetTaxByIdAsync(id);
            return HandleResult(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetAllTaxes([FromQuery] PageDTO page)
        {
            var result = await _taxQuery.GetAllTaxesAsync(page);
            return HandleResult(result);
        }
    }
}