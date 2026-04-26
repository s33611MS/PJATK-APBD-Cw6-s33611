using APBD6.DTOs;

namespace APBD6.Exceptions;

public class BadRequestException(ErrorResponseDto error) : Exception(error.Massage);