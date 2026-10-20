// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// ===================================================================
// HealthTrack chart helpers (Chart.js is loaded in _Layout before this file)
// Every dashboard chart uses these so they all look the same:
// thin lines, rounded bar ends, quiet grid lines, hover tooltips, and a
// colour palette that was checked for colour-blind safety on the dark theme.
// ===================================================================
if (window.Chart) {
    Chart.defaults.color = '#A1A1AA';                         // axis and legend text
    Chart.defaults.borderColor = 'rgba(255, 255, 255, 0.06)'; // grid lines
    Chart.defaults.font.family = 'Inter, system-ui, sans-serif';
}

window.HT = (function () {
    // Series colours in a fixed order (validated: lightness, contrast and colour-blind separation)
    const colors = ['#EE6122', '#3987e5', '#199e70', '#9085e9'];
    const surface = '#18181B';

    // Settings shared by every chart: hover shows all values for that point, tooltip styled for the dark theme
    function baseOptions(showLegend) {
        return {
            responsive: true,
            maintainAspectRatio: false,
            interaction: { mode: 'index', intersect: false },
            plugins: {
                legend: {
                    display: showLegend,
                    position: 'bottom',
                    labels: { usePointStyle: true, pointStyle: 'circle', boxWidth: 8, padding: 16 }
                },
                tooltip: {
                    backgroundColor: '#0E0E10',
                    borderColor: '#2C2C31',
                    borderWidth: 1,
                    titleColor: '#FFFFFF',
                    bodyColor: '#D4D4D8',
                    padding: 10,
                    cornerRadius: 8
                }
            }
        };
    }

    // Soft orange fill under a line, fading to transparent at the bottom
    function fillGradient(ctx, area, hex) {
        if (!area) return hex + '33';
        const g = ctx.createLinearGradient(0, area.top, 0, area.bottom);
        g.addColorStop(0, hex + '55');
        g.addColorStop(1, hex + '00');
        return g;
    }

    function find(id) { return document.getElementById(id); }

    return {
        colors: colors,

        // Area line chart for one measure over time (e.g. weight). unit is shown on the y axis.
        // fromZero: start the y axis at 0 (for counts and minutes, which can never be negative).
        line: function (id, labels, values, label, unit, fromZero) {
            const canvas = find(id);
            if (!canvas || !window.Chart) return;
            const options = baseOptions(false);
            options.scales = {
                x: { grid: { display: false } },
                y: fromZero
                    ? { beginAtZero: true, title: { display: !!unit, text: unit }, ticks: { precision: 0 } }
                    : { title: { display: !!unit, text: unit }, grace: '5%' }
            };
            options.plugins.tooltip.callbacks = {
                label: function (c) { return ' ' + label + ': ' + c.formattedValue + (unit ? ' ' + unit : ''); }
            };
            new Chart(canvas, {
                type: 'line',
                data: {
                    labels: labels,
                    datasets: [{
                        label: label,
                        data: values,
                        borderColor: colors[0],
                        borderWidth: 2,
                        tension: 0.35,
                        cubicInterpolationMode: 'monotone',   // smooth, but never dips below the real values
                        fill: true,
                        backgroundColor: function (c) { return fillGradient(c.chart.ctx, c.chart.chartArea, colors[0]); },
                        pointRadius: values.length > 12 ? 0 : 4,
                        pointHoverRadius: 6,
                        pointBackgroundColor: colors[0],
                        pointBorderColor: surface,
                        pointBorderWidth: 2
                    }]
                },
                options: options
            });
        },

        // Vertical bars. series = [{ label, values }], one colour per series in palette order.
        bars: function (id, labels, series, unit) {
            const canvas = find(id);
            if (!canvas || !window.Chart) return;
            const options = baseOptions(series.length > 1);
            options.scales = {
                x: { grid: { display: false } },
                y: { beginAtZero: true, title: { display: !!unit, text: unit }, ticks: { precision: 0 } }
            };
            new Chart(canvas, {
                type: 'bar',
                data: {
                    labels: labels,
                    datasets: series.map(function (s, i) {
                        return {
                            label: s.label,
                            data: s.values,
                            backgroundColor: colors[i],
                            borderRadius: { topLeft: 4, topRight: 4 },  // rounded end, flat on the baseline
                            borderSkipped: 'start',
                            maxBarThickness: 22,
                            categoryPercentage: 0.7,
                            barPercentage: 0.9
                        };
                    })
                },
                options: options
            });
        },

        // Horizontal bars for ranked lists (e.g. most logged activities). One colour: the axis names each bar.
        ranked: function (id, labels, values, label) {
            const canvas = find(id);
            if (!canvas || !window.Chart) return;
            const options = baseOptions(false);
            options.indexAxis = 'y';
            options.interaction = { mode: 'nearest', axis: 'y', intersect: false };
            options.scales = {
                x: { beginAtZero: true, ticks: { precision: 0 } },
                y: { grid: { display: false } }
            };
            new Chart(canvas, {
                type: 'bar',
                data: {
                    labels: labels,
                    datasets: [{
                        label: label,
                        data: values,
                        backgroundColor: colors[0],
                        borderRadius: { topRight: 4, bottomRight: 4 },
                        borderSkipped: 'start',
                        maxBarThickness: 18
                    }]
                },
                options: options
            });
        },

        // Donut for parts of a whole (max 3 parts, validated as a set). Legend shows each part.
        donut: function (id, labels, values) {
            const canvas = find(id);
            if (!canvas || !window.Chart) return;
            const options = baseOptions(true);
            options.cutout = '68%';
            options.interaction = { mode: 'nearest', intersect: true };
            new Chart(canvas, {
                type: 'doughnut',
                data: {
                    labels: labels,
                    datasets: [{
                        data: values,
                        backgroundColor: colors.slice(0, values.length),
                        borderColor: surface,     // 2px gap between slices
                        borderWidth: 2,
                        hoverOffset: 6
                    }]
                },
                options: options
            });
        },

        // Tiny trend line inside a stat card (no axes; the card shows the number itself)
        spark: function (id, values) {
            const canvas = find(id);
            if (!canvas || !window.Chart || values.length < 2) return;
            new Chart(canvas, {
                type: 'line',
                data: {
                    labels: values.map(function (_, i) { return i; }),
                    datasets: [{
                        data: values,
                        borderColor: colors[0],
                        borderWidth: 2,
                        tension: 0.4,
                        cubicInterpolationMode: 'monotone',
                        pointRadius: 0,
                        fill: true,
                        backgroundColor: function (c) { return fillGradient(c.chart.ctx, c.chart.chartArea, colors[0]); }
                    }]
                },
                options: {
                    responsive: true,
                    maintainAspectRatio: false,
                    events: [],                       // decoration only: the number is in the card
                    plugins: { legend: { display: false }, tooltip: { enabled: false } },
                    scales: { x: { display: false }, y: { display: false } }
                }
            });
        }
    };
})();
