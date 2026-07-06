using System.Net;

namespace ITMOScrapper;

internal sealed class Program 
{
    private static void Main()
    {
        // #if DEBUG
        // Console.WriteLine($"Текущая конфигурация: Debug");
        // #else
        // Console.WriteLine($"Текущая конфигурация: Release");
        // #endif

        string[] links = [
            "https://abit.itmo.ru/rating/bachelor/budget/2340",
            "https://abit.itmo.ru/rating/bachelor/budget/2342",
            "https://abit.itmo.ru/rating/bachelor/budget/2339",
            "https://abit.itmo.ru/rating/bachelor/budget/2334"];

        var results = RequestParser.MakeRequests(links);

        if (results.Any(x => x.StatusCode != HttpStatusCode.OK)) 
        {
            Console.WriteLine("There is an error, not HTTP200OK");
            var errors = results.Where(x => x.StatusCode != HttpStatusCode.OK);
            foreach (var error in errors) 
            {
                Console.Error.WriteLine($"{error.RequestMessage} - {error.StatusCode}");
            }
            return;
        }

        var competition = new Competition();

        foreach (var result in results)
        {
            competition.CompetitionLists.Add(RequestParser.CreateCompetitionList(result, competition.Candidates));
        }

        Console.Write("Введите ваш ID абитуриента: ");
        var myId = Console.ReadLine();

        CompetitionResolver.Resolve(competition);

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
    }

}
