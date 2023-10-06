using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EventBusRabbitMQ.Events.Interfaces
{
    public abstract class IEvent
    {
        public Guid RequestId { get; private init; } //Her event oluştuğunda unique bir guid üzerinden track edebilmemizi sağlayacak.
        public DateTime CretionDate { get; private init; } //Ne zaman oluşturulduğuna dair bilgi.

        //Consructorda bunları init ediyoruz.
        public IEvent()
        {
            RequestId = Guid.NewGuid();
            CretionDate = DateTime.UtcNow;
        }
    }
}
