using System;
using System.Collections.Generic;
using System.Text;

namespace OrderingSystem.Application.DTOs
{
    public class MenuRecords
    {
        public record AddMenuItemRequest(
      int CategoryId,
      string NameAr,
      string NameEn,
      string Description,
      decimal Price,
      string ImageUrl,
      bool IsAvailable
  );

        
        public record UpdateMenuItemRequest(
            int MenuItemId,
            int CategoryId,
            string NameAr,
            string NameEn,
            string Description,
            decimal Price,
            string ImageUrl,
            bool IsAvailable
        );

      
        public record DeleteMenuItemRequest(int MenuItemId);

        public record MenuItemResponse(
            int MenuItemId,
         int CategoryId,
            string NameAr,
         string NameEn,
         string Description,
    decimal Price,
    string ImageUrl,
    bool IsAvailable
);

    }
}
