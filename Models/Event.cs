using System;
using System.Collections.Generic;

namespace Test26.Models;

public partial class Event
{
    public Guid EventId { get; set; }

    public Guid EventTypeId { get; set; }

    public Guid RelatedTableId { get; set; }

    public Guid RelatedId { get; set; }

    public Guid? UserId { get; set; }

    public string Description { get; set; } = null!;

    public DateTime Date { get; set; }

    public virtual EventType EventType { get; set; } = null!;

    public virtual RelatedTable RelatedTable { get; set; } = null!;
}
