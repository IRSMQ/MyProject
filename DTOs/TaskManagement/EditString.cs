using System;
using System.Collections.Generic;
using Test26.Models;

namespace Test26.DTOs;

public partial class EditString
{
    public Guid TaskID { get; set; }
    public string Text { get; set; } = null!;
}
