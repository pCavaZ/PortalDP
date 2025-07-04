using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortalDP.Domain.Entities
{
    public class TimeSlot
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [Range(1, 7)] // 1=Monday, 7=Sunday
        public int DayOfWeek { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        public int MaxCapacity { get; set; } = 10;

        public bool IsActive { get; set; } = true;
    }
}
