using EventBusRabbitMQ.Events.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace EventBusRabbitMQ.Producer
{
    public class EventBusRabbitMQProducer
    {
        private readonly IRabbitMQPersistentConnection _persistentConnection; //Producer üzerinden event gönderebilmek için RabbitMQ'ya bağlanmamız gerekiyor. O yüzden bir connection nesnesi oluşturuyoruz.
        private readonly ILogger<EventBusRabbitMQProducer> _logger;
        private readonly int _retryCount; //Polly framework ile implementasyon yapacağımız için bir retryCount tanımlıyoruz.

        public EventBusRabbitMQProducer(IRabbitMQPersistentConnection persistentConnection, ILogger<EventBusRabbitMQProducer> logger, int retryCount = 5)
        {
            _persistentConnection = persistentConnection;
            _logger = logger;
            _retryCount = retryCount;
        }

        //Producerın amacı bir event üretip bu eventi de queue ya bırakmaktır. Bu yüzden bir Publish methodu oluşturuyoruz. queuename ve event tipinde nesne beklediğimizi söylüyoruz.
        public void Publish(string queueName, IEvent @event)
        {
            //Connection durumunu kontrol ediyoruz. Connect değilse TryConnect ile conenct olmaya zorluyoruz.
            if (!_persistentConnection.IsConnected)
            {
                _persistentConnection.TryConnect();
            }

            //Policy tanımlıyoruz ve bu policy üzerinde publish işlemini yapmaya zorluyoruz.
            var policy = RetryPolicy.Handle<BrokerUnreachableException>()
            .Or<SocketException>()
            .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
            {
                _logger.LogWarning(ex, "Could not publish event: {EventId} after {Timeout}s ({ExceptionMessage})", @event.RequestId, $"{time.TotalSeconds:n1}", ex.Message);
            });

            //Connection nesnesi üzerinden CreateModel ı üretiyoruz. Ve daha sonra bunu kullanarak publish işlemlerini gerçekleştiriyor olacağız.
            using (var channel = _persistentConnection.CreateModel())
            {
                channel.QueueDeclare(queueName, durable: false, exclusive: false, autoDelete: false, arguments: null); // Queueyu declare ettik.
                var message = JsonConvert.SerializeObject(@event); //Mesajı jsona dmnüştürdük.
                var body = Encoding.UTF8.GetBytes(message); //Mesajı byte haline getirdik.

                policy.Execute(() =>
                {
                    IBasicProperties properties = channel.CreateBasicProperties(); //Channelın ihtiyacı olan propertyleri tanımlıyoruz.
                    properties.Persistent = true;
                    properties.DeliveryMode = 2;

                    channel.ConfirmSelect();
                    //channel parametresi üzerinden BasicPublish methodu ile ismini erdiğimiz Queueya mesajımızı publish ediyoruz.
                    channel.BasicPublish(
                        exchange: "",
                        routingKey: queueName,
                        mandatory: true,
                        basicProperties: properties,
                        body: body);
                    channel.WaitForConfirmsOrDie();

                    channel.BasicAcks += (sender, eventArgs) =>
                    {
                        Console.WriteLine("Sent RabbitMQ");
                        //implement ack handle
                    };
                });
            }
        }

    }
}
