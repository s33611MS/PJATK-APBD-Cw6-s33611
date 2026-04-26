using System.Text;
using APBD6.DTOs;
using Microsoft.Data.SqlClient;

namespace APBD6.Services;

public class AppointmentService(IConfiguration configuration) : IAppointmentService
{
    public async Task<IEnumerable<AppointmentListDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection"));
        await using var command = new SqlCommand();
        command.Connection = connection;
        
        var result = new List<AppointmentListDto>();
        
        var sqlCommand = new StringBuilder("""
                                           select a.IdAppointment, a.AppointmentDate, a.Status, a.Reason, CONCAT(p.FirstName, ' ', p.LastName), p.Email 
                                           from Appointments a join Patients p on a.IdPatient = p.IdPatient
                                           """);

        command.Connection = connection;
        command.CommandText = sqlCommand.ToString();

        await connection.OpenAsync(cancellationToken);
        
        var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            result.Add(new AppointmentListDto
            {
                IdAppointment = reader.GetInt32(0),
                AppointmentDate = reader.GetDateTime(1),
                Status = reader.GetString(2),
                Reason = reader.GetString(3),
                PatientFullName = reader.GetString(4),
                PatientEmail = reader.GetString(5),
            });
        }
        
        return result;
    }

    public async Task<AppointmentDetailsDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task<CreateAppointmentRequestDto> AddAsync(CreateAppointmentRequestDto dto, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
    
    public async Task UpdateAsync(int id, UpdateAppointmentRequestDto dto, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }

    public async Task RemoveAsync(int id, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}