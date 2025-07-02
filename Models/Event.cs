using System;
using System.Collections.Generic;

namespace Project.Models;

public partial class Event
{
    public int EventId { get; set; }

    public string EventName { get; set; } = null!;

    public string Venue { get; set; } = null!;

    public DateTime StartingDate { get; set; }

    public DateTime EndingDate { get; set; }

    public int CreatedbyId { get; set; }

    public string Category { get; set; } = null!;

    public decimal Price { get; set; }

    public virtual User Createdby { get; set; } = null!;

    public virtual ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
}
