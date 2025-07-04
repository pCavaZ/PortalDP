using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortalDP.Application.DTOs
{
    public class RecoveryClassDto
    {
        public int Id { get; set; }
        public int StudentId { get; set; }
        public DateOnly ClassDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int OriginalCancellationId { get; set; }
        public DateTime BookedAt { get; set; }
        public string TimeRange { get; set; } = string.Empty;
    }

    public class CreateRecoveryClassDto
    {
        public DateOnly ClassDate { get; set; }
        public TimeSpan StartTime { get; set; }
        public TimeSpan EndTime { get; set; }
        public int OriginalCancellationId { get; set; }
    }
}
