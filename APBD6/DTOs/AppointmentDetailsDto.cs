namespace APBD6.DTOs;

public class AppointmentDetailsDto
{
    public int IdAppointment { get; set; }
    public DateTime AppointmentDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public string PatientFullName { get; set; } = string.Empty;
    public string PatientEmail { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string DoctorsLicenseNumber { get; set; } =  string.Empty;
    public string InternalNotes { get; set; } =  string.Empty;
    public DateTime CreatedAt { get; set; }
}