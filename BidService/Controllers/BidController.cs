using BidService.Entities;
using BidService.Repositories.Interface;
using EventBusRabbitMQ.Core;
using EventBusRabbitMQ.Events;
using EventBusRabbitMQ.Producer;
using Microsoft.AspNetCore.Mvc;
using System.Net;

namespace BidService.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class BidController : ControllerBase
    {
        #region Variables
        private readonly IBidRepository _bidRepository;
        private readonly ILogger<BidController> _logger;
        private readonly EventBusRabbitMQProducer _eventBus;

        #endregion
        #region Constructor
        public BidController(IBidRepository bidRepository,ILogger<BidController> logger, EventBusRabbitMQProducer eventBus)
        {
            _bidRepository = bidRepository;
            _logger = logger;
            _eventBus = eventBus;
        }
        #endregion

        #region Crud Actions

        [HttpGet] //Get isteği olduğunu belirtiyoruz.
        [ProducesResponseType(typeof(Bid),(int)HttpStatusCode.OK)] //Geriye dönüş bilgisini belirtiyoruz.
        public async Task<ActionResult<List<Bid>>> GetBids()
        {
            var bids = await _bidRepository.GetBids();
            return Ok(bids);
        }

        [HttpPut] //Put isteği olduğunu belirtiyoruz.
        [ProducesResponseType(typeof(Bid), (int)HttpStatusCode.OK)] //Geriye dönüş bilgisini belirtiyoruz.
        public async Task<IActionResult> UpdateBid([FromBody] Bid bid)
        {
            return Ok(await _bidRepository.Update(bid));
        }

        [HttpDelete("{id:length(24)}")] //Delete isteği olduğunu belirtiyoruz.Bid nesnesi içindeki Id bilgisi ObjectId olduğu için 24 karakter bilgisini burada belirtiyoruz.
        [ProducesResponseType(typeof(Bid), (int)HttpStatusCode.OK)] //Geriye dönüş bilgisini belirtiyoruz.
        public async Task<IActionResult>DeleteBidById(string id)
        {
            return Ok(await _bidRepository.Delete(id));
        }

        [HttpPost] //Post isteği olduğunu belirtiyoruz.
        [ProducesResponseType((int)HttpStatusCode.OK)]  //Geriye dönüş bilgisini belirtiyoruz.
        public async Task<ActionResult> SendBid([FromBody] Bid bid)
        {
            await _bidRepository.SendBid(bid);

            return Ok();
        }

        #endregion

        #region Test Event

        [HttpPost("TestEvent")]
        public ActionResult<BidCretaeEvent> TestEvent()
        {
            BidCretaeEvent eventMessage = new BidCretaeEvent();
            eventMessage.Id = "dummy1";
            eventMessage.ProductId = "dummy_product_1";
            eventMessage.Price = 100;
            eventMessage.SellerUserName = "test@test.com";
            eventMessage.CreatedAt = DateTime.Now;

            try
            {
                _eventBus.Publish(EventBusConstants.BidCreateQueue, eventMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERROR Publishing integration event: {EventId} from {AppName}", eventMessage.Id, "Sourcing");
                throw;
            }

            return Accepted(eventMessage);
        }

        #endregion

    }
}
