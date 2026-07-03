using OrderingSystem.Application.DTOs;
using OrderingSystem.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace OrderingSystem.Application.Mappers
{
    public static class CategoryMappers
    {
        public static Category ToEntity(CategoriesRecords.AddCategoryRequest request)
        {
            if (request == null)
            {
                return null;
            }

            return new Category
            {
                NameAr = request.NameAr,
                NameEn = request.NameEn,
                IsAvailable = request.IsAvailable
            };
        }

        public static CategoriesRecords.CategoryResponse ToResponse(this Category category)
        {
            if (category == null)
            {
                return null;
            }

            return new CategoriesRecords.CategoryResponse(
                category.CategoryId,
                category.NameAr,
                category.NameEn,
                category.IsAvailable
            );
        }
    }
}