using Project_Photo.ViewModels;

namespace Project_Photo.Services
{
    public interface IProductService
    {
        SellerProductDetailViewModel GetProductDetail(long productId);
    }
}