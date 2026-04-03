// TDD Ciclos 7-11 — GREEN: mínimo para pasar todos los tests de JoinWaitlist

using MediatR;
using Waitlist.Application.Exceptions;
using Waitlist.Application.Ports;
using Waitlist.Domain.Entities;

namespace Waitlist.Application.UseCases.JoinWaitlist;

public class JoinWaitlistHandler : IRequestHandler<JoinWaitlistCommand, JoinWaitlistResult>
{
    private readonly IWaitlistRepository _repo;
    private readonly ICatalogClient      _catalog;

    public JoinWaitlistHandler(IWaitlistRepository repo, ICatalogClient catalog)
    {
        _repo    = repo;
        _catalog = catalog;
    }

    public async Task<JoinWaitlistResult> Handle(JoinWaitlistCommand command, CancellationToken cancellationToken)
    {
        // Ciclo 11: Catalog 503 guard
        int availableCount;
        try
        {
            availableCount = await _catalog.GetAvailableCountAsync(command.EventId, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new WaitlistServiceUnavailableException("Catalog service is unavailable.", ex);
        }

        // Ciclo 8: stock > 0 → 409
        if (availableCount > 0)
            throw new WaitlistConflictException(
                "Hay tickets disponibles para este evento. La lista de espera no aplica.");

        // Ciclo 9: duplicado activo → 409
        var hasActive = await _repo.HasActiveEntryAsync(command.Email, command.EventId, cancellationToken);
        if (hasActive)
            throw new WaitlistConflictException(
                "Ya estás en la lista de espera de este evento.");

        // Ciclo 7: crear entry y persistir
        var entry    = WaitlistEntry.Create(command.Email, command.EventId);
        await _repo.AddAsync(entry, cancellationToken);

        var position = await _repo.GetQueuePositionAsync(command.EventId, cancellationToken);
        return new JoinWaitlistResult(entry.Id, position);
    }
}
