using System;
using System.Collections.Generic;
using Test26.Models;

namespace Test26.DTOs;

public partial class EditDates
{
    public Guid ID { get; set; }
    public DateTime? CreateDate { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? EndDate { get; set; }
}
