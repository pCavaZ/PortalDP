using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortalDP.Domain.Entities
{
    public class RecoveryClass
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int StudentId { get; set; }

        [Required]
        public DateOnly ClassDate { get; set; }

        [Required]
        public TimeSpan StartTime { get; set; }

        [Required]
        public TimeSpan EndTime { get; set; }

        [Required]
        public int OriginalCancellationId { get; set; }

        public DateTime BookedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public virtual Student Student { get; set; } = null!;
        public virtual ClassCancellation OriginalCancellation { get; set; } = null!;
    }
}
