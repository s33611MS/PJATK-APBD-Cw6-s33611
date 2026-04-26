using APBD6.DTOs;

namespace APBD6.Exceptions;

public class NotFoundException (ErrorResponseDto error) : Exception($"Appointment with id: {error.IdAppointment} not found");