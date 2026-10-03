using System;
using System.Collections.Generic;

namespace Test26.Models;

public partial class RelatedTable
{
    public Guid RelatedTableId { get; set; }

    public string RelatedTableName { get; set; } = null!;

    public virtual ICollection<Event> Events { get; set; } = new List<Event>();
}
