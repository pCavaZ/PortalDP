using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using PortalDP.Application.DTOs;
using PortalDP.Application.Interfaces;

namespace PortalDP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CalendarController : ControllerBase
    {
        private readonly ICalendarService _calendarService;
        private readonly ILogger<CalendarController> _logger;

        public CalendarController(
            ICalendarService calendarService,
            ILogger<CalendarController> logger)
        {
            _calendarService = calendarService;
            _logger = logger;
        }

        /// <summary>
        /// Obtener calendario de un estudiante para un mes específico
        /// </summary>
        [HttpGet("student/{studentId}/{year}/{month}")]
        public async Task<ActionResult<ApiResponse<CalendarDto>>> GetStudentCalendar(
            int studentId,
            int year,
            int month)
        {
            try
            {
                _logger.LogInformation("Requesting calendar for student {StudentId}, {Year}-{Month}",
                    studentId, year, month);

                // Los estudiantes solo pueden acceder a su propio calendario
                if (!IsAdmin() && GetCurrentStudentId() != studentId)
                {
                    _logger.LogWarning("Student {CurrentStudentId} attempted to access calendar for student {RequestedStudentId}",
                        GetCurrentStudentId(), studentId);
                    return Forbid();
                }

                if (year < 2020 || year > 2030)
                {
                    return BadRequest(ApiResponse<CalendarDto>.ErrorResponse("Year must be between 2020 and 2030"));
                }

                if (month < 1 || month > 12)
                {
                    return BadRequest(ApiResponse<CalendarDto>.ErrorResponse("Month must be between 1 and 12"));
                }

                var result = await _calendarService.GetStudentCalendarAsync(studentId, year, month);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving calendar for student {StudentId}", studentId);
                return StatusCode(500, ApiResponse<CalendarDto>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Obtener calendario del estudiante actual para un mes específico
        /// </summary>
        [HttpGet("my-calendar/{year}/{month}")]
        [Authorize(Roles = "Student")]
        public async Task<ActionResult<ApiResponse<CalendarDto>>> GetMyCalendar(int year, int month)
        {
            try
            {
                var studentId = GetCurrentStudentId();
                if (studentId == 0)
                {
                    return BadRequest(ApiResponse<CalendarDto>.ErrorResponse("Invalid student token"));
                }

                return await GetStudentCalendar(studentId, year, month);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving current student calendar");
                return StatusCode(500, ApiResponse<CalendarDto>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Obtener slots disponibles para una fecha específica
        /// </summary>
        [HttpGet("available-slots/{date}")]
        public async Task<ActionResult<ApiResponse<List<AvailableSlotDto>>>> GetAvailableSlots(string date)
        {
            try
            {
                _logger.LogInformation("Requesting available slots for date: {Date}", date);

                if (!DateOnly.TryParse(date, out var parsedDate))
                {
                    return BadRequest(ApiResponse<List<AvailableSlotDto>>.ErrorResponse("Invalid date format. Use YYYY-MM-DD"));
                }

                var result = await _calendarService.GetAvailableSlotsAsync(parsedDate);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving available slots for date: {Date}", date);
                return StatusCode(500, ApiResponse<List<AvailableSlotDto>>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Verificar si un estudiante puede cancelar una clase en una fecha específica
        /// </summary>
        [HttpGet("can-cancel/{studentId}/{date}")]
        public async Task<ActionResult<ApiResponse<bool>>> CanCancelClass(int studentId, string date)
        {
            try
            {
                _logger.LogInformation("Checking if student {StudentId} can cancel class on {Date}", studentId, date);

                // Los estudiantes solo pueden verificar sus propias clases
                if (!IsAdmin() && GetCurrentStudentId() != studentId)
                {
                    return Forbid();
                }

                if (!DateOnly.TryParse(date, out var parsedDate))
                {
                    return BadRequest(ApiResponse<bool>.ErrorResponse("Invalid date format. Use YYYY-MM-DD"));
                }

                var result = await _calendarService.CanCancelClassAsync(studentId, parsedDate);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking cancellation eligibility for student {StudentId} on {Date}", studentId, date);
                return StatusCode(500, ApiResponse<bool>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Cancelar una clase
        /// </summary>
        [HttpPost("cancel-class")]
        public async Task<ActionResult<ApiResponse<ClassCancellationDto>>> CancelClass([FromBody] CreateClassCancellationDto cancellationDto)
        {
            try
            {
                var studentId = GetCurrentStudentId();
                if (studentId == 0)
                {
                    return BadRequest(ApiResponse<ClassCancellationDto>.ErrorResponse("Invalid student token"));
                }

                _logger.LogInformation("Student {StudentId} attempting to cancel class on {Date}",
                    studentId, cancellationDto.ClassDate);

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(ApiResponse<ClassCancellationDto>.ErrorResponse("Validation failed", errors));
                }

                var result = await _calendarService.CancelClassAsync(studentId, cancellationDto);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                _logger.LogInformation("Class cancelled successfully for student {StudentId} on {Date}",
                    studentId, cancellationDto.ClassDate);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling class for student on {Date}", cancellationDto.ClassDate);
                return StatusCode(500, ApiResponse<ClassCancellationDto>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Reservar una clase de recuperación
        /// </summary>
        [HttpPost("book-recovery")]
        public async Task<ActionResult<ApiResponse<RecoveryClassDto>>> BookRecoveryClass([FromBody] CreateRecoveryClassDto recoveryDto)
        {
            try
            {
                var studentId = GetCurrentStudentId();
                if (studentId == 0)
                {
                    return BadRequest(ApiResponse<RecoveryClassDto>.ErrorResponse("Invalid student token"));
                }

                _logger.LogInformation("Student {StudentId} attempting to book recovery class on {Date}",
                    studentId, recoveryDto.ClassDate);

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(ApiResponse<RecoveryClassDto>.ErrorResponse("Validation failed", errors));
                }

                var result = await _calendarService.BookRecoveryClassAsync(studentId, recoveryDto);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                _logger.LogInformation("Recovery class booked successfully for student {StudentId} on {Date}",
                    studentId, recoveryDto.ClassDate);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error booking recovery class for student on {Date}", recoveryDto.ClassDate);
                return StatusCode(500, ApiResponse<RecoveryClassDto>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Obtener clases canceladas disponibles para recuperación
        /// </summary>
        [HttpGet("available-recoveries/{studentId}")]
        public async Task<ActionResult<ApiResponse<List<ClassCancellationDto>>>> GetAvailableRecoveryClasses(int studentId)
        {
            try
            {
                _logger.LogInformation("Requesting available recovery classes for student {StudentId}", studentId);

                // Los estudiantes solo pueden acceder a sus propias clases de recuperación
                if (!IsAdmin() && GetCurrentStudentId() != studentId)
                {
                    return Forbid();
                }

                var result = await _calendarService.GetAvailableRecoveryClassesAsync(studentId);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving available recovery classes for student {StudentId}", studentId);
                return StatusCode(500, ApiResponse<List<ClassCancellationDto>>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Obtener clases canceladas del estudiante actual disponibles para recuperación
        /// </summary>
        [HttpGet("my-available-recoveries")]
        [Authorize(Roles = "Student")]
        public async Task<ActionResult<ApiResponse<List<ClassCancellationDto>>>> GetMyAvailableRecoveryClasses()
        {
            try
            {
                var studentId = GetCurrentStudentId();
                if (studentId == 0)
                {
                    return BadRequest(ApiResponse<List<ClassCancellationDto>>.ErrorResponse("Invalid student token"));
                }

                return await GetAvailableRecoveryClasses(studentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving current student's available recovery classes");
                return StatusCode(500, ApiResponse<List<ClassCancellationDto>>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Verificar si el estudiante actual puede cancelar una clase en una fecha específica
        /// </summary>
        [HttpGet("can-cancel-my-class/{date}")]
        [Authorize(Roles = "Student")]
        public async Task<ActionResult<ApiResponse<bool>>> CanCancelMyClass(string date)
        {
            try
            {
                var studentId = GetCurrentStudentId();
                if (studentId == 0)
                {
                    return BadRequest(ApiResponse<bool>.ErrorResponse("Invalid student token"));
                }

                return await CanCancelClass(studentId, date);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking if current student can cancel class on {Date}", date);
                return StatusCode(500, ApiResponse<bool>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Obtener estadísticas del calendario (solo administradores)
        /// </summary>
        [HttpGet("statistics/{year}/{month}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<CalendarStatisticsDto>>> GetCalendarStatistics(int year, int month)
        {
            try
            {
                _logger.LogInformation("Admin requesting calendar statistics for {Year}-{Month}", year, month);

                if (year < 2020 || year > 2030)
                {
                    return BadRequest(ApiResponse<CalendarStatisticsDto>.ErrorResponse("Year must be between 2020 and 2030"));
                }

                if (month < 1 || month > 12)
                {
                    return BadRequest(ApiResponse<CalendarStatisticsDto>.ErrorResponse("Month must be between 1 and 12"));
                }

                // Aquí implementarías la lógica para obtener estadísticas
                // Por ahora devolvemos un placeholder
                var statistics = new CalendarStatisticsDto
                {
                    Year = year,
                    Month = month,
                    TotalClasses = 0,
                    CancelledClasses = 0,
                    RecoveryClasses = 0,
                    UtilizationRate = 0
                };

                return Ok(ApiResponse<CalendarStatisticsDto>.SuccessResponse(statistics));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving calendar statistics for {Year}-{Month}", year, month);
                return StatusCode(500, ApiResponse<CalendarStatisticsDto>.ErrorResponse("Internal server error"));
            }
        }

        #region Helper Methods

        private bool IsAdmin()
        {
            return User.IsInRole("Admin");
        }

        private int GetCurrentStudentId()
        {
            var studentIdClaim = User.FindFirst("StudentId")?.Value;
            return int.TryParse(studentIdClaim, out var studentId) ? studentId : 0;
        }

        private string GetCurrentDNI()
        {
            return User.FindFirst("DNI")?.Value ?? "";
        }

        private string GetCurrentUserName()
        {
            return User.FindFirst(ClaimTypes.Name)?.Value ?? "";
        }

        #endregion
    }
}
