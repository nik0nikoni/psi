using ExamTickets.Core.Abstractions;
using ExamTickets.Core.Parsing;
using NSubstitute;

namespace ExamTickets.UnitTests.Parsing;

public sealed class TicketParserTests
{
    private readonly IAppLogger _logger = Substitute.For<IAppLogger>();
    private readonly TicketParser _parser;

    public TicketParserTests()
    {
        _parser = new TicketParser(_logger);
    }

    [Fact]
    public void Parse_ValidTicket_ReturnsNumberAndThreeQuestions()
    {
        var tickets = _parser.Parse(
        [
            "Билет 1",
            "1. Вопрос первый",
            "2. Вопрос второй",
            "3. Вопрос третий",
        ]);

        var ticket = Assert.Single(tickets);
        Assert.Equal(1, ticket.Number);
        Assert.Equal(["Вопрос первый", "Вопрос второй", "Вопрос третий"], ticket.Questions);
        _logger.DidNotReceiveWithAnyArgs().Error(default!);
    }

    [Fact]
    public void Parse_SeveralTicketsWithEmptyParagraphs_ReturnsAllInOrder()
    {
        var tickets = _parser.Parse(
        [
            "Билет 1",
            "1. A1",
            "2. A2",
            "3. A3",
            "",
            "   ",
            "Билет 2",
            "1. B1",
            "2. B2",
            "3. B3",
        ]);

        Assert.Equal([1, 2], tickets.Select(ticket => ticket.Number));
        Assert.Equal(["B1", "B2", "B3"], tickets[1].Questions);
    }

    [Fact]
    public void Parse_ExtraWhitespaceAndLowerCaseHeader_IsRecognised()
    {
        var tickets = _parser.Parse(["  билет   7 ", " 1.   Q1 ", "2.Q2", "3. Q3"]);

        var ticket = Assert.Single(tickets);
        Assert.Equal(7, ticket.Number);
        Assert.Equal(["Q1", "Q2", "Q3"], ticket.Questions);
    }

    [Fact]
    public void Parse_MoreThanThreeQuestions_KeepsFirstThree()
    {
        var tickets = _parser.Parse(["Билет 1", "1. Q1", "2. Q2", "3. Q3", "4. Q4"]);

        Assert.Equal(["Q1", "Q2", "Q3"], Assert.Single(tickets).Questions);
    }

    [Fact]
    public void Parse_TicketWithTwoQuestions_IsSkippedAndLogged()
    {
        var tickets = _parser.Parse(
        [
            "Билет 1",
            "1. Q1",
            "2. Q2",
            "Билет 2",
            "1. B1",
            "2. B2",
            "3. B3",
        ]);

        Assert.Equal(2, Assert.Single(tickets).Number);
        _logger.Received(1).Error(Arg.Is<string>(message => message.Contains("Билет 1")));
    }

    [Fact]
    public void Parse_LastTicketWithoutQuestions_IsSkippedAndLogged()
    {
        var tickets = _parser.Parse(["Билет 1", "1. Q1", "2. Q2", "3. Q3", "Билет 2"]);

        Assert.Equal(1, Assert.Single(tickets).Number);
        _logger.Received(1).Error(Arg.Is<string>(message => message.Contains("Билет 2")));
    }

    [Theory]
    [InlineData("Билет")]
    [InlineData("Билет два")]
    [InlineData("Вопросы к экзамену")]
    public void Parse_UnrecognisedHeader_SkipsItsQuestionsAndLogs(string header)
    {
        var tickets = _parser.Parse(
        [
            header,
            "1. X1",
            "2. X2",
            "3. X3",
            "Билет 2",
            "1. B1",
            "2. B2",
            "3. B3",
        ]);

        var ticket = Assert.Single(tickets);
        Assert.Equal(2, ticket.Number);
        Assert.Equal(["B1", "B2", "B3"], ticket.Questions);
        _logger.Received(1).Error(Arg.Is<string>(message => message.Contains(header)));
    }

    [Fact]
    public void Parse_QuestionsBeforeFirstHeader_AreSkippedAndLogged()
    {
        var tickets = _parser.Parse(["1. X1", "2. X2", "3. X3", "Билет 1", "1. Q1", "2. Q2", "3. Q3"]);

        Assert.Equal(1, Assert.Single(tickets).Number);
        _logger.Received(1).Error(Arg.Any<string>());
    }

    [Fact]
    public void Parse_DuplicateNumber_KeepsFirstTicketAndLogs()
    {
        var tickets = _parser.Parse(
        [
            "Билет 1", "1. A1", "2. A2", "3. A3",
            "Билет 1", "1. B1", "2. B2", "3. B3",
        ]);

        Assert.Equal(["A1", "A2", "A3"], Assert.Single(tickets).Questions);
        _logger.Received(1).Error(Arg.Is<string>(message => message.Contains("Билет 1")));
    }

    [Fact]
    public void Parse_NoParagraphs_ReturnsEmptyList()
    {
        var tickets = _parser.Parse([]);

        Assert.Empty(tickets);
        _logger.DidNotReceiveWithAnyArgs().Error(default!);
    }
}
