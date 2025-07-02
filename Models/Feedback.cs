using System;
using System.Collections.Generic;

namespace Project.Models;

public partial class Feedback
{
    public int FeedbackId { get; set; }

    public int UserId { get; set; }

    public int EventId { get; set; }

    public int Rating { get; set; }

    public string Comments { get; set; } = null!;

    public DateTime FeedbackDate { get; set; }

    public virtual Event Event { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
