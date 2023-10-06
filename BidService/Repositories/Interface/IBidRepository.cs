using BidService.Entities;
using System.Collections;

namespace BidService.Repositories.Interface
{
    public interface IBidRepository
    {
        Task<List<Bid>> GetBids(); //Verilen tüm teklifleri listeler.
        Task<bool> Update(Bid bid); //Teklifi günceller.
        Task<bool> Delete(string id); //Teklifi siler.
        Task SendBid(Bid bid); //Yeni bir teklif oluşturur.
    }
}
