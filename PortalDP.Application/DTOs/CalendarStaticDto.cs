using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortalDP.Application.DTOs
{
    public class CalendarStatisticsDto
    {
        public int Year { get; set; }
        public int Month { get; set; }
        public int TotalClasses { get; set; }
        public int CancelledClasses { get; set; }
        public int RecoveryClasses { get; set; }
        public decimal UtilizationRate { get; set; }
        public Dictionary<string, int> ClassesByDay { get; set; } = new();
        public Dictionary<string, int> ClassesByTimeSlot { get; set; } = new();
    }

}
