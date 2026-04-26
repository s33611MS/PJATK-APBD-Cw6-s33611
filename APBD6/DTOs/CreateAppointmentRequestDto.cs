using System.ComponentModel.DataAnnotations;

namespace APBD6.DTOs;

public class CreateAppointmentRequestDto
{
    [Required]
    public int IdPatient { get; set; }
    [Required]
    public int IdDoctor { get; set; }
    [Required]
    public DateTime AppointmentDate { get; set; }
    [Required]
    public string Reason { get; set; } = string.Empty;
}