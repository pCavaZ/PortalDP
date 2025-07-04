using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortalDP.Domain.Entities
{
    public class ClassCancellation
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StudentId { get; set; }

        [Required]
        public DateOnly ClassDate { get; set; }

        [Required]
        public int OriginalScheduleId { get; set; }

        public DateTime CancelledAt { get; set; } = DateTime.UtcNow;

        [MaxLength(500)]
        public string? Reason { get; set; }

        // Navigation properties
        public virtual Student Student { get; set; } = null!;
        public virtual Schedule OriginalSchedule { get; set; } = null!;
        public virtual ICollection<RecoveryClass> RecoveryClasses { get; set; } = new List<RecoveryClass>();
    }
}
