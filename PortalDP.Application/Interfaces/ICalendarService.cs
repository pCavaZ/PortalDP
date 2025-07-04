using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PortalDP.Application.DTOs;

namespace PortalDP.Application.Interfaces
{
    public interface ICalendarService
    {
        Task<ApiResponse<CalendarDto>> GetStudentCalendarAsync(int studentId, int year, int month);
        Task<ApiResponse<List<AvailableSlotDto>>> GetAvailableSlotsAsync(DateOnly date);
        Task<ApiResponse<bool>> CanCancelClassAsync(int studentId, DateOnly classDate);
        Task<ApiResponse<ClassCancellationDto>> CancelClassAsync(int studentId, CreateClassCancellationDto cancellationDto);
        Task<ApiResponse<RecoveryClassDto>> BookRecoveryClassAsync(int studentId, CreateRecoveryClassDto recoveryDto);
        Task<ApiResponse<List<ClassCancellationDto>>> GetAvailableRecoveryClassesAsync(int studentId);
    }
}
