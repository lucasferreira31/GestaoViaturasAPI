using FluentValidation;
using GestaoViaturasAPI.Data;
using GestaoViaturasAPI.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace GestaoViaturasAPI.Controllers;

[ApiController, Route("api/[controller]"), Authorize]
public class ViaturasController(AppDbContext context) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> ListarViaturas(CancellationToken ct)
        => Ok(await context.Viaturas.AsNoTracking().OrderBy(v => v.Id).ToListAsync(ct));

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetViatura(int id, CancellationToken ct)
    {
        var viatura = await context.Viaturas.AsNoTracking().SingleOrDefaultAsync(v => v.Id == id, ct);
        return viatura is null ? NotFound() : Ok(viatura);
    }

    [HttpPost]
    public async Task<IActionResult> CriarViatura(CriarViaturaDTO dto,
        [FromServices] IValidator<CriarViaturaDTO> validator, CancellationToken ct)
    {
        var result = await validator.ValidateAsync(dto, ct);
        if (!result.IsValid)
            return BadRequest(new ValidationProblemDetails(result.ToDictionary()) { Status = 400 });
        var placa = dto.Placa.Trim().ToUpperInvariant();
        if (await context.Viaturas.AnyAsync(v => v.Matricula == placa, ct))
            return Problem(statusCode: 409, title: "Já existe uma viatura com essa placa.");
        var viatura = Viatura.Criar(placa, dto.Modelo, dto.AnoFabricacao, dto.QuilometragemInicial);
        context.Viaturas.Add(viatura);
        await context.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetViatura), new { id = viatura.Id }, viatura);
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> PutViatura(int id, AtualizarViaturaDTO dto,
        [FromServices] IValidator<AtualizarViaturaDTO> validator, CancellationToken ct)
    {
        var result = await validator.ValidateAsync(dto, ct);
        if (!result.IsValid)
            return BadRequest(new ValidationProblemDetails(result.ToDictionary()) { Status = 400 });
        var viatura = await context.Viaturas.SingleOrDefaultAsync(v => v.Id == id, ct);
        if (viatura is null) return NotFound();
        var placa = dto.Placa.Trim().ToUpperInvariant();
        if (await context.Viaturas.AnyAsync(v => v.Id != id && v.Matricula == placa, ct))
            return Problem(statusCode: 409, title: "Já existe uma viatura com essa placa.");
        viatura.Atualizar(placa, dto.Modelo, dto.AnoFabricacao, dto.QuilometragemAtual, dto.Estado);
        await context.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> DeleteViatura(int id, CancellationToken ct)
    {
        var viatura = await context.Viaturas.SingleOrDefaultAsync(v => v.Id == id, ct);
        if (viatura is null) return NotFound();
        context.Viaturas.Remove(viatura);
        await context.SaveChangesAsync(ct);
        return NoContent();
    }
}
