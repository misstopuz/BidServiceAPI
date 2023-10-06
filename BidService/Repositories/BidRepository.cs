using BidService.Data.Interface;
using BidService.Entities;
using BidService.Repositories.Interface;
using MongoDB.Driver;

namespace BidService.Repositories
{
    public class BidRepository : IBidRepository
    {
        private readonly IBidContext _context;
        public BidRepository(IBidContext context)
        {
                _context = context;
        }

        public async Task<bool> Delete(string id)
        {
            var filter = Builders<Bid>.Filter.Eq(m => m.Id,id);
            DeleteResult deleteResult = await _context.Bids.DeleteOneAsync(filter);

            return deleteResult.IsAcknowledged && deleteResult.DeletedCount > 0;
        }

        public async Task<List<Bid>> GetBids()
        {
            return await _context.Bids.Find(p => true).ToListAsync();
        }

        public async Task SendBid(Bid bid)
        {
            await _context.Bids.InsertOneAsync(bid);
        }

        public async Task<bool> Update(Bid bid)
        {
            var updateResult = await _context.Bids.ReplaceOneAsync(filter : g => g.Id == bid.Id,replacement:bid);
            return updateResult.IsAcknowledged && updateResult.ModifiedCount > 0;    
        }
    }
}
