using ITMOScrapper.Scrappers;

namespace ITMOScrapper;

internal sealed class Program 
{
    private async static Task Main()
    {
        string[] links = [
            "https://abit.itmo.ru/rating/bachelor/budget/2340",
            "https://abit.itmo.ru/rating/bachelor/budget/2342",
            "https://abit.itmo.ru/rating/bachelor/budget/2339",
            "https://abit.itmo.ru/rating/bachelor/budget/2334"];

        IRequestParser itmoScrapper = new ItmoScrapper();

        var competition = await itmoScrapper.CreateCompetition(links);

        if (competition is null)
        {
            foreach (var error in itmoScrapper.Errors) 
            {
                Console.Error.WriteLine($"{error.RequestMessage} - {error.StatusCode}");
            }
            return;
        }

        CompetitionResolver.Resolve(competition);

        Console.Write("\nВведите ваш ID абитуриента: ");
        var myId = Console.ReadLine();

        foreach (var list in competition.CompetitionLists)
        {
            var name = list.Name;
            var places = list.Places;
            var examPlaces = places - (list.TargetedQuota + list.SpecialQuota + list.SeparateQuota + list.Students.Count(x => x.ApplicantType == ApplicantType.Olympiad));

            var currentPlace = list.Students.FindIndex(x => x.Code == myId);
            var type = currentPlace > -1 ? list.Students[currentPlace].ApplicantType switch 
            {
                ApplicantType.Olympiad => "Олимпиада",
                ApplicantType.Target => "Целевая квота",
                ApplicantType.Special => "Особая квота",
                ApplicantType.Separate => "Отдельная квота",
                ApplicantType.Common => "Общий конкурс",
                _ => "*неизвестная ошибка*"
            } : null;

            Console.WriteLine($"{name} |\nВсего {list.Places} мест, из которых {examPlaces} - общий конкурс");

            if (currentPlace > -1) Console.WriteLine($"Вы находитесь на {currentPlace + 1} месте ({type})");
            else Console.WriteLine("Вас нет в списке");
            Console.WriteLine();
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
