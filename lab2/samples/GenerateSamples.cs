#:package ClosedXML@0.105.1
#:property PublishAot=false

// Regenerates the demo files: dotnet run samples/GenerateSamples.cs -- samples
using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

var outputDirectory = args.Length > 0 ? args[0] : ".";
Directory.CreateDirectory(outputDirectory);

WriteStudents(Path.Combine(outputDirectory, "students.xlsx"));
WriteTickets(Path.Combine(outputDirectory, "tickets.docx"));
Console.WriteLine($"Samples written to {Path.GetFullPath(outputDirectory)}");

static void WriteStudents(string path)
{
    (string Group, (string LastName, string FirstName)[] Students)[] groups =
    [
        ("TI-221", [("Иванов", "Иван"), ("Петрова", "Мария"), ("Сидоров", "Алексей"), ("Кузнецова", "Анна"), ("Попов", "Дмитрий")]),
        ("TI-222", [("Смирнов", "Сергей"), ("Волкова", "Елена"), ("Морозов", "Андрей"), ("Новикова", "Ольга")]),
        ("FAF-231", [("Лебедев", "Никита"), ("Козлова", "Дарья"), ("Соколов", "Максим")]),
        ("Пустая группа", []),
    ];

    using var workbook = new XLWorkbook();

    foreach (var (group, students) in groups)
    {
        var worksheet = workbook.AddWorksheet(group);
        worksheet.Cell(1, 1).Value = "Фамилия";
        worksheet.Cell(1, 2).Value = "Имя";
        worksheet.Row(1).Style.Font.Bold = true;

        for (var i = 0; i < students.Length; i++)
        {
            worksheet.Cell(i + 2, 1).Value = students[i].LastName;
            worksheet.Cell(i + 2, 2).Value = students[i].FirstName;
        }

        worksheet.Columns(1, 2).AdjustToContents();
    }

    workbook.SaveAs(path);
}

static void WriteTickets(string path)
{
    // Tickets 1-5 use typed numbers, 8-10 use Word automatic numbering;
    // "Билет 6" (two questions) and "Билет VII" (unrecognised header) are invalid on purpose.
    (string Header, string[] Questions, bool AutoNumbered)[] tickets =
    [
        ("Билет 1", ["Принципы SOLID и их назначение.", "Модульное тестирование: цели и инструменты.", "Система контроля версий Git: ветки и слияние."], false),
        ("Билет 2", ["Паттерн MVVM в десктопных приложениях.", "Внедрение зависимостей без контейнера.", "Обработка исключений и доменные ошибки."], false),
        ("Билет 3", ["Чтение и запись Excel-файлов из .NET.", "Структура документа Word в формате OpenXML.", "Подставные объекты (mocks) в тестах."], false),
        ("Билет 4", ["Принцип единственной ответственности.", "Интеграционные и E2E-тесты: отличия.", "Conventional Commits и история репозитория."], false),
        ("Билет 5", ["Привязка данных в WPF.", "Команды ICommand и их доступность.", "Блокировки файлов в Windows."], false),
        ("Билет 6", ["Принцип открытости/закрытости.", "Регулярные выражения в .NET."], false),
        ("Билет VII", ["Этот билет не распознаётся.", "Его заголовок записан римскими цифрами.", "Парсер пропустит его и запишет ошибку в лог."], false),
        ("Билет 8", ["Принцип подстановки Лисков.", "Идемпотентность операций.", "Журналирование событий приложения."], true),
        ("Билет 9", ["Принцип разделения интерфейсов.", "Автоматизация UI с помощью FlaUI.", "Хронологический журнал результатов."], true),
        ("Билет 10", ["Принцип инверсии зависимостей.", "Генерация случайных чисел и тестируемость.", "Работа со временем через абстракцию IClock."], true),
    ];

    using var document = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
    var mainPart = document.AddMainDocumentPart();
    var numbering = new Numbering(DecimalListDefinition());
    var body = new Body();
    var numberingId = 0;

    foreach (var (header, questions, autoNumbered) in tickets)
    {
        body.Append(new Paragraph(
            new ParagraphProperties(new SpacingBetweenLines { Before = "240" }),
            new Run(new RunProperties(new Bold()), new Text(header))));

        if (autoNumbered)
        {
            numberingId++;
            numbering.Append(RestartedList(numberingId));
        }

        for (var i = 0; i < questions.Length; i++)
        {
            body.Append(autoNumbered
                ? new Paragraph(
                    new ParagraphProperties(new NumberingProperties(
                        new NumberingLevelReference { Val = 0 },
                        new NumberingId { Val = numberingId })),
                    new Run(new Text(questions[i])))
                : new Paragraph(new Run(new Text($"{i + 1}. {questions[i]}"))));
        }
    }

    mainPart.AddNewPart<NumberingDefinitionsPart>().Numbering = numbering;
    mainPart.Document = new Document(body);
}

static AbstractNum DecimalListDefinition() =>
    new(
        new Level(
            new StartNumberingValue { Val = 1 },
            new NumberingFormat { Val = NumberFormatValues.Decimal },
            new LevelText { Val = "%1." },
            new LevelJustification { Val = LevelJustificationValues.Left },
            new PreviousParagraphProperties(new Indentation { Left = "720", Hanging = "360" }))
        { LevelIndex = 0 })
    { AbstractNumberId = 1 };

// Each ticket gets its own list instance that starts again from 1.
static NumberingInstance RestartedList(int id) =>
    new(
        new AbstractNumId { Val = 1 },
        new LevelOverride(new StartOverrideNumberingValue { Val = 1 }) { LevelIndex = 0 })
    { NumberID = id };
