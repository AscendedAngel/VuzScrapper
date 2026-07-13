using VuzScrapper.Scrappers;
using System.Runtime.InteropServices;
using VuzScrapper.Scrappers.Spbgu;
using VuzScrapper.Scrappers.Itmo;

namespace VuzScrapper;

internal sealed class Program 
{
    private static HttpClientWrapper _wrapper = null!;

    private static IEnumerable<IRequestParser> GetScrappers()
    {
        yield return new ItmoScrapper(_wrapper);
        yield return new SpbguScrapper(_wrapper);
    }

    private static void OutputResult(Competition competition, Applicant applicant)
    {
        foreach (var list in competition.CompetitionLists)
        {
            var currentPlace = list.Students.FindIndex(x => x.Code == applicant.Code);
            if (currentPlace == -1)
            {
                continue;
            }
            
            var type = list.Students[currentPlace].ApplicantType switch 
            {
                ApplicantType.Olympiad => "Олимпиада",
                ApplicantType.Target => "Целевая квота",
                ApplicantType.Special => "Особая квота",
                ApplicantType.Separate => "Отдельная квота",
                ApplicantType.Common => "Общий конкурс",
                _ => "*неизвестная ошибка*"
            };
            
            var name = list.Name;
            var places = list.Places;
            var priority = list.Students[currentPlace].ProgramPriority;
            var examPlaces = places - (list.TargetedQuota + list.SpecialQuota + list.SeparateQuota + list.Students.Count(x => x.ApplicantType == ApplicantType.Olympiad));

            Console.WriteLine($"{name} |\nВсего {list.Places} мест, из которых {examPlaces} - общий конкурс");
            Console.WriteLine($"Вы находитесь на {currentPlace + 1} месте ({type}; Приоритет - №{priority})\n");
        }
    }

    private static IRequestParser AskForScrapper()
    {
        var scrappers = GetScrappers().ToArray();

        Console.WriteLine("\nВыберите ВУЗ:");

        foreach (var (index, value) in scrappers.Select((value, index) => (index, value)))
        {
            Console.WriteLine($"{index+1}. {value.Name}");
        }

        Console.WriteLine();

        var chosen = -1;

        do
        {
            Console.Write("Выберите номер ВУЗа: ");
            if (!int.TryParse(Console.ReadLine(), out chosen) || !(chosen > 0 && chosen <= scrappers.Length)) continue;
            break;
        } while (true);

        return scrappers[chosen - 1];
    }

    private static async Task Main()
    { 
        _wrapper = new HttpClientWrapper();
        var scrapper = AskForScrapper();
        var competitionResult = await scrapper.CreateCompetition();

        if (competitionResult.IsFailure)
        {
            foreach (var error in competitionResult.Error) 
            {
                await Console.Error.WriteLineAsync($"{error.RequestMessage} - {error.StatusCode}");
            }
            return;
        }

        var competition = competitionResult.Value;

        Console.Write("\nВведите ваш ID абитуриента: ");
        var myId = Console.ReadLine()!;
        var applicant = new Applicant { Code = myId };
        CompetitionResolver.Resolve(competition, applicant);

        OutputResult(competition, applicant);


        #if DEBUG
            Console.WriteLine("\nНажмите любую клавишу чтобы выйти...");
            Console.ReadKey();
        #else
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Console.Write("\nНажмите любую клавишу для выхода...");
                Console.ReadKey();
            }
        #endif
    }
}
