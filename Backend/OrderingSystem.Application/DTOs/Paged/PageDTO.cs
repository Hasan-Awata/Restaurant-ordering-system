using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace OrderingSystem.Application.DTOs.Paged
{
    public class PageDTO
    {
        [Required(ErrorMessage = "Enter the page size")]
        [Range(1, 100, ErrorMessage = "Page size must be between 1 and 100.")]
        public int PageSize { get; set; }
        
        [Required(ErrorMessage = "Enter the page number")]
        [Range(1, int.MaxValue, ErrorMessage = "Page number must be at least 1.")]
        public int PageNumber { get; set; }
    }
}
