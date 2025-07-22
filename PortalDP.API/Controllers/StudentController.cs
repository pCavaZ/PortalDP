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
    public class StudentsController : ControllerBase
    {
        private readonly IStudentService _studentService;
        private readonly ILogger<StudentsController> _logger;

        public StudentsController(
            IStudentService studentService,
            ILogger<StudentsController> logger)
        {
            _studentService = studentService;
            _logger = logger;
        }

        /// <summary>
        /// Obtener todos los estudiantes (solo administradores)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<List<StudentDto>>>> GetAllStudents()
        {
            try
            {
                _logger.LogInformation("Admin requesting all students list");

                var result = await _studentService.GetAllStudentsAsync();

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving all students");
                return StatusCode(500, ApiResponse<List<StudentDto>>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Obtener estudiante por ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ApiResponse<StudentDto>>> GetStudent(int id)
        {
            try
            {
                _logger.LogInformation("Requesting student with ID: {StudentId}", id);

                // Los estudiantes solo pueden acceder a sus propios datos, los admins a cualquiera
                if (!IsAdmin() && GetCurrentStudentId() != id)
                {
                    _logger.LogWarning("Student {CurrentStudentId} attempted to access data for student {RequestedStudentId}",
                        GetCurrentStudentId(), id);
                    return Forbid();
                }

                var result = await _studentService.GetStudentByIdAsync(id);

                if (!result.Success)
                {
                    return NotFound(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving student with ID: {StudentId}", id);
                return StatusCode(500, ApiResponse<StudentDto>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Obtener estudiante por DNI
        /// </summary>
        [HttpGet("by-dni/{dni}")]
        public async Task<ActionResult<ApiResponse<StudentDto>>> GetStudentByDni(string dni)
        {
            try
            {
                _logger.LogInformation("Requesting student with DNI: {DNI}", dni);

                // Los estudiantes solo pueden acceder a sus propios datos, los admins a cualquiera
                if (!IsAdmin() && GetCurrentDNI() != dni.ToUpper())
                {
                    _logger.LogWarning("Student {CurrentDNI} attempted to access data for DNI {RequestedDNI}",
                        GetCurrentDNI(), dni);
                    return Forbid();
                }

                var result = await _studentService.GetStudentByDniAsync(dni);

                if (!result.Success)
                {
                    return NotFound(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving student with DNI: {DNI}", dni);
                return StatusCode(500, ApiResponse<StudentDto>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Crear nuevo estudiante (solo administradores)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<StudentDto>>> CreateStudent([FromBody] CreateStudentDto createStudentDto)
        {
            try
            {
                _logger.LogInformation("Admin creating new student: {StudentName}", createStudentDto.Name);

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(ApiResponse<StudentDto>.ErrorResponse("Validation failed", errors));
                }

                var result = await _studentService.CreateStudentAsync(createStudentDto);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                _logger.LogInformation("Student created successfully with ID: {StudentId}", result.Data?.Id);
                return CreatedAtAction(
                    nameof(GetStudent),
                    new { id = result.Data?.Id },
                    result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating student: {StudentName}", createStudentDto.Name);
                return StatusCode(500, ApiResponse<StudentDto>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Actualizar estudiante (solo administradores)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<StudentDto>>> UpdateStudent(int id, [FromBody] UpdateStudentDto updateStudentDto)
        {
            try
            {
                _logger.LogInformation("Admin updating student with ID: {StudentId}", id);

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(ApiResponse<StudentDto>.ErrorResponse("Validation failed", errors));
                }

                var result = await _studentService.UpdateStudentAsync(id, updateStudentDto);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating student with ID: {StudentId}", id);
                return StatusCode(500, ApiResponse<StudentDto>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Eliminar estudiante (soft delete - solo administradores)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<bool>>> DeleteStudent(int id)
        {
            try
            {
                _logger.LogInformation("Admin deleting student with ID: {StudentId}", id);

                var result = await _studentService.DeleteStudentAsync(id);

                if (!result.Success)
                {
                    return BadRequest(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting student with ID: {StudentId}", id);
                return StatusCode(500, ApiResponse<bool>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Verificar capacidad de horario (solo administradores)
        /// </summary>
        [HttpGet("check-schedule-capacity")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<ApiResponse<bool>>> CheckScheduleCapacity(
            [FromQuery] int dayOfWeek,
            [FromQuery] string startTime,
            [FromQuery] string endTime,
            [FromQuery] int? excludeStudentId = null)
        {
            try
            {
                if (!TimeSpan.TryParse(startTime, out var parsedStartTime))
                {
                    return BadRequest(ApiResponse<bool>.ErrorResponse("Invalid start time format"));
                }

                if (!TimeSpan.TryParse(endTime, out var parsedEndTime))
                {
                    return BadRequest(ApiResponse<bool>.ErrorResponse("Invalid end time format"));
                }

                if (dayOfWeek < 1 || dayOfWeek > 7)
                {
                    return BadRequest(ApiResponse<bool>.ErrorResponse("Day of week must be between 1 and 7"));
                }

                var result = await _studentService.CheckScheduleCapacityAsync(
                    dayOfWeek,
                    parsedStartTime,
                    parsedEndTime,
                    excludeStudentId);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking schedule capacity");
                return StatusCode(500, ApiResponse<bool>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Obtener información del estudiante actual (basado en el token)
        /// </summary>
        [HttpGet("me")]
        public async Task<ActionResult<ApiResponse<StudentDto>>> GetCurrentStudent()
        {
            try
            {
                if (IsAdmin())
                {
                    return BadRequest(ApiResponse<StudentDto>.ErrorResponse("Admin users don't have student data"));
                }

                var studentId = GetCurrentStudentId();
                if (studentId == 0)
                {
                    return BadRequest(ApiResponse<StudentDto>.ErrorResponse("Invalid student token"));
                }

                var result = await _studentService.GetStudentByIdAsync(studentId);

                if (!result.Success)
                {
                    return NotFound(result);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving current student data");
                return StatusCode(500, ApiResponse<StudentDto>.ErrorResponse("Internal server error"));
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

//using System.Security.Claims;
//using Microsoft.AspNetCore.Authorization;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.EntityFrameworkCore;
//using PortalDP.Application.DTOs;
//using PortalDP.Application.Interfaces;
//using PortalDP.Infrastructure.Data;

//namespace AcademiaCostura.API.Controllers
//{
//    [ApiController]
//    [Route("api/[controller]")]
//    [Authorize] // Este endpoint requiere autenticación
//    public class StudentsController : ControllerBase
//    {
//        //private readonly ApplicationDbContext _context;
//        private readonly ILogger<StudentsController> _logger;
//        private readonly IStudentService _studentService;

//        public StudentsController(/*ApplicationDbContext context*/, ILogger<StudentsController> logger, IStudentService studentService)
//        {
//            _studentService = studentService;
//            //_context = context;
//            _logger = logger;
//        }

//        /// <summary>
//        /// Obtener todos los estudiantes (solo administradores)
//        /// </summary>
//        //[HttpGet]
//        //[Authorize(Roles = "Admin")]
//        //public async Task<ActionResult<object>> GetAllStudents()
//        //{
//        //    try
//        //    {
//        //        _logger.LogInformation("Admin requesting all students list");

//        //        var students = await _context.Students
//        //            .Where(s => s.IsActive)
//        //            .Select(s => new {
//        //                s.Id,
//        //                s.Name,
//        //                s.DNI,
//        //                s.Email,
//        //                s.Phone,
//        //                s.IsActive,
//        //                s.CreatedAt,
//        //                s.UpdatedAt
//        //            })
//        //            .OrderBy(s => s.Name)
//        //            .ToListAsync();

//        //        return Ok(new
//        //        {
//        //            success = true,
//        //            count = students.Count,
//        //            students = students
//        //        });
//        //    }
//        //    catch (Exception ex)
//        //    {
//        //        _logger.LogError(ex, "Error retrieving all students");
//        //        return StatusCode(500, new
//        //        {
//        //            success = false,
//        //            message = "Internal server error"
//        //        });
//        //    }
//        //}

//        /// <summary>
//        /// Obtener estudiante por ID
//        /// </summary>
//        //[HttpGet("{id}")]
//        //public async Task<ActionResult<object>> GetStudent(int id)
//        //{
//        //    try
//        //    {
//        //        _logger.LogInformation("Requesting student with ID: {StudentId}", id);

//        //        // Los estudiantes solo pueden acceder a sus propios datos, los admins a cualquiera
//        //        if (!IsAdmin() && GetCurrentStudentId() != id)
//        //        {
//        //            _logger.LogWarning("Student {CurrentStudentId} attempted to access data for student {RequestedStudentId}",
//        //                GetCurrentStudentId(), id);
//        //            return Forbid();
//        //        }

//        //        var student = await _context.Students
//        //            .Where(s => s.Id == id && s.IsActive)
//        //            .Select(s => new {
//        //                s.Id,
//        //                s.Name,
//        //                s.DNI,
//        //                s.Email,
//        //                s.Phone,
//        //                s.IsActive,
//        //                s.CreatedAt,
//        //                s.UpdatedAt
//        //            })
//        //            .FirstOrDefaultAsync();

//        //        if (student == null)
//        //        {
//        //            return NotFound(new
//        //            {
//        //                success = false,
//        //                message = "Student not found"
//        //            });
//        //        }

//        //        return Ok(new
//        //        {
//        //            success = true,
//        //            student = student
//        //        });
//        //    }
//        //    catch (Exception ex)
//        //    {
//        //        _logger.LogError(ex, "Error retrieving student with ID: {StudentId}", id);
//        //        return StatusCode(500, new
//        //        {
//        //            success = false,
//        //            message = "Internal server error"
//        //        });
//        //    }
//        //}

//        /// <summary>
//        /// Obtener información del estudiante actual (basado en el token)
//        /// </summary>
//        [HttpGet("me")]
//        //[Authorize(Roles = "Student")]
//        public async Task<ActionResult<object>> GetCurrentStudent()
//        {
//            try
//            {
//                var studentId = GetCurrentStudentId();
//                if (studentId == 0)
//                {
//                    return BadRequest(new
//                    {
//                        success = false,
//                        message = "Invalid student token"
//                    });
//                }

//                return await GetStudent(studentId);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error retrieving current student data");
//                return StatusCode(500, new
//                {
//                    success = false,
//                    message = "Internal server error"
//                });
//            }
//        }

//        /// <summary>
//        /// Obtener información del usuario actual (admin o estudiante)
//        /// </summary>
//        [HttpGet("whoami")]
//        public ActionResult<object> WhoAmI()
//        {
//            try
//            {
//                var claims = User.Claims.ToDictionary(c => c.Type, c => c.Value);

//                return Ok(new
//                {
//                    success = true,
//                    isAuthenticated = User.Identity?.IsAuthenticated ?? false,
//                    name = GetCurrentUserName(),
//                    dni = GetCurrentDNI(),
//                    isAdmin = IsAdmin(),
//                    studentId = GetCurrentStudentId(),
//                    roles = User.Claims.Where(c => c.Type == ClaimTypes.Role).Select(c => c.Value).ToList(),
//                    allClaims = claims
//                });
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error getting current user info");
//                return StatusCode(500, new
//                {
//                    success = false,
//                    message = "Internal server error"
//                });
//            }
//        }

//        ///<summary>
//        /// Crear nuevo estudiante (solo administradores)
//        /// </summary>
//        [HttpPost]
//        [Authorize(Roles = "Admin")]
//        public async Task<ActionResult<ApiResponse<StudentDto>>> CreateStudent([FromBody] CreateStudentDto createStudentDto)
//        {
//            try
//            {
//                _logger.LogInformation("Admin creating new student: {StudentName}", createStudentDto.Name);

//                if (!ModelState.IsValid)
//                {
//                    var errors = ModelState.Values
//                        .SelectMany(v => v.Errors)
//                        .Select(e => e.ErrorMessage)
//                        .ToList();

//                    return BadRequest(ApiResponse<StudentDto>.ErrorResponse("Validation failed", errors));
//                }

//                var result = await _studentService.CreateStudentAsync(createStudentDto);

//                if (!result.Success)
//                {
//                    return BadRequest(result);
//                }

//                _logger.LogInformation("Student created successfully with ID: {StudentId}", result.Data?.Id);
//                return CreatedAtAction(
//                    nameof(GetStudent),
//                    new { id = result.Data?.Id },
//                    result);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error creating student: {StudentName}", createStudentDto.Name);
//                return StatusCode(500, ApiResponse<StudentDto>.ErrorResponse("Internal server error"));
//            }
//        }

//        /// <summary>
//        /// Actualizar estudiante (solo administradores)
//        /// </summary>
//        [HttpPut("{id}")]
//        [Authorize(Roles = "Admin")]
//        public async Task<ActionResult<ApiResponse<StudentDto>>> UpdateStudent(int id, [FromBody] UpdateStudentDto updateStudentDto)
//        {
//            try
//            {
//                _logger.LogInformation("Admin updating student with ID: {StudentId}", id);

//                if (!ModelState.IsValid)
//                {
//                    var errors = ModelState.Values
//                        .SelectMany(v => v.Errors)
//                        .Select(e => e.ErrorMessage)
//                        .ToList();

//                    return BadRequest(ApiResponse<StudentDto>.ErrorResponse("Validation failed", errors));
//                }

//                var result = await _studentService.UpdateStudentAsync(id, updateStudentDto);

//                if (!result.Success)
//                {
//                    return BadRequest(result);
//                }

//                return Ok(result);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error updating student with ID: {StudentId}", id);
//                return StatusCode(500, ApiResponse<StudentDto>.ErrorResponse("Internal server error"));
//            }
//        }

//        /// <summary>
//        /// Eliminar estudiante (soft delete - solo administradores)
//        /// </summary>
//        [HttpDelete("{id}")]
//        [Authorize(Roles = "Admin")]
//        public async Task<ActionResult<ApiResponse<bool>>> DeleteStudent(int id)
//        {
//            try
//            {
//                _logger.LogInformation("Admin deleting student with ID: {StudentId}", id);

//                var result = await _studentService.DeleteStudentAsync(id);

//                if (!result.Success)
//                {
//                    return BadRequest(result);
//                }

//                return Ok(result);
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error deleting student with ID: {StudentId}", id);
//                return StatusCode(500, ApiResponse<bool>.ErrorResponse("Internal server error"));
//            }
//        }

//        ///// <summary>
//        ///// Añadir un nuevo estudiante (solo administradores)
//        ///// </summary>
//        //[HttpPost("create")]
//        //[Authorize (Roles = "Admin")]
//        //public async Task<ActionResult<ApiResponse<StudentDto>>> CreateStudent([FromBody] CreateStudentDto createStudentDto)
//        //{
//        //    try
//        //    {
//        //        _logger.LogInformation("Singup attemp for DNI: {DNI}", createStudentDto.DNI);

//        //        var createStudentResult = await _studentService.CreateStudentAsync(createStudentDto);
//        //        if ( !createStudentResult.Success || createStudentResult.Data == null)
//        //        {
//        //            _logger.LogWarning("Failed to create student: {Errors}", createStudentResult.Errors);
//        //            return BadRequest(new
//        //            {
//        //                success = false,
//        //                message = "Failed to create student",
//        //                errors = createStudentResult.Errors
//        //            });
//        //        }

//        //        return Ok(ApiResponse<StudentDto>.SuccessResponse(createStudentResult.Data, "Student created successfully"));
//        //    }
//        //    catch (Exception ex)
//        //    {
//        //        _logger.LogError(ex, "Error creating student: {StudentName}", createStudentDto.Name);
//        //        return StatusCode(500, new
//        //        {
//        //            success = false,
//        //            message = "Internal server error"
//        //        });
//        //    }
//        //}

//        #region Helper Methods

//        private bool IsAdmin()
//        {
//            return User.IsInRole("Admin");
//        }

//        private int GetCurrentStudentId()
//        {
//            var studentIdClaim = User.FindFirst("StudentId")?.Value;
//            return int.TryParse(studentIdClaim, out var studentId) ? studentId : 0;
//        }

//        private string GetCurrentDNI()
//        {
//            return User.FindFirst("DNI")?.Value ?? "";
//        }

//        private string GetCurrentUserName()
//        {
//            return User.FindFirst(ClaimTypes.Name)?.Value ?? "";
//        }

//        #endregion
//    }
//}
