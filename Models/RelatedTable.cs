using System;
using System.Collections.Generic;

namespace Test26.Models;

public partial class RelatedTable
{
    public Guid RelatedTableId { get; set; }

    public string RelatedTableName { get; set; } = null!;

    public virtual ICollection<Log> Logs { get; set; } = new List<Log>();
}
