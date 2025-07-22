using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PortalDP.Application.DTOs;
using PortalDP.Application.Interfaces;
using PortalDP.Domain.Entities;
using PortalDP.Infrastructure.Data;

namespace PortalDP.Application.Services
{
    public class CalendarService : ICalendarService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<CalendarService> _logger;

        public CalendarService(
            ApplicationDbContext context,
            IMapper mapper,
            ILogger<CalendarService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ApiResponse<CalendarDto>> GetStudentCalendarAsync(int studentId, int year, int month)
        {
            try
            {
                _logger.LogInformation("Retrieving calendar for student {StudentId}, {Year}-{Month}", studentId, year, month);

                if (studentId <= 0)
                {
                    return ApiResponse<CalendarDto>.ErrorResponse("Invalid student ID");
                }

                if (month < 1 || month > 12)
                {
                    return ApiResponse<CalendarDto>.ErrorResponse("Invalid month");
                }

                var student = await _context.Students
                    .Include(s => s.Schedules.Where(sc => sc.IsActive))
                    .Include(s => s.ClassCancellations)
                    .Include(s => s.RecoveryClasses)
                    .FirstOrDefaultAsync(s => s.Id == studentId && s.IsActive);

                if (student == null)
                {
                    _logger.LogWarning("Student with ID {StudentId} not found", studentId);
                    return ApiResponse<CalendarDto>.ErrorResponse("Student not found");
                }

                var calendar = new CalendarDto
                {
                    Year = year,
                    Month = month,
                    Days = new List<CalendarDayDto>()
                };

                var startDate = new DateOnly(year, month, 1);
                var endDate = startDate.AddMonths(1).AddDays(-1);

                for (var date = startDate; date <= endDate; date = date.AddDays(1))
                {
                    var dayDto = await CreateCalendarDayDto(student, date);
                    calendar.Days.Add(dayDto);
                }

                _logger.LogInformation("Generated calendar for student {StudentId} with {DayCount} days", studentId, calendar.Days.Count);
                return ApiResponse<CalendarDto>.SuccessResponse(calendar);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving calendar for student {StudentId}", studentId);
                return ApiResponse<CalendarDto>.ErrorResponse($"Error retrieving calendar: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<AvailableSlotDto>>> GetAvailableSlotsAsync(DateOnly date)
        {
            try
            {
                _logger.LogInformation("Retrieving available slots for date {Date}", date);

                if (date <= DateOnly.FromDateTime(DateTime.UtcNow))
                {
                    return ApiResponse<List<AvailableSlotDto>>.SuccessResponse(new List<AvailableSlotDto>());
                }

                var dayOfWeek = (int)date.DayOfWeek;
                if (dayOfWeek == 0) dayOfWeek = 7; // Domingo = 7

                // Solo días laborables (Lunes a Viernes)
                if (dayOfWeek > 5)
                {
                    return ApiResponse<List<AvailableSlotDto>>.SuccessResponse(new List<AvailableSlotDto>());
                }

                var timeSlots = await _context.TimeSlots
                    .Where(ts => ts.DayOfWeek == dayOfWeek && ts.IsActive)
                    .OrderBy(ts => ts.StartTime)
                    .ToListAsync();

                var availableSlots = new List<AvailableSlotDto>();

                foreach (var timeSlot in timeSlots)
                {
                    var occupiedCount = await GetOccupiedCountForDateAsync(date, timeSlot.StartTime, timeSlot.EndTime);
                    var availableSpots = timeSlot.MaxCapacity - occupiedCount;

                    if (availableSpots > 0)
                    {
                        availableSlots.Add(new AvailableSlotDto
                        {
                            StartTime = timeSlot.StartTime,
                            EndTime = timeSlot.EndTime,
                            TimeRange = $"{timeSlot.StartTime:hh\\:mm}-{timeSlot.EndTime:hh\\:mm}",
                            AvailableSpots = availableSpots,
                            TotalSpots = timeSlot.MaxCapacity
                        });
                    }
                }

                _logger.LogInformation("Found {SlotCount} available slots for date {Date}", availableSlots.Count, date);
                return ApiResponse<List<AvailableSlotDto>>.SuccessResponse(availableSlots);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving available slots for date {Date}", date);
                return ApiResponse<List<AvailableSlotDto>>.ErrorResponse($"Error retrieving available slots: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> CanCancelClassAsync(int studentId, DateOnly classDate)
        {
            try
            {
                _logger.LogInformation("Checking if student {StudentId} can cancel class on {Date}", studentId, classDate);

                var now = DateTime.UtcNow;
                var classDateTime = classDate.ToDateTime(TimeOnly.MinValue);

                // Debe ser al menos 24 horas de antelación
                if ((classDateTime - now).TotalHours < 24)
                {
                    _logger.LogDebug("Cannot cancel - less than 24 hours notice");
                    return ApiResponse<bool>.SuccessResponse(false);
                }

                // Verificar que el estudiante tiene clase ese día
                var dayOfWeek = (int)classDate.DayOfWeek;
                if (dayOfWeek == 0) dayOfWeek = 7;

                var hasSchedule = await _context.Schedules
                    .AnyAsync(s => s.StudentId == studentId
                              && s.DayOfWeek == dayOfWeek
                              && s.IsActive
                              && s.Student.IsActive);

                if (!hasSchedule)
                {
                    _logger.LogDebug("Cannot cancel - no scheduled class on {Date}", classDate);
                    return ApiResponse<bool>.SuccessResponse(false);
                }

                // Verificar que no esté ya cancelada
                var alreadyCancelled = await _context.ClassCancellations
                    .AnyAsync(c => c.StudentId == studentId && c.ClassDate == classDate);

                var canCancel = !alreadyCancelled;
                _logger.LogDebug("Can cancel class: {CanCancel}", canCancel);

                return ApiResponse<bool>.SuccessResponse(canCancel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking cancellation eligibility for student {StudentId} on {Date}", studentId, classDate);
                return ApiResponse<bool>.ErrorResponse($"Error checking cancellation eligibility: {ex.Message}");
            }
        }

        public async Task<ApiResponse<ClassCancellationDto>> CancelClassAsync(int studentId, CreateClassCancellationDto cancellationDto)
        {
            try
            {
                _logger.LogInformation("Cancelling class for student {StudentId} on {Date}", studentId, cancellationDto.ClassDate);

                // Verificar que se puede cancelar
                var canCancel = await CanCancelClassAsync(studentId, cancellationDto.ClassDate);
                if (!canCancel.Success || !canCancel.Data)
                {
                    _logger.LogWarning("Cannot cancel class for student {StudentId} on {Date}", studentId, cancellationDto.ClassDate);
                    return ApiResponse<ClassCancellationDto>.ErrorResponse("Cannot cancel this class");
                }

                // Verificar que el horario pertenece al estudiante
                var schedule = await _context.Schedules
                    .FirstOrDefaultAsync(s => s.Id == cancellationDto.OriginalScheduleId
                                            && s.StudentId == studentId
                                            && s.IsActive);

                if (schedule == null)
                {
                    _logger.LogWarning("Schedule {ScheduleId} not found for student {StudentId}", cancellationDto.OriginalScheduleId, studentId);
                    return ApiResponse<ClassCancellationDto>.ErrorResponse("Schedule not found");
                }

                // Crear la cancelación
                var cancellation = new ClassCancellation
                {
                    StudentId = studentId,
                    ClassDate = cancellationDto.ClassDate,
                    OriginalScheduleId = cancellationDto.OriginalScheduleId,
                    Reason = cancellationDto.Reason?.Trim(),
                    CancelledAt = DateTime.UtcNow
                };

                _context.ClassCancellations.Add(cancellation);
                await _context.SaveChangesAsync();

                // Recargar con los datos relacionados
                var savedCancellation = await _context.ClassCancellations
                    .Include(c => c.OriginalSchedule)
                    .FirstAsync(c => c.Id == cancellation.Id);

                var cancellationDto2 = _mapper.Map<ClassCancellationDto>(savedCancellation);

                _logger.LogInformation("Cancelled class for student {StudentId} on {Date} successfully", studentId, cancellationDto.ClassDate);
                return ApiResponse<ClassCancellationDto>.SuccessResponse(cancellationDto2, "Class cancelled successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling class for student {StudentId} on {Date}", studentId, cancellationDto.ClassDate);
                return ApiResponse<ClassCancellationDto>.ErrorResponse($"Error cancelling class: {ex.Message}");
            }
        }

        public async Task<ApiResponse<RecoveryClassDto>> BookRecoveryClassAsync(int studentId, CreateRecoveryClassDto recoveryDto)
        {
            try
            {
                _logger.LogInformation("Booking recovery class for student {StudentId} on {Date}", studentId, recoveryDto.ClassDate);

                // Verificar que el estudiante tiene clases canceladas disponibles para recuperar
                var availableRecoveries = await GetAvailableRecoveryClassesAsync(studentId);
                if (!availableRecoveries.Success || !availableRecoveries.Data.Any())
                {
                    _logger.LogWarning("No available recovery classes for student {StudentId}", studentId);
                    return ApiResponse<RecoveryClassDto>.ErrorResponse("No available recovery classes");
                }

                // Verificar que la cancelación original pertenece al estudiante
                var cancellation = await _context.ClassCancellations
                    .FirstOrDefaultAsync(c => c.Id == recoveryDto.OriginalCancellationId && c.StudentId == studentId);

                if (cancellation == null)
                {
                    _logger.LogWarning("Cancellation {CancellationId} not found for student {StudentId}", recoveryDto.OriginalCancellationId, studentId);
                    return ApiResponse<RecoveryClassDto>.ErrorResponse("Original cancellation not found");
                }

                // Verificar que no hay una clase de recuperación ya reservada para esta cancelación
                var existingRecovery = await _context.RecoveryClasses
                    .AnyAsync(rc => rc.OriginalCancellationId == recoveryDto.OriginalCancellationId);

                if (existingRecovery)
                {
                    _logger.LogWarning("Recovery class already booked for cancellation {CancellationId}", recoveryDto.OriginalCancellationId);
                    return ApiResponse<RecoveryClassDto>.ErrorResponse("Recovery class already booked for this cancellation");
                }

                // Verificar que el horario está disponible
                var occupiedCount = await GetOccupiedCountForDateAsync(recoveryDto.ClassDate, recoveryDto.StartTime, recoveryDto.EndTime);
                if (occupiedCount >= 10)
                {
                    _logger.LogWarning("Time slot full for recovery class on {Date} {StartTime}-{EndTime}", recoveryDto.ClassDate, recoveryDto.StartTime, recoveryDto.EndTime);
                    return ApiResponse<RecoveryClassDto>.ErrorResponse("Time slot is full");
                }

                // Verificar que la fecha es futura
                if (recoveryDto.ClassDate <= DateOnly.FromDateTime(DateTime.UtcNow))
                {
                    return ApiResponse<RecoveryClassDto>.ErrorResponse("Recovery class must be scheduled for a future date");
                }

                // Verificar que no es día de clase regular del estudiante
                var dayOfWeek = (int)recoveryDto.ClassDate.DayOfWeek;
                if (dayOfWeek == 0) dayOfWeek = 7;

                var hasRegularClass = await _context.Schedules
                    .AnyAsync(s => s.StudentId == studentId
                              && s.DayOfWeek == dayOfWeek
                              && s.IsActive);

                if (hasRegularClass)
                {
                    return ApiResponse<RecoveryClassDto>.ErrorResponse("Cannot book recovery class on your regular class day");
                }

                // Crear la clase de recuperación
                var recoveryClass = new RecoveryClass
                {
                    StudentId = studentId,
                    ClassDate = recoveryDto.ClassDate,
                    StartTime = recoveryDto.StartTime,
                    EndTime = recoveryDto.EndTime,
                    OriginalCancellationId = recoveryDto.OriginalCancellationId,
                    BookedAt = DateTime.UtcNow
                };

                _context.RecoveryClasses.Add(recoveryClass);
                await _context.SaveChangesAsync();

                var recoveryClassDto = _mapper.Map<RecoveryClassDto>(recoveryClass);

                _logger.LogInformation("Booked recovery class for student {StudentId} on {Date} successfully", studentId, recoveryDto.ClassDate);
                return ApiResponse<RecoveryClassDto>.SuccessResponse(recoveryClassDto, "Recovery class booked successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error booking recovery class for student {StudentId} on {Date}", studentId, recoveryDto.ClassDate);
                return ApiResponse<RecoveryClassDto>.ErrorResponse($"Error booking recovery class: {ex.Message}");
            }
        }

        public async Task<ApiResponse<List<ClassCancellationDto>>> GetAvailableRecoveryClassesAsync(int studentId)
        {
            try
            {
                _logger.LogInformation("Retrieving available recovery classes for student {StudentId}", studentId);

                // Obtener todas las cancelaciones del estudiante
                var allCancellations = await _context.ClassCancellations
                    .Include(c => c.OriginalSchedule)
                    .Where(c => c.StudentId == studentId)
                    .ToListAsync();

                // Obtener IDs de cancelaciones que ya tienen clase de recuperación reservada
                var recoveredCancellationIds = await _context.RecoveryClasses
                    .Where(rc => rc.StudentId == studentId)
                    .Select(rc => rc.OriginalCancellationId)
                    .ToListAsync();

                // Filtrar cancelaciones disponibles para recuperación
                var availableCancellations = allCancellations
                    .Where(c => !recoveredCancellationIds.Contains(c.Id))
                    .OrderByDescending(c => c.CancelledAt)
                    .ToList();

                var cancellationDtos = _mapper.Map<List<ClassCancellationDto>>(availableCancellations);

                _logger.LogInformation("Found {Count} available recovery classes for student {StudentId}", cancellationDtos.Count, studentId);
                return ApiResponse<List<ClassCancellationDto>>.SuccessResponse(cancellationDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving available recovery classes for student {StudentId}", studentId);
                return ApiResponse<List<ClassCancellationDto>>.ErrorResponse($"Error retrieving available recovery classes: {ex.Message}");
            }
        }

        #region Helper Methods

        private async Task<CalendarDayDto> CreateCalendarDayDto(Student student, DateOnly date)
        {
            var dayDto = new CalendarDayDto
            {
                Date = date,
                Classes = new List<ClassDto>(),
                RecoveryClasses = new List<RecoveryClassDto>(),
                Cancellations = new List<ClassCancellationDto>(),
                IsAvailable = date.DayOfWeek >= DayOfWeek.Monday && date.DayOfWeek <= DayOfWeek.Friday
            };

            // Obtener clases regulares para este día
            var dayOfWeek = (int)date.DayOfWeek;
            if (dayOfWeek == 0) dayOfWeek = 7; // Domingo = 7

            var regularSchedules = student.Schedules
                .Where(s => s.DayOfWeek == dayOfWeek && s.IsActive)
                .ToList();

            foreach (var schedule in regularSchedules)
            {
                var isCancelled = student.ClassCancellations
                    .Any(c => c.ClassDate == date && c.OriginalScheduleId == schedule.Id);

                var canCancel = !isCancelled && date > DateOnly.FromDateTime(DateTime.UtcNow.AddHours(24));

                dayDto.Classes.Add(new ClassDto
                {
                    Id = schedule.Id,
                    StartTime = schedule.StartTime,
                    EndTime = schedule.EndTime,
                    TimeRange = $"{schedule.StartTime:hh\\:mm}-{schedule.EndTime:hh\\:mm}",
                    IsCancelled = isCancelled,
                    CanCancel = canCancel
                });
            }

            // Obtener clases de recuperación para este día
            var recoveryClasses = student.RecoveryClasses
                .Where(rc => rc.ClassDate == date)
                .ToList();

            dayDto.RecoveryClasses = _mapper.Map<List<RecoveryClassDto>>(recoveryClasses);

            // Obtener cancelaciones para este día
            var cancellations = student.ClassCancellations
                .Where(c => c.ClassDate == date)
                .ToList();

            dayDto.Cancellations = _mapper.Map<List<ClassCancellationDto>>(cancellations);

            // Obtener slots disponibles si es un día futuro laborable y no tiene clase regular
            if (date > DateOnly.FromDateTime(DateTime.UtcNow) && dayDto.IsAvailable && !dayDto.Classes.Any())
            {
                var availableSlots = await GetAvailableSlotsForDateInternalAsync(date);
                dayDto.AvailableSlots = availableSlots;
            }

            return dayDto;
        }

        private async Task<List<AvailableSlotDto>> GetAvailableSlotsForDateInternalAsync(DateOnly date)
        {
            var dayOfWeek = (int)date.DayOfWeek;
            if (dayOfWeek == 0) dayOfWeek = 7;

            var timeSlots = await _context.TimeSlots
                .Where(ts => ts.DayOfWeek == dayOfWeek && ts.IsActive)
                .OrderBy(ts => ts.StartTime)
                .ToListAsync();

            var availableSlots = new List<AvailableSlotDto>();

            foreach (var timeSlot in timeSlots)
            {
                var occupiedCount = await GetOccupiedCountForDateAsync(date, timeSlot.StartTime, timeSlot.EndTime);
                var availableSpots = timeSlot.MaxCapacity - occupiedCount;

                if (availableSpots > 0)
                {
                    availableSlots.Add(new AvailableSlotDto
                    {
                        StartTime = timeSlot.StartTime,
                        EndTime = timeSlot.EndTime,
                        TimeRange = $"{timeSlot.StartTime:hh\\:mm}-{timeSlot.EndTime:hh\\:mm}",
                        AvailableSpots = availableSpots,
                        TotalSpots = timeSlot.MaxCapacity
                    });
                }
            }

            return availableSlots;
        }

        private async Task<int> GetOccupiedCountForDateAsync(DateOnly date, TimeSpan startTime, TimeSpan endTime)
        {
            var dayOfWeek = (int)date.DayOfWeek;
            if (dayOfWeek == 0) dayOfWeek = 7;

            // Contar clases regulares
            var regularCount = await _context.Schedules
                .Where(s => s.DayOfWeek == dayOfWeek
                       && s.StartTime == startTime
                       && s.EndTime == endTime
                       && s.IsActive
                       && s.Student.IsActive)
                .CountAsync();

            // Restar clases canceladas
            var cancelledCount = await _context.ClassCancellations
                .Where(c => c.ClassDate == date
                       && c.OriginalSchedule.StartTime == startTime
                       && c.OriginalSchedule.EndTime == endTime)
                .CountAsync();

            // Sumar clases de recuperación
            var recoveryCount = await _context.RecoveryClasses
                .Where(rc => rc.ClassDate == date
                        && rc.StartTime == startTime
                        && rc.EndTime == endTime)
                .CountAsync();

            var totalOccupied = regularCount - cancelledCount + recoveryCount;

            _logger.LogDebug("Occupied count for {Date} {StartTime}-{EndTime}: Regular={RegularCount}, Cancelled={CancelledCount}, Recovery={RecoveryCount}, Total={TotalOccupied}",
                date, startTime, endTime, regularCount, cancelledCount, recoveryCount, totalOccupied);

            return Math.Max(0, totalOccupied);
        }

        #endregion
    }
}