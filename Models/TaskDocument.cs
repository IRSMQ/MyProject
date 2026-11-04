using System;
using System.Collections.Generic;

namespace Test26.Models;

public partial class TaskDocument
{
    public Guid TaskDocumentId { get; set; }

    public Guid TaskId { get; set; }

    public Guid DocumentId { get; set; }

    public bool IsDeleted { get; set; }

    public virtual Document Document { get; set; } = null!;

    public virtual TaskManagement Task { get; set; } = null!;
}
