using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PortalDP.Application.DTOs;

namespace PortalDP.Application.Interfaces
{
    public interface IStudentService
    {
        Task<ApiResponse<List<StudentDto>>> GetAllStudentsAsync();
        Task<ApiResponse<StudentDto>> GetStudentByIdAsync(int id);
        Task<ApiResponse<StudentDto>> GetStudentByDniAsync(string dni);
        Task<ApiResponse<StudentDto>> CreateStudentAsync(CreateStudentDto createStudentDto);
        Task<ApiResponse<StudentDto>> UpdateStudentAsync(int id, UpdateStudentDto updateStudentDto);
        Task<ApiResponse<bool>> DeleteStudentAsync(int id);
        Task<ApiResponse<bool>> ValidateStudentDniAsync(string dni); 
        Task<ApiResponse<bool>> CheckScheduleCapacityAsync(int dayOfWeek, TimeSpan startTime, TimeSpan endTime, int? excludeStudentId = null);
    }

}
