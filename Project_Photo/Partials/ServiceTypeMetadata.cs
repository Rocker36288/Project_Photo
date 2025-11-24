using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace Project_Photo.Metadata
{
    internal class ServiceTypeMetadata
    {
        [Display(Name = "服務類型名稱")]
        public string? ServiceName { get; set; }


        [Display(Name = "服務類型描述")]
        public string? Description { get; set; }


        [Display(Name = "前台圖示")]
        public string? IconUrl { get; set; }

        //[Display(Name = "成列的順序")]
        //public int DisplayOrder { get; set; }
       
        [Display(Name = "是否啟用")]
        public bool IsActive { get; set; }

    }
}