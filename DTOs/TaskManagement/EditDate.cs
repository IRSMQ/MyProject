using System;
using System.Collections.Generic;
using Test26.Models;

namespace Test26.DTOs;

public partial class EditDate
{
    public Guid ID { get; set; }
    public DateTime? Date { get; set; }
}
