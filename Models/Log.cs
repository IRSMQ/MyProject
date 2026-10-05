using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Test26.Models;

public partial class Log : ISoftDeletable
{
    public Guid? LogId { get; set; } = Guid.NewGuid();

    [RegularExpression("^(Craete|Edit|Delete|Read)", ErrorMessage = "Valid LogType")]
    public string LogType { get; set; } = null!;

    public string RelatedTableName { get; set; } = null!;

    public Guid RelatedId { get; set; }

    public Guid? UserId { get; set; }

    public DateTime Date { get; set; } = DateTime.Now;

    public bool SoftDelete { get; set; } = false;

    public virtual User? User { get; set; }
}
