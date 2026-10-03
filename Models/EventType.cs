using System;
using System.Collections.Generic;

namespace Test26.Models;

public partial class EventType
{
    public Guid EventTypeId { get; set; }

    public string EventTypeName { get; set; } = null!;

    public virtual ICollection<Event> Events { get; set; } = new List<Event>();
}
