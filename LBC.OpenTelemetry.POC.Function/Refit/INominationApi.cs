using Refit;

namespace LBC.OpenTelemetry.POC.Function.Refit;

[Headers("Accept: application/json")]
public interface INominationApi
{
    [Post("/api/nomination")]
    Task CreateNominationAsync(Montova montova, CancellationToken cancellationToken = default);
}