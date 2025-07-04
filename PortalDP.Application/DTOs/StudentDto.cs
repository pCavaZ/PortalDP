using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PortalDP.Application.DTOs
{
    public class StudentDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string DNI { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<ScheduleDto> Schedules { get; set; } = new();
    }

    public class CreateStudentDto
    {
        public string Name { get; set; } = string.Empty;
        public string DNI { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public List<CreateScheduleDto> Schedules { get; set; } = new();
    }

    public class UpdateStudentDto
    {
        public string Name { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public bool IsActive { get; set; }
    }
}
