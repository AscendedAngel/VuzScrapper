using VuzScrapper.Scrappers;
using System.Runtime.InteropServices;
using VuzScrapper.Scrappers.Spbgu;
using VuzScrapper.Scrappers.Itmo;

namespace VuzScrapper;

internal sealed class Program 
{
    private static async Task Main()
    {
        IRequestParser itmoScrapper = new ItmoScrapper();
        IRequestParser spbguScrapper = new SpbguScrapper();

        var competition = await spbguScrapper.CreateCompetition();

        if (competition is null)
        {
            foreach (var error in spbguScrapper.Errors) 
            {
                await Console.Error.WriteLineAsync($"{error.RequestMessage} - {error.StatusCode}");
            }
            return;
        }


        Console.Write("\nВведите ваш ID абитуриента: ");
        var myId = Console.ReadLine()!;
        CompetitionResolver.Resolve(competition, new Applicant { Code = myId });

        foreach (var list in competition.CompetitionLists)
        {
            var currentPlace = list.Students.FindIndex(x => x.Code == myId);
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
