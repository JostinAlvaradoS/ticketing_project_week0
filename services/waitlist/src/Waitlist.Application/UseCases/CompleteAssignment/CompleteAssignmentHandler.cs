using MediatR;
using Waitlist.Application.Ports;

namespace Waitlist.Application.UseCases.CompleteAssignment;

public class CompleteAssignmentHandler : IRequestHandler<CompleteAssignmentCommand>
{
    private readonly IWaitlistRepository _repository;

    public CompleteAssignmentHandler(IWaitlistRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(CompleteAssignmentCommand request, CancellationToken cancellationToken)
    {
        var entry = await _repository.GetByIdAsync(request.EntryId, cancellationToken);
        if (entry is null) return;

        entry.Complete();
        await _repository.UpdateAsync(entry, cancellationToken);
    }
}
