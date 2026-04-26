using System.Text;
using APBD6.DTOs;
using APBD6.Exceptions;
using Microsoft.Data.SqlClient;

namespace APBD6.Services;

public class AppointmentService(IConfiguration configuration) : IAppointmentService
{
    public async Task<IEnumerable<AppointmentListDto>> GetAllAsync(string? status, string? patientLastName, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection"));
        await using var command = new SqlCommand();
        command.Connection = connection;
        
        var result = new List<AppointmentListDto>();
        
        var sqlCommand = new StringBuilder("""
                                           SELECT a.IdAppointment, a.AppointmentDate, a.Status, a.Reason, p.FirstName + N' ' + p.LastName AS PatientFullName, p.Email AS PatientEmail
                                           FROM Appointments a JOIN Patients p ON p.IdPatient = a.IdPatient
                                           WHERE (@Status IS NULL OR a.Status = @Status)
                                             AND (@PatientLastName IS NULL OR p.LastName = @PatientLastName)
                                           ORDER BY a.AppointmentDate;
                                           """);
        
        command.Connection = connection;
        command.CommandText = sqlCommand.ToString();
        
        var parameters = new List<SqlParameter>();

        parameters.Add(new SqlParameter("@Status", status is not null ? status : DBNull.Value));
        
        parameters.Add(new SqlParameter("@PatientLastName", patientLastName is not null ? patientLastName : DBNull.Value));
        
        command.Parameters.AddRange(parameters.ToArray());

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
        await using var connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection"));
        await using var command = new SqlCommand();
        command.Connection = connection;
        
        AppointmentDetailsDto? result = null;
        
        const string sqlCommand = """
                                  select a.IdAppointment, a.AppointmentDate, a.Status, a.Reason, 
                                  CONCAT(p.FirstName, ' ', p.LastName), p.Email, p.PhoneNumber, 
                                  d.LicenseNumber, a.InternalNotes, a.CreatedAt
                                  from Appointments a join Patients p on a.IdPatient = p.IdPatient join Doctors d on a.IdDoctor = d.IdDoctor
                                  WHERE a.IdAppointment = @Id
                                  """;
        
        command.Connection = connection;
        command.CommandText = sqlCommand;
        command.Parameters.AddWithValue("@Id", id);

        await connection.OpenAsync(cancellationToken);

        var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            result ??= new AppointmentDetailsDto
            {
                IdAppointment = reader.GetInt32(0),
                AppointmentDate = reader.GetDateTime(1),
                Status = reader.GetString(2),
                Reason = reader.GetString(3),
                PatientFullName = reader.GetString(4),
                PatientEmail = reader.GetString(5),
                PhoneNumber = reader.GetString(6),
                DoctorsLicenseNumber = reader.GetString(7),
                InternalNotes = reader.IsDBNull(8) ? null : reader.GetString(8),
                CreatedAt =  reader.GetDateTime(9),
            };
        }

        if (result is null)
        {
            throw new NotFoundException(new ErrorResponseDto{Massage = $"There is no Appointment with id: {id}"});
        }
        
