namespace Database_Backend.Services;

public static class IncidentPriorityService
{
    public static IncidentPriorityResult Calculate(string severityLevel, int affectedPopulation)
    {
        var severityRank = GetSeverityRank(severityLevel);

        var safeAffectedPopulation = Math.Max(0, affectedPopulation);
        var score = severityRank * (1m + (safeAffectedPopulation / 100m));

        var priorityLevel = score switch
        {
            >= 7m => 1,
            >= 4m => 2,
            >= 2m => 3,
            _ => 4
        };

        return new IncidentPriorityResult
        {
            PriorityLevel = priorityLevel,
            PriorityLabel = PriorityLabel(priorityLevel),
            PriorityScore = decimal.Round(score, 2, MidpointRounding.AwayFromZero),
            EstimatedResponseMinutes = EstimatedResponseMinutes(priorityLevel)
        };
    }

    private static int GetSeverityRank(string severityLevel)
    {
        return severityLevel.ToLowerInvariant() switch
        {
            "critical" => 4,
            "high" => 3,
            "medium" => 2,
            "low" => 1,
            _ => 0
        };
    }

    private static string PriorityLabel(int priorityLevel)
    {
        return priorityLevel switch
        {
            1 => "Critical",
            2 => "High",
            3 => "Medium",
            _ => "Low"
        };
    }

    private static int EstimatedResponseMinutes(int priorityLevel)
    {
        return priorityLevel switch
        {
            1 => 15,
            2 => 30,
            3 => 60,
            _ => 120
        };
    }
}

public class IncidentPriorityResult
{
    public int PriorityLevel { get; set; }

    public string PriorityLabel { get; set; } = string.Empty;

    public decimal PriorityScore { get; set; }

    public int EstimatedResponseMinutes { get; set; }
}
