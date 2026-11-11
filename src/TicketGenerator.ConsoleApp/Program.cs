using System.Text;
using TicketGenerator.ConsoleApp;
using TicketGenerator.ConsoleApp.Input;
using TicketGenerator.ConsoleApp.Output;
using TicketGenerator.Core.Generation;
using TicketGenerator.Core.Services;
using TicketGenerator.Core.Validation;
using TicketGenerator.Infrastructure.Excel;
using TicketGenerator.Infrastructure.Randomness;
using TicketGenerator.Infrastructure.Time;

Console.InputEncoding = Encoding.UTF8;
Console.OutputEncoding = Encoding.UTF8;

var journalPath = Path.Combine(Environment.CurrentDirectory, "journal.xlsx");

var console = new SystemConsole();
var validator = new StudentNameValidator();
var service = new TicketIssuingService(
    validator,
    new TicketNumberGenerator(new SystemRandomNumberSource()),
    new SystemClock(),
    new ExcelTicketRepository(journalPath));

var application = new TicketConsoleApplication(
    console,
    new ConsoleLineReader(console),
    validator,
    service);

application.Run();
