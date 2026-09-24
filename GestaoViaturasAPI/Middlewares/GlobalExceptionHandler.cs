using Microsoft.AspNetCore.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
namespace GestaoViaturasAPI.Middlewares;
public class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger, IProblemDetailsService service) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        var (status, title) = exception switch
        {
            RegraDeNegocioException => (409, exception.Message),
            DbUpdateConcurrencyException => (409, "A viatura foi alterada por outra requisição. Consulte os dados e tente novamente."),
            DbUpdateException { InnerException: SqlException { Number: 2601 or 2627 } }
                => (409, "Já existe um registro com esses dados únicos."),
            _ => (500, "Ocorreu um erro inesperado ao processar sua requisição.")
        };
        if (status == 500) logger.LogError(exception, "Erro não tratado. TraceId: {TraceId}", httpContext.TraceIdentifier);
        httpContext.Response.StatusCode = status;
        await service.WriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new Microsoft.AspNetCore.Mvc.ProblemDetails
            {
                Status = status, Title = title, Instance = httpContext.Request.Path,
                Extensions = { ["traceId"] = httpContext.TraceIdentifier }
            }
        });
        return true;
    }
}
