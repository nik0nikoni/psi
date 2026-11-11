using TicketGenerator.Core.Abstractions;
using TicketGenerator.Core.Generation;
using TicketGenerator.Core.Models;
using TicketGenerator.Core.Validation;

namespace TicketGenerator.Core.Services;

public sealed class TicketIssuingService
{
    private readonly StudentNameValidator _validator;
    private readonly TicketNumberGenerator _generator;
    private readonly IClock _clock;
    private readonly ITicketRepository _repository;

    public TicketIssuingService(
        StudentNameValidator validator,
        TicketNumberGenerator generator,
        IClock clock,
        ITicketRepository repository)
    {
        _validator = validator;
        _generator = generator;
        _clock = clock;
        _repository = repository;
    }

    public TicketRecord Issue(string lastName, string firstName)
    {
        if (!_validator.TryValidate(lastName, out var normalizedLastName))
        {
            throw new ArgumentException("Фамилия не может быть пустой.", nameof(lastName));
        }

        if (!_validator.TryValidate(firstName, out var normalizedFirstName))
        {
            throw new ArgumentException("Имя не может быть пустым.", nameof(firstName));
        }

        var record = new TicketRecord(
            normalizedLastName,
            normalizedFirstName,
            _generator.Generate(),
            _clock.Now);

        _repository.Append(record);
        return record;
    }
}
