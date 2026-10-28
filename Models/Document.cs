using System;
using System.Collections.Generic;

namespace Test26.Models;

public partial class Document
{
    public Guid DocumentId { get; set; }

    public string FileName { get; set; } = null!;

    public string StoredFileName { get; set; } = null!;

    public string FilePath { get; set; } = null!;

    public string ContentType { get; set; } = null!;

    public long FileSize { get; set; }

    public virtual ICollection<TaskDocument> TaskDocuments { get; set; } = new List<TaskDocument>();
}
