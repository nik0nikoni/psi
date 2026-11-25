namespace ExamTickets.Presentation.Abstractions;

public interface IUserNotifier
{
    void ShowError(string message);

    bool AskRetry(string message);
}
