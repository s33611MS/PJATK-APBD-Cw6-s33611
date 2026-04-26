using APBD6.Exceptions;
using APBD6.Services;
using APBD6.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace APBD6.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AppointmentsController(IAppointmentService service) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? status, [FromQuery] string? patientLastName, CancellationToken cancellationToken)
    {
        return Ok(await service.GetAllAsync(status, patientLastName, cancellationToken));
    }
    
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetByIdAsync([FromRoute] int id, CancellationToken cancellationToken = default)
    {
        try
        {
            return Ok(await service.GetByIdAsync(id, cancellationToken));
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
    }
    
    [HttpPost]
    public async Task<IActionResult> CreateAsync([FromBody] CreateAppointmentRequestDto dto, CancellationToken cancellationToken)
    {
        try
        {
            await service.AddAsync(dto, cancellationToken);
            return Created();
        }
        catch (NotFoundException e)
        {
            return NotFound(e.Message);
        }
        catch (ConflictException e)
        {
            return Conflict(e.Message);
        }
    }
}