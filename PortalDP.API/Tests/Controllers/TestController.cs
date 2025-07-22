using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PortalDP.Domain.Entities;
using PortalDP.Infrastructure.Data;

namespace AcademiaCostura.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TestController> _logger;

        public TestController(ApplicationDbContext context, ILogger<TestController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Verificar que la API funciona correctamente
        /// </summary>
        [HttpGet]
        public ActionResult<object> Get()
        {
            return Ok(new
            {
                message = "API funcionando correctamente",
                timestamp = DateTime.UtcNow,
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Unknown",
                version = "1.0.0"
            });
        }

        /// <summary>
        /// Probar conexión a la base de datos
        /// </summary>
        [HttpGet("database")]
        public async Task<ActionResult<object>> TestDatabase()
        {
            try
            {
                _logger.LogInformation("Testing database connection...");

                // Verificar conexión a base de datos
                var canConnect = await _context.Database.CanConnectAsync();

                // Contar estudiantes
                var studentCount = await _context.Students.CountAsync();

                // Información adicional
                var connectionString = _context.Database.GetConnectionString();
                var providerName = _context.Database.ProviderName;

                return Ok(new
                {
                    success = true,
                    canConnect = canConnect,
                    studentCount = studentCount,
                    databaseProvider = providerName,
                    connectionInfo = connectionString?.Split(';')
                        .Where(part => !part.Contains("Password", StringComparison.OrdinalIgnoreCase))
                        .ToArray()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database test failed");
                return Ok(new
                {
                    success = false,
                    canConnect = false,
                    error = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }
        }

        /// <summary>
        /// Ver todos los estudiantes (sin autenticación, solo para testing)
        /// </summary>
        [HttpGet("students")]
        public async Task<ActionResult<object>> GetStudents()
        {
            try
            {
                var students = await _context.Students
                    .Where(s => s.IsActive)
                    .Select(s => new {
                        s.Id,
                        s.Name,
                        s.DNI,
                        s.Email,
                        s.Phone,
                        s.IsActive,
                        s.CreatedAt
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    count = students.Count,
                    students = students
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving students for test");
                return BadRequest(new
                {
                    success = false,
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Crear un estudiante de prueba
        /// </summary>
        [HttpPost("create-student")]
        public async Task<ActionResult<object>> CreateTestStudent([FromBody] CreateTestStudentRequest? request)
        {
            try
            {
                // Generar datos aleatorios si no se proporcionan
                var random = new Random();
                var testNumber = random.Next(1000, 9999);

                var student = new Student
                {
                    Name = request?.Name ?? $"Estudiante Test {testNumber}",
                    DNI = request?.DNI ?? $"{testNumber:D8}T",
                    Email = request?.Email ?? $"test{testNumber}@test.com",
                    Phone = request?.Phone ?? $"666{testNumber:D6}",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                // Verificar que el DNI no existe
                var existingStudent = await _context.Students
                    .FirstOrDefaultAsync(s => s.DNI == student.DNI);

                if (existingStudent != null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        error = $"Student with DNI {student.DNI} already exists"
                    });
                }

                _context.Students.Add(student);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "Test student created successfully",
                    student = new
                    {
                        student.Id,
                        student.Name,
                        student.DNI,
                        student.Email,
                        student.Phone
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating test student");
                return BadRequest(new
                {
                    success = false,
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Limpiar datos de prueba
        /// </summary>
        [HttpDelete("cleanup")]
        public async Task<ActionResult<object>> CleanupTestData()
        {
            try
            {
                // Eliminar estudiantes de prueba (que contengan "Test" en el nombre)
                var testStudents = await _context.Students
                    .Where(s => s.Name.Contains("Test") || s.Email.Contains("test"))
                    .ToListAsync();

                _context.Students.RemoveRange(testStudents);
                var deletedCount = await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = $"Deleted {testStudents.Count} test students",
                    deletedCount = testStudents.Count
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cleaning up test data");
                return BadRequest(new
                {
                    success = false,
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Obtener información del sistema
        /// </summary>
        [HttpGet("system-info")]
        public ActionResult<object> GetSystemInfo()
        {
            return Ok(new
            {
                serverTime = DateTime.UtcNow,
                localTime = DateTime.Now,
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                machineName = Environment.MachineName,
                osVersion = Environment.OSVersion.ToString(),
                dotnetVersion = Environment.Version.ToString(),
                workingDirectory = Directory.GetCurrentDirectory(),
                assemblies = AppDomain.CurrentDomain.GetAssemblies()
                    .Select(a => new {
                        name = a.GetName().Name,
                        version = a.GetName().Version?.ToString()
                    })
                    .Where(a => a.name?.StartsWith("AcademiaCostura") == true)
                    .ToList()
            });
        }
    }

    // Modelo para crear estudiante de prueba
    public class CreateTestStudentRequest
    {
        public string? Name { get; set; }
        public string? DNI { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
    }
}