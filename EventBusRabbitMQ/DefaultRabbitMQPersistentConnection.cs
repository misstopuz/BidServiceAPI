using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using RabbitMQ.Client.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace EventBusRabbitMQ
{
    public class DefaultRabbitMQPersistentConnection : IRabbitMQPersistentConnection
    {
        private readonly IConnectionFactory _connectionFactory; //RabbitMq connection oluşturabilmek için RabbitMQ kütüphanesinden gelen IConnectionFactory interfacesden türeyen bir property oluşturalım.
        private IConnection _connection; //Connection işlemleri için IConnection tipinde connection propertysine ihtiyacımız var.

        //Bu connection yapısında Polly kütüphanesini de kullanacağız. Polly güvenilirlik sağlayan, mikroservislerde retry,fallback gibi mekanizmaları kurmamızı sağlayan .net tabanlı birk kütüphane.
        private readonly int _retryCount;
        private readonly ILogger<DefaultRabbitMQPersistentConnection> _logger;
        private bool _disposed;

        public DefaultRabbitMQPersistentConnection(IConnectionFactory connectionFactory, int retryCount, ILogger<DefaultRabbitMQPersistentConnection> logger)
        {
            _connectionFactory = connectionFactory;
            _retryCount = retryCount;
            _logger = logger;
        }

        public bool IsConnected //Connect olup olmadığıyla ilgili bilgi alabileceğimiz bir property. Connection property null değilse connection isopen true ise ve dispose olmamış bir durumda isek connection var demek istiyoruz.
        {
            get { return _connection != null && _connection.IsOpen && !_disposed; }
        }

        public bool TryConnect()
        {
            _logger.LogInformation("RabbitMQ Client is trying to connect");

            //Bir defa deneyip connect olmadığında işlemin durmasını istemediğimiz ve tekrar tekrar bağlantıyı denemesi için Polly frameworkünü kullanarak bir policy tanımlıyoruz.
            //BrokerUnreachableException => RabbitMQ'ya erişemediğinde fırlattığı exception.
            //Burada bir retrypolicy tanımladık. SocketException ya da BrokerUnreachableException aldığında benim verdiğim koşullarda bekle ve tekrar dene anlamında bir policy geliştirmiş olduk.
            var policy = RetryPolicy.Handle<SocketException>()
                .Or<BrokerUnreachableException>()
                .WaitAndRetry(_retryCount, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)), (ex, time) =>
                {
                    _logger.LogWarning(ex, "RabbitMQ Client could not connect after {TimeOut}s ({ExceptionMessage})", $"{time.TotalSeconds:n1}", ex.Message);
                });

            //İlgili policy üzerinden tanımladığımız connectiona yeni bir connection oluşturmasını sağlıyoruz.
            policy.Execute(() => {

                _connection = _connectionFactory.CreateConnection();
            });

            //Eğer hala işlemler başarılı değilse hata tiplerine göre callbcak methodları çağırıp dispose ya da yeniden bağlanma deniyoruz.
            if (IsConnected)
            {
                _connection.ConnectionShutdown += OnConnectionShutdown;
                _connection.CallbackException += OnCallbackException; 
                _connection.ConnectionBlocked += OnConnectionBlocked; 

                _logger.LogInformation("RabbitMQ Client acquired a persistent connection to '{HostName}' and is subscribed to failure events", _connection.Endpoint.HostName);

                return true;
            }
            else
            {
                _logger.LogCritical("FATAL ERROR: RabbitMQ connections could not be created and opened");

                return false;
            }
        }

        //Her methodda dispose ise dispose dönüyoruz. Değil ise TryConnect() methodu ile yeniden bağlanmaya çalışıyoruz.
        private void OnConnectionBlocked(object sender, ConnectionBlockedEventArgs e)
        {
            if (_disposed) return;

            _logger.LogWarning("A RabbitMQ connection is shutdown. Trying to re-connect...");

            TryConnect();
        }

        void OnCallbackException(object sender, CallbackExceptionEventArgs e)
        {
            if (_disposed) return;

            _logger.LogWarning("A RabbitMQ connection throw exception. Trying to re-connect...");

            TryConnect();
        }

        void OnConnectionShutdown(object sender, ShutdownEventArgs reason)
        {
            if (_disposed) return;

            _logger.LogWarning("A RabbitMQ connection is on shutdown. Trying to re-connect...");

            TryConnect();
        }

        //Qmanagement işlemlerini yaptığımız methodlar bulunmaktadır. Connect olup olmadığına bakıyoruz. Connection işlemleri başarılı ise CreateModel methodunu kullanarak IModel tipinde bir nesne geriye döndürdük.
        public IModel CreateModel()
        {
            if (!IsConnected)
            {
                throw new InvalidOperationException("No RabbitMQ connections are available to perform this action");
            }

            return _connection.CreateModel();
        }

       
        public void Dispose() //IDisposeble interfaceinden türettiğimiz için bir Dispose methodu oluşturuyoruz. Burada da nesne dispose olmuşsa geri döndürüyoruz.
                              //Değilse Dispose olduğunu belirtip connectionı dispose ediyoruz.
                              //Daha sonra farklı methodlardan eriştiğimizde açık connectionları kapatmak için bu methodu geliştirdik.
        {
            if (_disposed) return;

            _disposed = true;

            try
            {
                _connection.Dispose();
            }
            catch (IOException ex)
            {
                _logger.LogCritical(ex.ToString());
            }
        }
    }
}
