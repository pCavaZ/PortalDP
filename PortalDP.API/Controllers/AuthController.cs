using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using PortalDP.Application.DTOs;
using PortalDP.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;


namespace PortalDP.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IStudentService _studentService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<AuthController> _logger;

        public AuthController(
            IStudentService studentService,
            IConfiguration configuration,
            ILogger<AuthController> logger)
        {
            _studentService = studentService;
            _configuration = configuration;
            _logger = logger;
        }

        /// <summary>
        /// Autenticación con DNI - para estudiantes usar DNI, para admin usar "ADMIN"
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<ApiResponse<LoginResponseDto>>> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                _logger.LogInformation("Login attempt for DNI: {DNI}", loginDto.DNI);

                if (string.IsNullOrWhiteSpace(loginDto.DNI))
                {
                    return BadRequest(ApiResponse<LoginResponseDto>.ErrorResponse("DNI is required"));
                }

                var normalizedDni = loginDto.DNI.Trim().ToUpper();

                // Verificar si es login de administrador
                if (normalizedDni == "ADMIN")
                {
                    _logger.LogInformation("Admin login successful");

                    var adminToken = GenerateJwtToken("ADMIN", "Administrator", true, 0);
                    var adminResponse = new LoginResponseDto
                    {
                        Token = adminToken,
                        RefreshToken = GenerateRefreshToken(),
                        IsAdmin = true,
                        ExpiresAt = DateTime.UtcNow.AddHours(24),
                        Student = null
                    };

                    return Ok(ApiResponse<LoginResponseDto>.SuccessResponse(adminResponse, "Admin login successful"));
                }

                // Login de estudiante regular
                var studentResult = await _studentService.GetStudentByDniAsync(normalizedDni);
                if (!studentResult.Success || studentResult.Data == null)
                {
                    _logger.LogWarning("Login failed for DNI: {DNI} - Student not found", normalizedDni);
                    return Unauthorized(ApiResponse<LoginResponseDto>.ErrorResponse("DNI not found or inactive"));
                }

                var student = studentResult.Data;
                var token = GenerateJwtToken(student.DNI, student.Name, false, student.Id);
                var response = new LoginResponseDto
                {
                    Token = token,
                    RefreshToken = GenerateRefreshToken(),
                    Student = student,
                    IsAdmin = false,
                    ExpiresAt = DateTime.UtcNow.AddHours(24)
                };

                _logger.LogInformation("Student login successful for {StudentName} (ID: {StudentId})", student.Name, student.Id);
                return Ok(ApiResponse<LoginResponseDto>.SuccessResponse(response, "Login successful"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login for DNI: {DNI}", loginDto.DNI);
                return StatusCode(500, ApiResponse<LoginResponseDto>.ErrorResponse("Internal server error during login"));
            }
        }

        /// <summary>
        /// Verificar si un DNI existe en el sistema
        /// </summary>
        [HttpPost("validate-dni")]
        public async Task<ActionResult<ApiResponse<bool>>> ValidateDni([FromBody] LoginDto loginDto)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(loginDto.DNI))
                {
                    return BadRequest(ApiResponse<bool>.ErrorResponse("DNI is required"));
                }

                var normalizedDni = loginDto.DNI.Trim().ToUpper();

                // Admin siempre es válido
                if (normalizedDni == "ADMIN")
                {
                    return Ok(ApiResponse<bool>.SuccessResponse(true));
                }

                var result = await _studentService.ValidateStudentDniAsync(normalizedDni);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating DNI: {DNI}", loginDto.DNI);
                return StatusCode(500, ApiResponse<bool>.ErrorResponse("Internal server error"));
            }
        }

        /// <summary>
        /// Refrescar token JWT
        /// </summary>
        [HttpPost("refresh-token")]
        public ActionResult<ApiResponse<LoginResponseDto>> RefreshToken([FromBody] RefreshTokenDto refreshTokenDto)
        {
            try
            {
                // En una implementación real, aquí validarías el refresh token contra una base de datos
                // Por simplicidad, generamos un nuevo token

                if (string.IsNullOrWhiteSpace(refreshTokenDto.RefreshToken))
                {
                    return BadRequest(ApiResponse<LoginResponseDto>.ErrorResponse("Refresh token is required"));
                }

                // Aquí iría la lógica de validación del refresh token
                // Por ahora retornamos error ya que no tenemos implementado el almacenamiento de refresh tokens

                return BadRequest(ApiResponse<LoginResponseDto>.ErrorResponse("Refresh token functionality not implemented"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error refreshing token");
                return StatusCode(500, ApiResponse<LoginResponseDto>.ErrorResponse("Internal server error"));
            }
        }

        #region Private Methods

        private string GenerateJwtToken(string dni, string name, bool isAdmin, int studentId)
        {
            var jwtSettings = _configuration.GetSection("JwtSettings");
            var secretKey = jwtSettings["SecretKey"];

            if (string.IsNullOrEmpty(secretKey))
            {
                throw new InvalidOperationException("JWT SecretKey not configured");
            }

            var key = Encoding.UTF8.GetBytes(secretKey);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, dni),
                new Claim(ClaimTypes.Name, name),
                new Claim("DNI", dni),
                new Claim("IsAdmin", isAdmin.ToString().ToLower()),
                new Claim("StudentId", studentId.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString(), ClaimValueTypes.Integer64)
            };

            if (isAdmin)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }
            else
            {
                claims.Add(new Claim(ClaimTypes.Role, "Student"));
            }

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddHours(24),
                Issuer = jwtSettings["Issuer"],
                Audience = jwtSettings["Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var token = tokenHandler.CreateToken(tokenDescriptor);

            return tokenHandler.WriteToken(token);
        }

        private string GenerateRefreshToken()
        {
            // En una implementación real, esto sería un token más complejo almacenado en base de datos
            return Convert.ToBase64String(Guid.NewGuid().ToByteArray());
        }

        #endregion
    }
}
