using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SecureLab.Api.Application.Incidents;
using SecureLab.Api.Data.Entities; // <-- ДОДАНО ДЛЯ IncidentStatus

namespace SecureLab.Api.Presentation.Endpoints;

public static class IncidentEndpoints
{
    public static IEndpointRouteBuilder MapIncidentEndpoints(this IEndpointRouteBuilder routes)
    {
        var group = routes.MapGroup("/api/incidents");

        group.MapGet("/", GetListAsync);
        group.MapGet("/{id:guid}", GetDetailsAsync);

        return routes;
    }

    private static async Task<IResult> GetListAsync(
        string? status,
        IncidentQueries queries,
        CancellationToken cancellationToken)
    {
        IncidentStatus? parsedStatus = null;
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<IncidentStatus>(status, true, out var parsed))
        {
            parsedStatus = parsed;
        }

        var incidents = await queries.GetListAsync(parsedStatus, cancellationToken);
        return Results.Ok(incidents);
    }

    private static async Task<IResult> GetDetailsAsync(
        Guid id,
        IncidentQueries queries,
        CancellationToken cancellationToken)
    {
        var incident = await queries.GetDetailsAsync(id, cancellationToken);
        return incident is not null
            ? Results.Ok(incident)
            : Results.NotFound();
    }
}