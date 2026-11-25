# Lab 2 — Exam Ticket Generator with a Desktop UI: Specification

English version of the assignment (`Лаб2_UI_генератор_билетов_ТЗ.pdf`), written as a detailed prompt for a code assistant. It restates every requirement of the original brief and fixes the technical decisions taken for this repository.

## 1. Goal

Build a Windows desktop application that lets an examiner pick a student group and a student, assigns the student an exam ticket from a Word document, shows the ticket in a separate window, and appends every generation to an Excel journal.

## 2. Technology stack

| Concern | Choice |
|---|---|
| Language / runtime | C#, .NET 10 (`net10.0`; WPF projects `net10.0-windows`) |
| GUI | WPF, MVVM pattern |
| Excel (read and write) | `ClosedXML` |
| Word (read) | `DocumentFormat.OpenXml` |
| Tests | xUnit, NSubstitute; FlaUI for end-to-end tests |
| Dependency wiring | Manual composition root in `App.xaml.cs`, no DI container, no `StartupUri` |

## 3. Input data

### 3.1 `students.xlsx`

- One worksheet per group; the worksheet name is the group name.
- Row 1 is a header; data starts at row 2.
- Column A — last name, column B — first name; one student per row.
- A worksheet without data rows is a valid, empty group.

### 3.2 `tickets.docx`

Plain paragraphs in the following format:

```text
Билет 1
1. First question
2. Second question
3. Third question

Билет 2
...
```

- A ticket header is a paragraph of the form `Билет N`, where `N` is a positive integer.
- A question is a numbered paragraph of the form `N. text`.
- Empty paragraphs are ignored.
- A ticket is valid when its header is recognised and it has at least three questions; the first three questions are shown.
- An invalid ticket (fewer than three questions, or questions with no recognised header before them) is **skipped**: the error is written to the application log and parsing continues with the remaining tickets.
- `N` (the ticket count used for random selection) is the number of valid tickets.

### 3.3 File locations

- `students.xlsx`, `tickets.docx` and `results.xlsx` are looked up in the current working directory.
- The command-line argument `--data <folder>` overrides that directory (used by end-to-end tests).

## 4. User interface

### 4.1 Main window

- **Group** combo box, **Student** combo box, **Generate ticket** button (caption «Сгенерировать билет»).
- On start-up the Group combo box is filled with the worksheet names of `students.xlsx`.
- After a group is selected, the Student combo box is filled with that group's students, each displayed as `LastName FirstName`. Changing the group clears the selected student.
- The button is disabled until both a group and a student are selected.
- If the selected group has no students, the Student list is empty and the button stays disabled; no exception is raised.

### 4.2 Result window

- Clicking the button assigns a ticket (section 5), saves it to the journal and opens a result window showing `Ваш билет № N` and the ticket's three questions.
- Pressing **Esc** closes only the result window and returns to the main window; the application keeps running and is ready for the next student.

## 5. Ticket assignment

A student is identified by the triple *(group, last name, first name)*.

1. Search `results.xlsx` for the **first** (earliest) row of this student.
2. If found — reuse that row's ticket number; do not generate a new one. Append a new row with the same number and `да` in the *Repeat* column.
3. If not found — choose a ticket uniformly at random in the range `1..N`. Append a new row with `нет` in the *Repeat* column.
4. The row is saved **immediately** after the assignment, not when the application closes.

Repeated selection of the same student therefore always returns the same ticket number (idempotency).

## 6. Output: `results.xlsx`

- Created on first use if it does not exist, with a header row.
- Rows are only appended to the end; existing rows are never modified or rewritten.

| A | B | C | D | E | F |
|---|---|---|---|---|---|
| Группа | Фамилия | Имя | Номер билета | Дата и время | Повтор (да/нет) |

The journal must allow restoring the full chronological history of all generations, including repeats.

## 7. Error handling (mandatory)

| Situation | Required behaviour |
|---|---|
| `students.xlsx` or `tickets.docx` missing at start-up | Clear message to the user; the application does not crash |
| Selected group is empty | Empty student list, disabled button, no exception |
| `results.xlsx` is locked (e.g. open in Excel) | Message to the user with a retry option; the application does not crash |
| Invalid ticket in `tickets.docx` | Ticket skipped, error logged, the remaining tickets are used |

Infrastructure errors are translated into domain exceptions (`SourceFileMissingException`, `JournalUnavailableException`) with user-readable messages.

## 8. Architecture

```text
Wpf → Presentation → Core ← Infrastructure
```

- **Core** (`net10.0`): models `Student`, `Ticket`, `ResultRecord`; abstractions `IStudentSource`, `ITicketSource`, `IResultJournal`, `IRandomNumberSource`, `IClock`, `IAppLogger`; `TicketParser`; `TicketAssignmentService`; domain exceptions. No knowledge of WPF, Excel or Word.
- **Infrastructure**: `ExcelStudentSource`, `DocxParagraphReader` / `DocxTicketSource`, `ExcelResultJournal`, system clock, random source, file logger.
- **Presentation** (`net10.0`): `MainViewModel`, `ResultViewModel`, abstractions `IResultPresenter` and `IUserNotifier`. No reference to WPF.
- **Wpf** (`net10.0-windows`): XAML windows, implementations of the presenter and notifier, composition root.

User interface I/O, file access, random numbers and the current time are hidden behind interfaces; the design follows SOLID.

## 9. Tests

- **Unit tests:** ticket parsing (valid and invalid tickets), reading students by group (including an empty group and a missing file), journal creation/append/lookup/lock, assignment service idempotency.
- **UI tests:** view models checked against the acceptance criteria with NSubstitute, without opening windows.
- **End-to-end tests:** FlaUI drives the real application in a temporary data folder and verifies the contents of `results.xlsx`.

## 10. Acceptance criteria

1. Selecting a group fills the student list with exactly the students of that worksheet.
2. The button is enabled only when both a group and a student are selected.
3. The ticket number and its three questions are displayed correctly for a random ticket from the file.
4. Selecting the same student again returns the same ticket number.
5. Esc closes the result window, not the application.
6. A row appears in `results.xlsx` immediately after generation; earlier rows are untouched.
7. A missing or locked file does not crash the application.
8. Selecting the same student again appends a new row at the end of the journal (existing rows are not changed); its ticket number equals the student's first record and *Repeat* is `да`.
9. The journal allows restoring the full history of generations, including repeats, in chronological order.

## 11. Version control

- Work is done on branch `lab2-ui-tickets` and merged into `main` at the end.
- Conventional Commits, one commit per step, pushed after each commit.
- `bin/`, `obj/`, `results.xlsx` and log files are excluded via `.gitignore`.