        return result;
    }

    public async Task AddAsync(CreateAppointmentRequestDto dto, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection"));
        await using var command = new SqlCommand();

        await connection.OpenAsync(cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        command.Connection = connection;
        command.Transaction = (SqlTransaction)transaction;


        await ExistsAsync("select 1 from Patients where IdPatient = @Id AND IsActive = 1",
            [new SqlParameter("@Id", dto.IdPatient)], $"There is no active patient with id: {dto.IdDoctor}",
            command, cancellationToken);

        await ExistsAsync("select 1 from Doctors where IdDoctor = @Id AND IsActive = 1",
            [new SqlParameter("@Id", dto.IdDoctor)], $"There is no active doctor with id: {dto.IdDoctor}",
            command, cancellationToken);

        if (DateTime.Now > dto.AppointmentDate)
            throw new BadRequestException(new ErrorResponseDto { Massage = "Appointment date cannot be in the past." });

        command.CommandText =
            "SELECT 1 from Appointments WHERE AppointmentDate = @AppointmentDate AND @IdDoctor = IdDoctor";
        command.Parameters.AddWithValue("@AppointmentDate", dto.AppointmentDate);
        command.Parameters.AddWithValue("@IdDoctor", dto.IdDoctor);
        var exists = await command.ExecuteScalarAsync(cancellationToken);

        if (exists is not null)
            throw new ConflictException(new ErrorResponseDto
            {
                Massage =
                    $"There is already appointment for doctor with id: {dto.IdDoctor} on date: {dto.AppointmentDate}"
            });

        command.Parameters.Clear();
        
        try
        {

            command.CommandText = """
                                  insert into Appointments (IdPatient, IdDoctor, AppointmentDate, Status, Reason)
                                  output inserted.IdAppointment
                                  values (@IdPatient, @IdDoctor, @AppointmentDate, @Status, @Reason)
                                  """;

            command.Parameters.AddWithValue("@IdPatient", dto.IdPatient);
            command.Parameters.AddWithValue("@IdDoctor", dto.IdDoctor);
            command.Parameters.AddWithValue("@AppointmentDate", dto.AppointmentDate);
            command.Parameters.AddWithValue("@Status", "Scheduled");
            command.Parameters.AddWithValue("@Reason", dto.Reason);

            await command.ExecuteNonQueryAsync(cancellationToken);
            command.Parameters.Clear();

            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task UpdateAsync(int id, UpdateAppointmentRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection"));
        await using var command = new SqlCommand();

        await connection.OpenAsync(cancellationToken);

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        command.Connection = connection;
        command.Transaction = (SqlTransaction)transaction;


        await ExistsAsync("select 1 from Patients where IdPatient = @Id AND IsActive = 1",
            [new SqlParameter("@Id", dto.IdPatient)], $"There is no active patient with id: {dto.IdPatient}",
            command, cancellationToken);

        await ExistsAsync("select 1 from Doctors where IdDoctor = @Id AND IsActive = 1",
            [new SqlParameter("@Id", dto.IdDoctor)], $"There is no active doctor with id: {dto.IdDoctor}",
            command, cancellationToken);

        var possibleStatus = new List<string> { "Scheduled", "Completed", "Cancelled" };
        if (!possibleStatus.Contains(dto.Status))
            throw new BadRequestException(new ErrorResponseDto
                { Massage = "Status should be either: scheduled, completed, or cancelled" });

        command.CommandText = "SELECT Status, AppointmentDate from Appointments WHERE IdAppointment = @IdAppointment";
        command.Parameters.AddWithValue("@IdAppointment", id);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        if (!await reader.ReadAsync(cancellationToken))
            throw new NotFoundException(new ErrorResponseDto { Massage = $"There is no appointment with id: {id}" });

        if (reader.GetString(0) == "Completed")
        {
            dto.AppointmentDate = reader.GetDateTime(1);
            reader.Close();
        }
        else
        {
            reader.Close();
            command.CommandText = "SELECT 1 from Appointments WHERE AppointmentDate = @AppointmentDate AND @IdDoctor = IdDoctor";
            command.Parameters.AddWithValue("@AppointmentDate", dto.AppointmentDate);
            command.Parameters.AddWithValue("@IdDoctor", dto.IdDoctor);
            var exists = await command.ExecuteScalarAsync(cancellationToken);

            if (exists is not null)
                throw new ConflictException(new ErrorResponseDto
                {
                    Massage =
                        $"There is already appointment for doctor with id: {dto.IdDoctor} on date: {dto.AppointmentDate}"
                });
        }

        command.Parameters.Clear();
        
        try
        {
            command.CommandText = """
                                  update Appointments set
                                  IdPatient = @IdPatient,
                                  IdDoctor = @IdDoctor,
                                  AppointmentDate = @AppointmentDate,
                                  Status = @Status,
                                  Reason = @Reason,
                                  InternalNotes = @InternalNotes
                                  where IdAppointment = @Id
                                  """;

            command.Parameters.AddWithValue("@Id", id);
            command.Parameters.AddWithValue("@IdPatient", dto.IdPatient);
            command.Parameters.AddWithValue("@IdDoctor", dto.IdDoctor);
            command.Parameters.AddWithValue("@AppointmentDate", dto.AppointmentDate);
            command.Parameters.AddWithValue("@Status", dto.Status);
            command.Parameters.AddWithValue("@Reason", dto.Reason);
            command.Parameters.AddWithValue("@InternalNotes", dto.InternalNotes);

            await command.ExecuteNonQueryAsync(cancellationToken);
            command.Parameters.Clear();

            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task RemoveAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var connection = new SqlConnection(configuration.GetConnectionString("DefaultConnection"));
        await using var command = new SqlCommand();
        
        command.Connection = connection;
        
        await connection.OpenAsync(cancellationToken);
        
        command.CommandText = "SELECT Status from Appointments WHERE IdAppointment = @IdAppointment";
        command.Parameters.AddWithValue("@IdAppointment", id);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        
        if (!await reader.ReadAsync(cancellationToken))
            throw new NotFoundException(new ErrorResponseDto{Massage = $"There is no appointment with id: {id}"});
            
        if (reader.GetString(0) == "Completed")
        {
            throw new ConflictException(new ErrorResponseDto{Massage = "Completed appointment cannot be removed"});
        }
        
        reader.Close();

        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);
        command.Transaction = (SqlTransaction)transaction;

        try
        {
            command.CommandText = "delete from Appointments where IdAppointment = @Id";
            command.Parameters.AddWithValue("@Id", id);
            await command.ExecuteNonQueryAsync(cancellationToken);
            
            command.Parameters.Clear();
            
            await transaction.CommitAsync(cancellationToken);
        }
        catch (Exception)
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static async Task ExistsAsync(string query, List<SqlParameter> parameters, string msg, SqlCommand command, CancellationToken cancellationToken = default)
    {
        command.CommandText = query;
        command.Parameters.AddRange(parameters.ToArray());
        var exists = await command.ExecuteScalarAsync(cancellationToken);
            
        if (exists is null) 
            throw new NotFoundException(new ErrorResponseDto{Massage = msg});
            
        command.Parameters.Clear();
    }
}