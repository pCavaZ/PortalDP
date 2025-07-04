using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortalDP.Application.DTOs
{
    public class CalendarDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public List<CalendarDayDto> Days { get; set; } = new();
    }

    public class CalendarDayDto
    {
        public DateOnly Date { get; set; }
        public List<ClassDto> Classes { get; set; } = new();
        public List<RecoveryClassDto> RecoveryClasses { get; set; } = new();
        public List<ClassCancellationDto> Cancellations { get; set; } = new();
        public bool IsAvailable { get; set; }
        public List<AvailableSlotDto> AvailableSlots { get; set; } = new();
    }

    public class ClassDto
    {
        public int Id { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string TimeRange { get; set; } = string.Empty;
        public bool IsCancelled { get; set; }
        public bool CanCancel { get; set; }
    }

    public class AvailableSlotDto
    {
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public string TimeRange { get; set; } = string.Empty;
        public int AvailableSpots { get; set; }
        public int TotalSpots { get; set; }
    }

}
