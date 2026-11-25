using System.Text.RegularExpressions;
using ExamTickets.Core.Abstractions;
using ExamTickets.Core.Models;

namespace ExamTickets.Core.Parsing;

public sealed partial class TicketParser
{
    public const int QuestionsPerTicket = 3;

    private readonly IAppLogger _logger;

    public TicketParser(IAppLogger logger)
    {
        _logger = logger;
    }

    public IReadOnlyList<Ticket> Parse(IEnumerable<string> paragraphs)
    {
        var tickets = new List<Ticket>();
        TicketDraft? draft = null;

        foreach (var paragraph in paragraphs)
        {
            var text = paragraph.Trim();
            if (text.Length == 0)
            {
                continue;
            }

            var header = HeaderPattern().Match(text);
            if (header.Success)
            {
                Complete(draft, tickets);
                draft = TicketDraft.WithHeader(int.Parse(header.Groups["number"].Value), text);
                continue;
            }

            var question = QuestionPattern().Match(text);
            if (question.Success)
            {
                draft ??= TicketDraft.WithoutHeader(text);
                draft.Questions.Add(question.Groups["text"].Value.Trim());
                continue;
            }

            // An unrecognised line opens a block that is skipped together with its questions.
            Complete(draft, tickets);
            draft = TicketDraft.WithoutHeader(text);
        }

        Complete(draft, tickets);
        return tickets;
    }

    private void Complete(TicketDraft? draft, List<Ticket> tickets)
    {
        if (draft is null)
        {
            return;
        }

        if (draft.Number is not { } number)
        {
            _logger.Error($"Билет пропущен: не распознан заголовок \"{draft.FirstLine}\".");
            return;
        }

        if (draft.Questions.Count < QuestionsPerTicket)
        {
            _logger.Error(
                $"Билет {number} пропущен: найдено вопросов {draft.Questions.Count}, " +
                $"требуется не менее {QuestionsPerTicket}.");
            return;
        }

        if (tickets.Any(ticket => ticket.Number == number))
        {
            _logger.Error($"Билет {number} пропущен: билет с таким номером уже есть.");
            return;
        }

        tickets.Add(new Ticket(number, draft.Questions.Take(QuestionsPerTicket).ToList()));
    }

    [GeneratedRegex(@"^Билет\s+(?<number>\d+)$", RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex HeaderPattern();

    [GeneratedRegex(@"^\d+\.\s*(?<text>\S.*)$")]
    private static partial Regex QuestionPattern();

    private sealed class TicketDraft
    {
        private TicketDraft(int? number, string firstLine)
        {
            Number = number;
            FirstLine = firstLine;
        }

        public int? Number { get; }

        public string FirstLine { get; }

        public List<string> Questions { get; } = [];

        public static TicketDraft WithHeader(int number, string line) => new(number, line);

        public static TicketDraft WithoutHeader(string line) => new(null, line);
    }
}
