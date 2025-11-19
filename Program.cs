
namespace ZadDod1;

class BikeRent
{
    public enum GenderType
    {
        Unknown = 0, Male = 1, Female = 2
    };
    
    public int TripDuration;
    public DateTime StartTime;
    public DateTime StopTime;
    public int StartStationId;
    public string StartStationName;
    public double StartStationLatitude;
    public double StartStationLongitude;
    public int EndStationId;
    public string EndStationName;
    public double EndStationLatitude;
    public double EndStationLongitude;
    public int BikeId;
    public string UserType;
    public int BirthYear;

    public GenderType Gender;

    public BikeRent(List<string> record)
    {
        if (record.Count < 15) throw new ArgumentException("Invalid record");
        
        static int ParseYear(string s)
        {
            if (string.IsNullOrWhiteSpace(s) || s == "\\N") return 0;
            double val = double.Parse(s, System.Globalization.CultureInfo.InvariantCulture); // kultura niezmienna, wymaga kropki w dziesietnych, YYYY-MM-DD w datach
            return (int)val;
        }

        try
        {
            TripDuration = int.Parse(record[0]);
            StartTime = DateTime.Parse(record[1]);
            StopTime = DateTime.Parse(record[2]);
            StartStationId = int.Parse(record[3]);
            StartStationName = record[4];
            StartStationLatitude = double.Parse(record[5], System.Globalization.CultureInfo.InvariantCulture);
            StartStationLongitude = double.Parse(record[6], System.Globalization.CultureInfo.InvariantCulture);
            EndStationId = int.Parse(record[7]);
            EndStationName = record[8];
            EndStationLatitude = double.Parse(record[9], System.Globalization.CultureInfo.InvariantCulture);
            EndStationLongitude = double.Parse(record[10], System.Globalization.CultureInfo.InvariantCulture);
            BikeId = int.Parse(record[11]);
            UserType = record[12];
            BirthYear = ParseYear(record[13]);
            Gender = (GenderType)int.Parse(record[14]); // => GenderType.Unknown / .Male / .Female
        }
        catch
        {
            Console.WriteLine($"Invalid record {string.Join(',', record)}");
        }
    }
}

static class ZadDod1
{
    private static List<BikeRent> _allData = new List<BikeRent>();
    
    static ZadDod1()
    {
        LoadAllCitibikeData();
    }

    private static void LoadAllCitibikeData()
    {
        string folderPath = "2014-citibike-tripdata"; // chg .csproj to add full dir

        List<List<string>> allCsvLines = Directory.GetFiles(folderPath, "*.csv", SearchOption.AllDirectories)
            .SelectMany(file => File.ReadAllLines(file).Skip(1))
            .Select(line => line.Split(',').ToList())
            .ToList();

        foreach (var record in allCsvLines) _allData.Add(new BikeRent(record));
    }
    
    public static List<BikeRent> GetAllData() => _allData;
}

static class DistanceCalculator
{
    public static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        static double ToRadians(double angle)
        {
            return Math.PI * angle / 180.0;
        }
        
        const double earthRadiusKm = 6371;
        
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        
        lat1 = ToRadians(lat1);
        lat2 = ToRadians(lat2);
        
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2) * Math.Cos(lat1) * Math.Cos(lat2);
        
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return earthRadiusKm * c;
    }
}

class Program
{
    static void Main()
    {
        var data = ZadDod1.GetAllData();
        Console.WriteLine($"Total records loaded: {data.Count}");
        
        // 1. Most popular routes
        var zapytanie1 = data.GroupBy(g => new {StartId = g.StartStationId, StartName = g.StartStationName, EndId = g.EndStationId, EndName = g.EndStationName})
            .Select(g => new {RouteStart = g.Key.StartName, RouteEnd = g.Key.EndName, TripCount = g.Count()})
            .OrderByDescending(g => g.TripCount)
            .First();
        // 2. Which stations are the most popular for start and stop?
            // start
        var zapytanie2a = data.GroupBy(g => new { StartName = g.StartStationName })
            .OrderByDescending(g => g.Count())
            .First();
            //stop
        var zapytanie2b = data.GroupBy(g => new { StopName = g.EndStationName })
            .OrderBy(g => g.Count())
            .First();
        // 3. When is the most popular time of the day to rent a bike?
        var zapytanie3 = data.GroupBy(rent => rent.StartTime.Hour / 2) // GroupBy tworzy grupy do ktorych mozna sie odwolac jako .Key
            .Select(g => new { StartHour = g.Key * 2, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .First();
        // 4. What is the average trip duration?
        var zapytanie4 = data.Average(rent => rent.TripDuration);
        // 5. What is percantage of Subscriber users among users younger than 25?
        var genz = data.ToList();
        int totalGenz = genz.Count();
        int subscribers = genz.Count(g => g.UserType == "Subscriber");
        var zapytanie5 = subscribers;
        // 6. 5 most used bikes with the most trips
        var zapytanie6 = data.GroupBy(g => g.BikeId)
            .OrderByDescending(g => g.Count()).Take(5).ToList();; // .Count jest dla kazdej grupki - liczy rekordy
        // 7. 5 most used bikes with the highest mileage
        var zapytanie7 = data.Select(g => new { 
            BikeId = g.BikeId, 
            TripLength = DistanceCalculator.CalculateDistance(g.StartStationLatitude, g.StartStationLongitude, g.EndStationLatitude, g.EndStationLongitude)
        })
        .GroupBy(g => g.BikeId)
        .Select(g => new
        {
            BikeId = g.Key,
            TotalMileage = g.Sum(x => x.TripLength)
        })
        .OrderByDescending(g => g.TotalMileage)
        .Take(5)
        .ToList();
        // Output
        Console.WriteLine("------------ ANSWERS -------------");
        Console.WriteLine($"1. Most popular route: \n\t{zapytanie1.RouteStart} => {zapytanie1.RouteEnd}");
        Console.WriteLine($"2a. Most popular start station: \n\t{zapytanie2a.Key.StartName}");
        Console.WriteLine($"2b. Most popular stop station: \n\t{zapytanie2b.Key.StopName}");
        Console.WriteLine($"3. The most popular time of the day to rent a bike: \n\t{zapytanie3.StartHour} - {zapytanie3.StartHour + 2}");
        Console.WriteLine($"4. Average trip duration: \n\t{(zapytanie4/60):F0} minutes {(zapytanie4%60):F0} seconds");
        Console.WriteLine($"5. Number of subscribers among riders: \n\t{zapytanie5} / {totalGenz}");
        Console.WriteLine($"6. 5 most frequently used bikes:");
        string x = "";
        foreach (var c  in zapytanie6)
            Console.WriteLine($"\tBike {c.Key}: {c.Count()} trips");
        
        Console.WriteLine($"7. 5 bikes with highest mileage:");
        x = "";
        foreach (var c  in zapytanie7)
            Console.WriteLine($"\tBike {c.BikeId}: {c.TotalMileage:F2} miles");
    }
}