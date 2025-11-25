using System.Collections.Generic;
// 這裡不需要 Project_Photo.Models;

namespace Project_Photo.ViewModels
{
    public class StoreIndexViewModel
    {
        // 賣場頂部資訊
        public string StoreName { get; set; }
        public string StoreDescription { get; set; }
        public string StoreAvatarUrl { get; set; }

        // 賣場商品列表
        public List<StoreProductViewModel> Products { get; set; } = new List<StoreProductViewModel>();
    }
}