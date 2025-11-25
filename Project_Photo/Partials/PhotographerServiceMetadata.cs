using Project_Photo.Models;
using System.ComponentModel.DataAnnotations;

namespace Project_Photo.Metadata
{
    internal class PhotographerServiceMetadata
    {
        [Display(Name = "攝影服務編號")]
        public int PhotographerServiceId { get; set; }

        [Display(Name = "攝影師/工作室")]
        public int PhotographerId { get; set; }
        [Display(Name = "攝影服務類型")]
        public int ServiceTypeId { get; set; }

        [Display(Name = "服務類型名稱")]
        public string ?ServiceName { get; set; }
        [Display(Name = "服務描述")]
        public string? Description { get; set; }
        [DisplayFormat(DataFormatString = "{0:C}")]
        [Display(Name = "服務基本價格")]
        public decimal BasePrice { get; set; }
        [Display(Name = "服務時長(分鐘)")]
        public int Duration { get; set; }
        [Display(Name = "最多修圖次數")]
        public int? MaxRevisions { get; set; }
        [Display(Name = "交件天數")]
        public int? DeliveryDays { get; set; }
        [Display(Name = "包含照片數")]
        public int? IncludedPhotos { get; set; }
        [Display(Name = "額外服務說明")]
        public string ?AdditionalServices { get; set; }
        [Display(Name = "是否啟用")]
        public bool IsActive { get; set; }
        [Display(Name = "建立時間")]
        public DateTime CreatedAt { get; set; }
        [Display(Name = "攝影師/工作室")]
        public virtual Photographer ?Photographer { get; set; }
        [Display(Name = "攝影服務類型")]
        public virtual ServiceType ?ServiceType { get; set; }
    }
}