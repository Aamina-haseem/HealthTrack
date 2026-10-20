namespace HealthTrack.ViewModels
{
    // One labelled value for a dashboard chart, e.g. ("Squats", 4) or ("12 Oct", 78.4).
    public class ChartPoint
    {
        public string Label { get; init; } = string.Empty;
        public double Value { get; init; }
    }
}
