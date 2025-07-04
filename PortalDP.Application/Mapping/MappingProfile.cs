using AutoMapper;
using PortalDP.Application.DTOs;
using PortalDP.Domain.Entities;

namespace PortalDP.Application.Mapping
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            CreateStudentMappings();
            CreateScheduleMappings();
            CreateClassCancellationMappings();
            CreateRecoveryClassMappings();
        }

        private void CreateStudentMappings()
        {
            // Student -> StudentDto
            CreateMap<Student, StudentDto>()
                .ForMember(dest => dest.Schedules, opt => opt.MapFrom(src => src.Schedules.Where(s => s.IsActive)));

            // CreateStudentDto -> Student
            CreateMap<CreateStudentDto, Student>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.DNI, opt => opt.MapFrom(src => src.DNI.ToUpper().Trim()))
                .ForMember(dest => dest.Name, opt => opt.MapFrom(src => src.Name.Trim()))
                .ForMember(dest => dest.Email, opt => opt.MapFrom(src =>
                    string.IsNullOrWhiteSpace(src.Email) ? null : src.Email.Trim()))
                .ForMember(dest => dest.Phone, opt => opt.MapFrom(src =>
                    string.IsNullOrWhiteSpace(src.Phone) ? null : src.Phone.Trim()))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.UpdatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Schedules, opt => opt.MapFrom(src => src.Schedules))
                .ForMember(dest => dest.ClassCancellations, opt => opt.Ignore())
                .ForMember(dest => dest.RecoveryClasses, opt => opt.Ignore());
        }

        private void CreateScheduleMappings()
        {
            // Schedule -> ScheduleDto
            CreateMap<Schedule, ScheduleDto>()
                .ForMember(dest => dest.DayName, opt => opt.MapFrom(src => GetDayName(src.DayOfWeek)))
                .ForMember(dest => dest.TimeRange, opt => opt.MapFrom(src =>
                    $"{src.StartTime:hh\\:mm}-{src.EndTime:hh\\:mm}"));

            // CreateScheduleDto -> Schedule
            CreateMap<CreateScheduleDto, Schedule>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.StudentId, opt => opt.Ignore())
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => true))
                .ForMember(dest => dest.CreatedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Student, opt => opt.Ignore())
                .ForMember(dest => dest.ClassCancellations, opt => opt.Ignore());
        }

        private void CreateClassCancellationMappings()
        {
            // ClassCancellation -> ClassCancellationDto
            CreateMap<ClassCancellation, ClassCancellationDto>()
                .ForMember(dest => dest.OriginalSchedule, opt => opt.MapFrom(src => src.OriginalSchedule));

            // CreateClassCancellationDto -> ClassCancellation
            CreateMap<CreateClassCancellationDto, ClassCancellation>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.StudentId, opt => opt.Ignore())
                .ForMember(dest => dest.CancelledAt, opt => opt.Ignore())
                .ForMember(dest => dest.Student, opt => opt.Ignore())
                .ForMember(dest => dest.OriginalSchedule, opt => opt.Ignore())
                .ForMember(dest => dest.RecoveryClasses, opt => opt.Ignore());
        }

        private void CreateRecoveryClassMappings()
        {
            // RecoveryClass -> RecoveryClassDto
            CreateMap<RecoveryClass, RecoveryClassDto>()
                .ForMember(dest => dest.TimeRange, opt => opt.MapFrom(src =>
                    $"{src.StartTime:hh\\:mm}-{src.EndTime:hh\\:mm}"));

            // CreateRecoveryClassDto -> RecoveryClass
            CreateMap<CreateRecoveryClassDto, RecoveryClass>()
                .ForMember(dest => dest.Id, opt => opt.Ignore())
                .ForMember(dest => dest.StudentId, opt => opt.Ignore())
                .ForMember(dest => dest.BookedAt, opt => opt.Ignore())
                .ForMember(dest => dest.Student, opt => opt.Ignore())
                .ForMember(dest => dest.OriginalCancellation, opt => opt.Ignore());
        }

        private string GetDayName(int dayOfWeek)
        {
            return dayOfWeek switch
            {
                1 => "Lunes",
                2 => "Martes",
                3 => "Miércoles",
                4 => "Jueves",
                5 => "Viernes",
                6 => "Sábado",
                7 => "Domingo",
                _ => "Desconocido"
            };
        }
    }
}
