using FluentValidation;
using GestaoViaturasAPI.DTOs;
namespace GestaoViaturasAPI.Validators;
public class CriarViaturaDTOValidator : AbstractValidator<CriarViaturaDTO>
{
    public CriarViaturaDTOValidator()
    {
        RuleFor(x => x.Placa).NotEmpty().Must(Viatura.PlacaValida).WithMessage("Informe uma placa válida, sem hífen.");
        RuleFor(x => x.Modelo).NotEmpty().MaximumLength(50);
        RuleFor(x => x.AnoFabricacao).GreaterThan(2000).Must(ano => ano <= DateTime.UtcNow.Year + 1);
        RuleFor(x => x.QuilometragemInicial).GreaterThanOrEqualTo(0);
    }
}
