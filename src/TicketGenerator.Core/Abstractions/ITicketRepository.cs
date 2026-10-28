using TicketGenerator.Core.Models;

namespace TicketGenerator.Core.Abstractions;

public interface ITicketRepository
{
    void Append(TicketRecord record);
}
