// =====================================================================
// FILE:    ViewModels/ChartPoint.cs
// PURPOSE: One labelled number for a chart, e.g. ("Squats", 4) or ("12 Oct", 78.4).
//          The dashboards send lists of these to the page, where Chart.js draws them.
// USED BY: ClientDashboardViewModel, TrainerDashboardViewModel and their views.
// =====================================================================

namespace HealthTrack.ViewModels   // all view models live in this namespace
{
    // One labelled value for a dashboard chart, e.g. ("Squats", 4) or ("12 Oct", 78.4).
    public class ChartPoint
    {
        public string Label { get; init; } = string.Empty;   // text on the chart axis, e.g. "Squats"
        public double Value { get; init; }                   // the number to plot
    }
}
