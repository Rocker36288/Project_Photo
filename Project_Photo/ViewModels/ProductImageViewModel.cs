using System.ComponentModel.DataAnnotations;

namespace Project_Photo.ViewModels
{
    public class ProductImageViewModel
    {
        public int ProductImageId { get; set; }
        public string ImageUrl { get; set; }
        public bool IsMainImage { get; set; }
        public int DisplayOrder { get; set; }
    }
}