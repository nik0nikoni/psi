# Лабораторная работа 2: WPF-генератор билетов

Дополняет общие правила из корневого `AGENTS.md`.

- Работа ведётся в ветке `lab2-ui-tickets` (требование ТЗ); в `main` она вливается последним шагом плана.
- Решение: `ExamTickets.sln`; тесты запускаются командой `dotnet test` из папки `lab2/`.
- GUI — WPF с MVVM. ViewModel'и живут в `ExamTickets.Presentation` (`net10.0`) и не ссылаются на WPF; окна и сообщения скрыты за интерфейсами.
- Зависимости собираются вручную в `App.xaml.cs`, без `StartupUri` и DI-контейнера.
- Excel — `ClosedXML`, Word — `DocumentFormat.OpenXml`.
- Входные файлы: `students.xlsx`, `tickets.docx`; выходной — `results.xlsx`. По умолчанию ищутся в текущем каталоге, аргумент `--data <папка>` задаёт другой каталог.
- UI-тесты проверяют ViewModel'и по критериям приёмки ТЗ; E2E-тесты управляют реальным окном через FlaUI и проверяют `results.xlsx`.
