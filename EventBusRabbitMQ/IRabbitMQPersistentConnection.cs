using RabbitMQ.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventBusRabbitMQ
{
    public interface IRabbitMQPersistentConnection : IDisposable //Connection sınıflarını best practice olarak IDisposable'dan türetmemiz daha doğru olacaktır.
    {
        bool IsConnected { get; } //Bu property sayesinde connect olup olmadığını, anlık olarak connectionın ne durumda olduğunu kontrol edebileceğimiz property. 
        bool TryConnect(); //Bu method sayesinde connectionı başlatacağız.

        IModel CreateModel(); // Bu method sayesinde qmanagement işlemlerimizi yapmamızı sağlayacak nesneyi oluşturacağız.
    }
}
