using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortalDP.Application.DTOs
{
    public class ClassCancellationDto
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public DateOnly ClassDate { get; set; }
        public int OriginalScheduleId { get; set; }
        public DateTime CancelledAt { get; set; }
        public string? Reason { get; set; }
        public ScheduleDto? OriginalSchedule { get; set; }
    }

    public class CreateClassCancellationDto
    {
        public DateOnly ClassDate { get; set; }
        public int OriginalScheduleId { get; set; }
        public string? Reason { get; set; }
    }
}
