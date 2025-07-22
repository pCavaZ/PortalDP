using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using PortalDP.Application.DTOs;
using PortalDP.Application.Interfaces;
using PortalDP.Domain.Entities;
using PortalDP.Infrastructure.Data;

namespace PortalDP.Application.Services
{
    public class StudentService : IStudentService
    {
        private readonly ApplicationDbContext _context;
        private readonly IMapper _mapper;
        private readonly ILogger<StudentService> _logger;

        public StudentService(
            ApplicationDbContext context,
            IMapper mapper,
            ILogger<StudentService> logger)
        {
            _context = context;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<ApiResponse<List<StudentDto>>> GetAllStudentsAsync()
        {
            try
            {
                _logger.LogInformation("Retrieving all students");

                var students = await _context.Students
                    .Include(s => s.Schedules.Where(sc => sc.IsActive))
                    .Where(s => s.IsActive)
                    .OrderBy(s => s.Name)
                    .ToListAsync();

                var studentDtos = _mapper.Map<List<StudentDto>>(students);

                _logger.LogInformation("Retrieved {Count} students successfully", students.Count);
                return ApiResponse<List<StudentDto>>.SuccessResponse(studentDtos);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving students");
                return ApiResponse<List<StudentDto>>.ErrorResponse($"Error retrieving students: {ex.Message}");
            }
        }

        public async Task<ApiResponse<StudentDto>> GetStudentByIdAsync(int id)
        {
            try
            {
                _logger.LogInformation("Retrieving student with ID: {StudentId}", id);

                if (id <= 0)
                {
                    return ApiResponse<StudentDto>.ErrorResponse("Invalid student ID");
                }

                var student = await _context.Students
                    .Include(s => s.Schedules.Where(sc => sc.IsActive))
                    .FirstOrDefaultAsync(s => s.Id == id && s.IsActive);

                if (student == null)
                {
                    _logger.LogWarning("Student with ID {StudentId} not found", id);
                    return ApiResponse<StudentDto>.ErrorResponse("Student not found");
                }

                var studentDto = _mapper.Map<StudentDto>(student);

                _logger.LogInformation("Retrieved student {StudentName} successfully", student.Name);
                return ApiResponse<StudentDto>.SuccessResponse(studentDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving student with ID: {StudentId}", id);
                return ApiResponse<StudentDto>.ErrorResponse($"Error retrieving student: {ex.Message}");
            }
        }

        public async Task<ApiResponse<StudentDto>> GetStudentByDniAsync(string dni)
        {
            try
            {
                _logger.LogInformation("Retrieving student with DNI: {DNI}", dni);

                if (string.IsNullOrWhiteSpace(dni))
                {
                    return ApiResponse<StudentDto>.ErrorResponse("DNI is required");
                }

                var normalizedDni = dni.Trim().ToUpper();

                var student = await _context.Students
                    .Include(s => s.Schedules.Where(sc => sc.IsActive))
                    .FirstOrDefaultAsync(s => s.DNI == normalizedDni && s.IsActive);

                if (student == null)
                {
                    _logger.LogWarning("Student with DNI {DNI} not found", normalizedDni);
                    return ApiResponse<StudentDto>.ErrorResponse("Student not found");
                }

                var studentDto = _mapper.Map<StudentDto>(student);

                _logger.LogInformation("Retrieved student {StudentName} with DNI {DNI} successfully", student.Name, normalizedDni);
                return ApiResponse<StudentDto>.SuccessResponse(studentDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving student with DNI: {DNI}", dni);
                return ApiResponse<StudentDto>.ErrorResponse($"Error retrieving student: {ex.Message}");
            }
        }

        public async Task<ApiResponse<StudentDto>> CreateStudentAsync(CreateStudentDto createStudentDto)
        {
            try
            {
                _logger.LogInformation("Creating new student: {StudentName}", createStudentDto.Name);

                // Validaciones básicas
                if (string.IsNullOrWhiteSpace(createStudentDto.Name))
                {
                    return ApiResponse<StudentDto>.ErrorResponse("Student name is required");
                }

                if (string.IsNullOrWhiteSpace(createStudentDto.DNI))
                {
                    return ApiResponse<StudentDto>.ErrorResponse("DNI is required");
                }

                var normalizedDni = createStudentDto.DNI.Trim().ToUpper();

                // Validar formato DNI (básico)
                if (!IsValidDniFormat(normalizedDni))
                {
                    return ApiResponse<StudentDto>.ErrorResponse("Invalid DNI format");
                }

                // Verificar que el DNI no existe
                var existingStudent = await _context.Students
                    .FirstOrDefaultAsync(s => s.DNI == normalizedDni);

                if (existingStudent != null)
                {
                    _logger.LogWarning("Attempt to create student with existing DNI: {DNI}", normalizedDni);
                    return ApiResponse<StudentDto>.ErrorResponse("A student with this DNI already exists");
                }

                // Validar capacidad de horarios
                foreach (var scheduleDto in createStudentDto.Schedules)
                {
                    var capacityCheck = await CheckScheduleCapacityAsync(scheduleDto.DayOfWeek, scheduleDto.StartTime, scheduleDto.EndTime);
                    if (!capacityCheck.Success || !capacityCheck.Data)
                    {
                        var dayName = GetDayName(scheduleDto.DayOfWeek);
                        var timeRange = $"{scheduleDto.StartTime:hh\\:mm}-{scheduleDto.EndTime:hh\\:mm}";
                        return ApiResponse<StudentDto>.ErrorResponse($"Schedule for {dayName} {timeRange} is full (maximum 10 students)");
                    }
                }

                // Crear el estudiante
                var student = _mapper.Map<Student>(createStudentDto);
                student.DNI = normalizedDni;
                student.CreatedAt = DateTime.UtcNow;
                student.UpdatedAt = DateTime.UtcNow;

                _context.Students.Add(student);
                await _context.SaveChangesAsync();

                // Recargar con los horarios incluidos
                var createdStudent = await _context.Students
                    .Include(s => s.Schedules)
                    .FirstAsync(s => s.Id == student.Id);

                var studentDto = _mapper.Map<StudentDto>(createdStudent);

                _logger.LogInformation("Created student {StudentName} with ID {StudentId} successfully", student.Name, student.Id);
                return ApiResponse<StudentDto>.SuccessResponse(studentDto, "Student created successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating student: {StudentName}", createStudentDto.Name);
                return ApiResponse<StudentDto>.ErrorResponse($"Error creating student: {ex.Message}");
            }
        }

        public async Task<ApiResponse<StudentDto>> UpdateStudentAsync(int id, UpdateStudentDto updateStudentDto)
        {
            try
            {
                _logger.LogInformation("Updating student with ID: {StudentId}", id);

                if (id <= 0)
                {
                    return ApiResponse<StudentDto>.ErrorResponse("Invalid student ID");
                }

                if (string.IsNullOrWhiteSpace(updateStudentDto.Name))
                {
                    return ApiResponse<StudentDto>.ErrorResponse("Student name is required");
                }

                var student = await _context.Students
                    .Include(s => s.Schedules)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (student == null)
                {
                    _logger.LogWarning("Student with ID {StudentId} not found for update", id);
                    return ApiResponse<StudentDto>.ErrorResponse("Student not found");
                }

                // Actualizar propiedades
                student.Name = updateStudentDto.Name.Trim();
                student.Email = string.IsNullOrWhiteSpace(updateStudentDto.Email) ? null : updateStudentDto.Email.Trim();
                student.Phone = string.IsNullOrWhiteSpace(updateStudentDto.Phone) ? null : updateStudentDto.Phone.Trim();
                student.IsActive = updateStudentDto.IsActive;
                student.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var studentDto = _mapper.Map<StudentDto>(student);

                _logger.LogInformation("Updated student {StudentName} with ID {StudentId} successfully", student.Name, id);
                return ApiResponse<StudentDto>.SuccessResponse(studentDto, "Student updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating student with ID: {StudentId}", id);
                return ApiResponse<StudentDto>.ErrorResponse($"Error updating student: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> DeleteStudentAsync(int id)
        {
            try
            {
                _logger.LogInformation("Deleting student with ID: {StudentId}", id);

                if (id <= 0)
                {
                    return ApiResponse<bool>.ErrorResponse("Invalid student ID");
                }

                var student = await _context.Students.FindAsync(id);

                if (student == null)
                {
                    _logger.LogWarning("Student with ID {StudentId} not found for deletion", id);
                    return ApiResponse<bool>.ErrorResponse("Student not found");
                }

                // Soft delete - marcar como inactivo
                student.IsActive = false;
                student.UpdatedAt = DateTime.UtcNow;

                // También desactivar los horarios
                var schedules = await _context.Schedules
                    .Where(s => s.StudentId == id)
                    .ToListAsync();

                foreach (var schedule in schedules)
                {
                    schedule.IsActive = false;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Deleted student {StudentName} with ID {StudentId} successfully", student.Name, id);
                return ApiResponse<bool>.SuccessResponse(true, "Student deleted successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting student with ID: {StudentId}", id);
                return ApiResponse<bool>.ErrorResponse($"Error deleting student: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> ValidateStudentDniAsync(string dni)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dni))
                {
                    return ApiResponse<bool>.ErrorResponse("DNI is required");
                }

                var normalizedDni = dni.Trim().ToUpper();

                var exists = await _context.Students
                    .AnyAsync(s => s.DNI == normalizedDni && s.IsActive);

                return ApiResponse<bool>.SuccessResponse(exists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating DNI: {DNI}", dni);
                return ApiResponse<bool>.ErrorResponse($"Error validating DNI: {ex.Message}");
            }
        }

        public async Task<ApiResponse<bool>> CheckScheduleCapacityAsync(int dayOfWeek, TimeSpan startTime, TimeSpan endTime, int? excludeStudentId = null)
        {
            try
            {
                var query = _context.Schedules
                    .Where(s => s.DayOfWeek == dayOfWeek
                           && s.StartTime == startTime
                           && s.EndTime == endTime
                           && s.IsActive
                           && s.Student.IsActive);

                if (excludeStudentId.HasValue)
                {
                    query = query.Where(s => s.StudentId != excludeStudentId.Value);
                }

                var occupiedCount = await query.CountAsync();
                var hasCapacity = occupiedCount < 10;

                _logger.LogDebug("Schedule capacity check - Day: {DayOfWeek}, Time: {StartTime}-{EndTime}, Occupied: {OccupiedCount}/10, HasCapacity: {HasCapacity}",
                    dayOfWeek, startTime, endTime, occupiedCount, hasCapacity);

                return ApiResponse<bool>.SuccessResponse(hasCapacity);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking schedule capacity");
                return ApiResponse<bool>.ErrorResponse($"Error checking schedule capacity: {ex.Message}");
            }
        }

        #region Helper Methods

        private bool IsValidDniFormat(string dni)
        {
            if (string.IsNullOrEmpty(dni) || dni.Length != 9)
                return false;

            // Los primeros 8 caracteres deben ser números
            var numbers = dni.Substring(0, 8);
            if (!numbers.All(char.IsDigit))
                return false;

            // El último caracter debe ser una letra
            var letter = dni[8];
            if (!char.IsLetter(letter))
                return false;

            // Validación básica del dígito de control (algoritmo simplificado)
            var dniLetters = "TRWAGMYFPDXBNJZSQVHLCKE";
            var expectedLetter = dniLetters[int.Parse(numbers) % 23];

            return letter == expectedLetter;
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
                _ => "Día desconocido"
            };
        }

        #endregion
    }
}