using BidService.Data.Interface;
using BidService.Entities;
using BidService.Settings;
using MongoDB.Driver;

namespace BidService.Data
{
    public class BidContext : IBidContext
    {

        public BidContext(IBidDatabaseSettings settings)
        {
            var client = new MongoClient(settings.ConnectionString); //Client tanımlıyoruz..
            var database = client.GetDatabase(settings.DatabaseName); // Client üzerinden database bilgisini al..

            Bids = database.GetCollection<Bid>(settings.CollectionName); // Databaseden gelecek olan collection bilgisini al. Bid entitiesi üzerinden konuşacak diyoruz.

        }
        public IMongoCollection<Bid> Bids { get; set; }
    }
}
