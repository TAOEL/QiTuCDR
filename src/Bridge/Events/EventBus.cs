using System;
using QiTuCDR.Bridge.DTO;

namespace QiTuCDR.Bridge.Events
{
    public sealed class EventBus
    {
        public event EventHandler<EventDto>? EventPublished;

        public void Publish(EventDto dto)
        {
            EventPublished?.Invoke(this, dto);
        }
    }
}
