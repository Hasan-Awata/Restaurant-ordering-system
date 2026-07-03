using System;
using System.Collections.Generic;
using System.Text;

namespace OrderingSystem.Application.DTOs
{
    public class CategoriesRecords
    {
        public record AddCategoryRequest(
            string NameAr,
            string NameEn,
            bool IsAvailable
        );

         public record UpdateCategoryRequest(
            int CategoryId,
            string NameAr,
            string NameEn,
            bool IsAvailable
        );

    
        public record DeleteCategoryRequest(int CategoryId);
        public record CategoryResponse(
            int CategoryId,
            string NameAr,
            string NameEn,
            bool IsAvailable
        );

    }
}
