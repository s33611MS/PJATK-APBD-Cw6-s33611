using APBD6.DTOs;

namespace APBD6.Services;

public interface IAppointmentService
{
    Task<IEnumerable<AppointmentListDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<AppointmentDetailsDto> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<CreateAppointmentRequestDto> AddAsync(CreateAppointmentRequestDto dto, CancellationToken cancellationToken = default);
    Task UpdateAsync(int id, UpdateAppointmentRequestDto dto, CancellationToken cancellationToken = default);
    Task RemoveAsync(int id, CancellationToken cancellationToken = default);
}