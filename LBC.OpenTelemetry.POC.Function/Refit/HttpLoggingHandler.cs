using Microsoft.Extensions.Logging;
using SerilogTimings;
using System.Net.Http.Headers;

namespace LBC.OpenTelemetry.POC.Function.Refit;

public class HttpLoggingHandler<TService> : DelegatingHandler
{
    private readonly ILogger<TService> _logger;

    public HttpLoggingHandler(ILogger<TService> logger)
    {
        _logger = logger;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using (Operation.Time("Sending request to {Uri}", request.RequestUri))
        {
            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            return response;
        }
    }

    private readonly string[] ContentTypes = new[] { "html", "text", "xml", "json", "txt", "x-www-form-urlencoded" };

    private bool IsLoggable(HttpContent content, HttpHeaders headers)
    {
        if (content == null || content.Headers == null)
            return false;

        return content is StringContent || IsTextBasedContentType(headers) || IsTextBasedContentType(content.Headers);
    }

    private async Task<Dictionary<string, object>> GetContext(HttpRequestMessage request, HttpResponseMessage response)
    {
        var context = new Dictionary<string, object>();

        if ((int)response.StatusCode >= 300)
        {
            if (IsLoggable(request.Content, request.Headers))
            {
                var requestContent = await GetHttpContentAsync(request.Content, limit: 8192);
                context.Add("RequestBody", requestContent);
            }

            if (IsLoggable(response.Content, response.Headers))
            {
                var responseContent = await GetHttpContentAsync(response.Content, limit: 8192);
                context.Add("ResponseBody", responseContent);
            }
        }

        return context;
    }

    private bool IsTextBasedContentType(HttpHeaders headers)
    {
        if (!headers.TryGetValues("Content-Type", out var values))
            return false;

        var header = string.Join(" ", values).ToLowerInvariant();

        return ContentTypes.Any(t => header.Contains(t));
    }

    private async Task<string> GetHttpContentAsync(HttpContent httpContent, int limit)
    {
        var content = await httpContent.ReadAsStringAsync();
        return content.Length > limit ? content[..limit] : content;
    }
}