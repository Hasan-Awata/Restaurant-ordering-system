using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace SpendWise.Application.DTOs.Paged
{
    public class PageDTO
    {
        [Required(ErrorMessage = "Enter the page size")]
        [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100.")]
        public int PageSize { get; set; }
        
        [Required(ErrorMessage = "Enter the page number")]
        [Range(1, int.MaxValue, ErrorMessage = "Page number must be at least 1.")]
        public int PageNumber { get; set; }

        // Optional search filters (nullable to keep backward compatibility)
        [Range(1, int.MaxValue, ErrorMessage = "TagId must be a positive integer.")]
        public int? TagId { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "CategoryId must be a positive integer.")]
        public int? CategoryId { get; set; }

        // TransactionType: 0 = Addition, 1 = Dedduction
        [Range(0, 1, ErrorMessage = "TransactionType must be 0 (Addition) or 1 (Dedduction).")]
        public int? TransactionType { get; set; }
    }
}
