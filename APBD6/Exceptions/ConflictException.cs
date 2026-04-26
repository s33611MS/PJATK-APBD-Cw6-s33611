using APBD6.DTOs;

namespace APBD6.Exceptions;

public class ConflictException (ErrorResponseDto error) : Exception(error.Massage);