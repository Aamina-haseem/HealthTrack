// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// ===================================================================
// HealthTrack chart helpers (Chart.js is loaded in _Layout before this file)
// Every chart in the app uses these, so they all share one modern look:
// smooth gradient area lines with ring points and a value bubble, gradient
// bars with rounded tops and their values on top, slim "progress bar" style
// ranked lists, and donuts with the total in the middle.
// Colours were checked for colour-blind safety on the dark theme.
// ===================================================================
(function () {
    if (!window.Chart) return;
    Chart.defaults.color = '#A1A1AA';                         // axis and legend text
    Chart.defaults.borderColor = 'rgba(255, 255, 255, 0.06)'; // grid lines
    Chart.defaults.font.family = 'Inter, system-ui, sans-serif';
    Chart.defaults.font.size = 12;
    // People who ask their system for less motion get charts without animation
    if (window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
        Chart.defaults.animation = false;
    }
})();

window.HT = (function () {
    // Series colours in a fixed order (validated: lightness, contrast and colour-blind separation)
    const colors = ['#EE6122', '#3987e5', '#199e70', '#9085e9'];
    // A lighter tint of each colour, used only for the top of gradients (same hue, so the meaning does not change)
    const tints = ['#FF9A5C', '#6BA8F0', '#3FC194', '#B3A9F2'];
    const surface = '#18181B';    // card background
    const ink = '#F5F5F4';        // main text
    const muted = '#A1A1AA';      // secondary text

    function find(id) { return document.getElementById(id); }

    // Turns "#EE6122" into "rgba(238, 97, 34, a)"
    function rgba(hex, a) {
        const n = parseInt(hex.slice(1), 16);
        return 'rgba(' + (n >> 16) + ', ' + ((n >> 8) & 255) + ', ' + (n & 255) + ', ' + a + ')';
    }

    // Settings shared by every chart: hover shows all values for that point, tooltip styled for the dark theme
    function baseOptions(showLegend) {
        return {
            responsive: true,
            maintainAspectRatio: false,
            interaction: { mode: 'index', intersect: false },
            layout: { padding: { top: 24, right: 8 } },             // room for the value labels
            plugins: {
                legend: {
                    display: showLegend,
                    position: 'bottom',
                    labels: { usePointStyle: true, pointStyle: 'circle', boxWidth: 8, boxHeight: 8, padding: 18, color: '#D4D4D8' }
                },
                tooltip: {
                    backgroundColor: '#0E0E10',
                    borderColor: '#2C2C31',
                    borderWidth: 1,
                    titleColor: '#FFFFFF',
                    bodyColor: '#D4D4D8',
                    padding: 12,
                    cornerRadius: 10,
                    usePointStyle: true,
                    boxPadding: 4
                }
            }
        };
    }

    // Quiet axes: no axis lines, dashed horizontal grid, no vertical grid
    function valueAxis(unit, fromZero) {
        return {
            beginAtZero: !!fromZero,
            grace: fromZero ? 0 : '8%',
            border: { display: false, dash: [4, 4] },
            grid: { color: 'rgba(255, 255, 255, 0.07)' },
            ticks: { padding: 8, precision: fromZero ? 0 : undefined },
            title: { display: !!unit, text: unit, color: muted }
        };
    }
    function categoryAxis() {
        return { grid: { display: false }, border: { display: false }, ticks: { padding: 8 } };
    }

    // Vertical gradient: strong at the top, fading towards the bottom (used for areas and bars)
    function verticalGradient(chart, top, bottom) {
        const area = chart.chartArea;
        if (!area) return top;                                    // first render: no size yet
        const g = chart.ctx.createLinearGradient(0, area.top, 0, area.bottom);
        g.addColorStop(0, top);
        g.addColorStop(1, bottom);
        return g;
    }

    // Draws a rounded rectangle (used by the value bubble)
    function roundRect(ctx, x, y, w, h, r) {
        ctx.beginPath();
        ctx.moveTo(x + r, y);
        ctx.arcTo(x + w, y, x + w, y + h, r);
        ctx.arcTo(x + w, y + h, x, y + h, r);
        ctx.arcTo(x, y + h, x, y, r);
        ctx.arcTo(x, y, x + w, y, r);
        ctx.closePath();
    }

    // ---- Small Chart.js plugins (each one draws a little extra on top of a chart) ----

    // Orange pill above the LAST point of a line, e.g. "78.4 kg" (only one label, so the chart stays clean)
    function lastValueBubble(unit) {
        return {
            id: 'htBubble',
            afterDatasetsDraw(chart) {
                const meta = chart.getDatasetMeta(0);
                const point = meta.data[meta.data.length - 1];
                if (!point) return;
                const value = chart.data.datasets[0].data[meta.data.length - 1];
                // "1 log" but "2 logs": drop the plural s when the value is exactly 1
                const word = value === 1 && /[a-z]s$/.test(unit || '') ? unit.slice(0, -1) : unit;
                const text = value + (word ? ' ' + word : '');
                const ctx = chart.ctx;
                ctx.save();
                ctx.font = '600 12px Inter, system-ui, sans-serif';
                const w = ctx.measureText(text).width + 16, h = 22;
                let x = point.x - w / 2;
                x = Math.max(chart.chartArea.left, Math.min(x, chart.chartArea.right - w));   // keep it inside the chart
                const y = point.y - h - 10;
                roundRect(ctx, x, y, w, h, 11);
                ctx.fillStyle = colors[0];
                ctx.fill();
                ctx.fillStyle = '#0E0E10';                       // dark text on orange (7:1 contrast)
                ctx.textAlign = 'center';
                ctx.textBaseline = 'middle';
                ctx.fillText(text, x + w / 2, y + h / 2 + 1);
                ctx.restore();
            }
        };
    }

    // Writes each bar's value at its end (above a vertical bar, right of a horizontal one)
    function barValues(datasetIndex, horizontal) {
        return {
            id: 'htBarValues',
            afterDatasetsDraw(chart) {
                const meta = chart.getDatasetMeta(datasetIndex);
                const data = chart.data.datasets[datasetIndex].data;
                const ctx = chart.ctx;
                ctx.save();
                ctx.font = '600 12px Inter, system-ui, sans-serif';
                ctx.fillStyle = ink;
                meta.data.forEach(function (bar, i) {
                    if (horizontal) {
                        ctx.textAlign = 'left';
                        ctx.textBaseline = 'middle';
                        ctx.fillText(data[i], bar.x + 8, bar.y);
                    } else {
                        ctx.textAlign = 'center';
                        ctx.textBaseline = 'bottom';
                        ctx.fillText(data[i], bar.x, bar.y - 6);
                    }
                });
                ctx.restore();
            }
        };
    }

    // The total in the middle of a donut, with a small caption underneath
    function centerTotal(caption) {
        return {
            id: 'htCenter',
            afterDatasetsDraw(chart) {
                const meta = chart.getDatasetMeta(0);
                if (!meta.data.length) return;
                const total = chart.data.datasets[0].data.reduce(function (a, b) { return a + b; }, 0);
                const x = meta.data[0].x, y = meta.data[0].y;
                const ctx = chart.ctx;
                ctx.save();
                ctx.textAlign = 'center';
                ctx.textBaseline = 'middle';
                ctx.fillStyle = '#FFFFFF';
                ctx.font = '700 28px Oswald, "Arial Narrow", sans-serif';
                ctx.fillText(total, x, y - 8);
                ctx.fillStyle = muted;
                ctx.font = '500 12px Inter, system-ui, sans-serif';
                ctx.fillText(caption, x, y + 16);
                ctx.restore();
            }
        };
    }

    return {
        colors: colors,

        // Smooth area line for one measure over time (e.g. weight). unit is shown on the axis and in the bubble.
        // fromZero: start the y axis at 0 (for counts and minutes, which can never be negative).
        line: function (id, labels, values, label, unit, fromZero) {
            const canvas = find(id);
            if (!canvas || !window.Chart) return;
            const options = baseOptions(false);
            options.scales = { x: categoryAxis(), y: valueAxis(unit, fromZero) };
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
                        borderWidth: 3,
                        tension: 0.4,
                        cubicInterpolationMode: 'monotone',   // smooth, but never dips below the real values
                        fill: true,
                        backgroundColor: function (c) { return verticalGradient(c.chart, rgba(colors[0], 0.45), rgba(colors[0], 0)); },
                        pointRadius: values.length > 14 ? 0 : 4,
                        pointHoverRadius: 7,
                        pointBackgroundColor: '#FFFFFF',      // white centre with an orange ring
                        pointBorderColor: colors[0],
                        pointBorderWidth: 3
                    }]
                },
                options: options,
                plugins: values.length ? [lastValueBubble(unit)] : []
            });
        },

        // Vertical bars. series = [{ label, values }], one colour per series in palette order.
        // A single series with up to 8 bars also gets its values printed on top.
        bars: function (id, labels, series, unit) {
            const canvas = find(id);
            if (!canvas || !window.Chart) return;
            const options = baseOptions(series.length > 1);
            options.scales = { x: categoryAxis(), y: valueAxis(unit, true) };
            const showValues = series.length === 1 && labels.length <= 8;
            new Chart(canvas, {
                type: 'bar',
                data: {
                    labels: labels,
                    datasets: series.map(function (s, i) {
                        return {
                            label: s.label,
                            data: s.values,
                            backgroundColor: function (c) { return verticalGradient(c.chart, tints[i], rgba(colors[i], 0.75)); },
                            hoverBackgroundColor: tints[i],
                            borderRadius: { topLeft: 8, topRight: 8 },   // rounded end, flat on the baseline
                            borderSkipped: 'start',
                            maxBarThickness: series.length > 1 ? 18 : 30,
                            categoryPercentage: 0.62,
                            barPercentage: 0.9
                        };
                    })
                },
                options: options,
                plugins: showValues ? [barValues(0, false)] : []
            });
        },

        // Ranked list as slim pill bars on a soft track (like progress bars). One colour: the axis names each bar.
        ranked: function (id, labels, values, label) {
            const canvas = find(id);
            if (!canvas || !window.Chart) return;
            const max = Math.max.apply(null, values.concat([1]));
            const options = baseOptions(false);
            options.indexAxis = 'y';
            const digits = Math.max.apply(null, values.map(function (v) { return String(v).length; }).concat([1]));
            options.layout = { padding: { right: 16 + digits * 8 } };   // room for the value at the end (wider for big numbers)
            options.interaction = { mode: 'nearest', axis: 'y', intersect: false };
            options.plugins.tooltip.filter = function (item) { return item.datasetIndex === 1; };   // no tooltip for the track
            options.scales = {
                x: { display: false, beginAtZero: true, max: max },
                y: { grid: { display: false }, border: { display: false }, ticks: { color: '#D4D4D8', padding: 10 } }
            };
            new Chart(canvas, {
                type: 'bar',
                data: {
                    labels: labels,
                    datasets: [
                        {   // the grey track behind each bar
                            label: 'track',
                            data: values.map(function () { return max; }),
                            backgroundColor: 'rgba(255, 255, 255, 0.06)',
                            borderRadius: 999, borderSkipped: false, grouped: false,
                            barThickness: 12, order: 2
                        },
                        {   // the value bar, orange fading to a lighter orange
                            label: label,
                            data: values,
                            backgroundColor: function (c) {
                                const area = c.chart.chartArea;
                                if (!area) return colors[0];
                                const g = c.chart.ctx.createLinearGradient(area.left, 0, area.right, 0);
                                g.addColorStop(0, colors[0]);
                                g.addColorStop(1, tints[0]);
                                return g;
                            },
                            borderRadius: 999, borderSkipped: false, grouped: false,
                            barThickness: 12, order: 1
                        }
                    ]
                },
                options: options,
                plugins: [barValues(1, true)]
            });
        },

        // Donut for parts of a whole, with the total in the middle.
        // opts.colors: own colours (e.g. an ordered ramp); opts.caption: text under the total.
        donut: function (id, labels, values, opts) {
            const canvas = find(id);
            if (!canvas || !window.Chart) return;
            opts = opts || {};
            const options = baseOptions(true);
            options.cutout = '74%';
            options.layout = { padding: 8 };
            options.interaction = { mode: 'nearest', intersect: true };
            new Chart(canvas, {
                type: 'doughnut',
                data: {
                    labels: labels,
                    datasets: [{
                        data: values,
                        backgroundColor: opts.colors || colors.slice(0, values.length),
                        borderColor: surface,
                        borderWidth: 3,           // gap between slices
                        borderRadius: 8,          // rounded slice ends
                        hoverOffset: 6
                    }]
                },
                options: options,
                plugins: [centerTotal(opts.caption || 'Total')]
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
                        backgroundColor: function (c) { return verticalGradient(c.chart, rgba(colors[0], 0.35), rgba(colors[0], 0)); }
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
