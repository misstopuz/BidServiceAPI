using BidService.Entities;
using MongoDB.Driver;

namespace BidService.Data.Interface
{
    public interface IBidContext
    {
        IMongoCollection<Bid> Bids { get; set; }
    }
}
