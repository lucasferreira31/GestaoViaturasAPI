using FluentValidation;
using GestaoViaturasAPI.DTOs;
namespace GestaoViaturasAPI.Validators;
public class AtualizarViaturaDTOValidator : AbstractValidator<AtualizarViaturaDTO>
{
    public AtualizarViaturaDTOValidator()
    {
        RuleFor(x => x.Placa).NotEmpty().Must(Viatura.PlacaValida).WithMessage("Informe uma placa válida, sem hífen.");
        RuleFor(x => x.Modelo).NotEmpty().MaximumLength(50);
        RuleFor(x => x.AnoFabricacao).GreaterThan(2000).Must(ano => ano <= DateTime.UtcNow.Year + 1);
        RuleFor(x => x.QuilometragemAtual).GreaterThanOrEqualTo(0);
        RuleFor(x => x.Estado).Must(estado => estado is Viatura.Disponivel or Viatura.EmPatrulha or Viatura.Manutencao)
            .WithMessage("Use Disponível, Em Patrulha ou Manutenção.");
    }
}
