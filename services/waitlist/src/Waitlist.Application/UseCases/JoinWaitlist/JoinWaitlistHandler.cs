using MediatR;
using Waitlist.Application.Ports;
using Waitlist.Domain.Entities;
using Waitlist.Domain.Exceptions;

namespace Waitlist.Application.UseCases.JoinWaitlist;

public class JoinWaitlistHandler : IRequestHandler<JoinWaitlistCommand, JoinWaitlistResult>
{
    private readonly IWaitlistRepository _repository;
    private readonly ICatalogClient _catalogClient;

    public JoinWaitlistHandler(IWaitlistRepository repository, ICatalogClient catalogClient)
    {
        _repository = repository;
        _catalogClient = catalogClient;
    }

    public async Task<JoinWaitlistResult> Handle(JoinWaitlistCommand request, CancellationToken cancellationToken)
    {
        int availableCount;
        try
        {
            availableCount = await _catalogClient.GetAvailableCountAsync(request.EventId, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new WaitlistServiceUnavailableException("Catalog service is unavailable.", ex);
        }

        if (availableCount > 0)
            throw new WaitlistConflictException("Seats are still available. Waitlist is not open yet.");

        var hasActive = await _repository.HasActiveEntryAsync(request.Email, request.EventId, cancellationToken);
        if (hasActive)
            throw new WaitlistConflictException("An active waitlist entry already exists for this email and event.");

        var entry = WaitlistEntry.Create(request.Email, request.EventId);
        await _repository.AddAsync(entry, cancellationToken);

        var position = await _repository.GetQueuePositionAsync(entry.Id, cancellationToken);

        return new JoinWaitlistResult(entry.Id, position, entry.Email, entry.EventId);
    }
}
