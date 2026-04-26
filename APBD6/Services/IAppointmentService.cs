using APBD6.DTOs;

namespace APBD6.Services;

public interface IAppointmentService
{
    Task<IEnumerable<AppointmentListDto>> GetAllAsync(string? status, string? patientLastName, CancellationToken cancellationToken = default);
    Task<AppointmentDetailsDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(CreateAppointmentRequestDto dto, CancellationToken cancellationToken = default);
    Task UpdateAsync(int id, UpdateAppointmentRequestDto dto, CancellationToken cancellationToken = default);
    Task RemoveAsync(int id, CancellationToken cancellationToken = default);
}