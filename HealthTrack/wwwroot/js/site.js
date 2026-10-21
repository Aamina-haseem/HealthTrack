// =====================================================================
// FILE:    wwwroot/js/site.js
// PURPOSE: Shared JavaScript, loaded on every page by _Layout.cshtml after
//          Chart.js. It defines window.HT, the chart helpers every view uses:
//            HT.line   - smooth area line over time (weight, minutes)
//            HT.bars   - vertical bars, one colour per series
//            HT.ranked - horizontal "progress bar" style ranked list
//            HT.donut  - parts of a whole with the total in the middle
//            HT.spark  - tiny trend line inside a KPI card
//          The views pass the data with @Json.Serialize(...).
// =====================================================================

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
// Runs once when the file loads: Chart.js defaults for the dark theme
(function () {
    // Stop if Chart.js did not load (e.g. no internet)
    if (!window.Chart) return;
    Chart.defaults.color = '#A1A1AA';                         // axis and legend text
    Chart.defaults.borderColor = 'rgba(255, 255, 255, 0.06)'; // grid lines
    // Same font as the rest of the page, 12px
    Chart.defaults.font.family = 'Inter, system-ui, sans-serif';
    Chart.defaults.font.size = 12;
    // People who ask their system for less motion get charts without animation
    if (window.matchMedia && window.matchMedia('(prefers-reduced-motion: reduce)').matches) {
        Chart.defaults.animation = false;
    }
})();

// HT: the function runs once and returns the public helpers; everything else inside stays private
window.HT = (function () {
    // Series colours in a fixed order (validated: lightness, contrast and colour-blind separation)
    const colors = ['#EE6122', '#3987e5', '#199e70', '#9085e9'];
    // A lighter tint of each colour, used only for the top of gradients (same hue, so the meaning does not change)
    const tints = ['#FF9A5C', '#6BA8F0', '#3FC194', '#B3A9F2'];
    const surface = '#18181B';    // card background
    const ink = '#F5F5F4';        // main text
    const muted = '#A1A1AA';      // secondary text

    // Short name for document.getElementById
    function find(id) { return document.getElementById(id); }

    // Turns "#EE6122" into "rgba(238, 97, 34, a)"
    function rgba(hex, a) {
        // The hex digits as one number
        const n = parseInt(hex.slice(1), 16);
        // Red = first 8 bits, green = middle 8, blue = last 8
        return 'rgba(' + (n >> 16) + ', ' + ((n >> 8) & 255) + ', ' + (n & 255) + ', ' + a + ')';
    }

    // Settings shared by every chart: hover shows all values for that point, tooltip styled for the dark theme
    function baseOptions(showLegend) {
        // A Chart.js options object
        return {
            // Resize with the card
            responsive: true,
            // Fill the height set in CSS (.ht-chart)
            maintainAspectRatio: false,
            // Hovering a column shows every value in it
            interaction: { mode: 'index', intersect: false },
            layout: { padding: { top: 24, right: 8 } },             // room for the value labels
            plugins: {
                // Legend under the chart, with small round markers
                legend: {
                    display: showLegend,
                    position: 'bottom',
                    labels: { usePointStyle: true, pointStyle: 'circle', boxWidth: 8, boxHeight: 8, padding: 18, color: '#D4D4D8' }
                },
                // Dark tooltip that matches the theme
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
            // Counts start at 0; weights may start nearer the data
            beginAtZero: !!fromZero,
            // 8% space above and below the line
            grace: fromZero ? 0 : '8%',
            // No axis line
            border: { display: false, dash: [4, 4] },
            // Faint grid lines
            grid: { color: 'rgba(255, 255, 255, 0.07)' },
            // Whole numbers only for counts
            ticks: { padding: 8, precision: fromZero ? 0 : undefined },
            // Unit (e.g. kg) as the axis title
            title: { display: !!unit, text: unit, color: muted }
        };
    }
    // The x-axis (labels such as dates): no grid, no axis line
    function categoryAxis() {
        return { grid: { display: false }, border: { display: false }, ticks: { padding: 8 } };
    }

    // Vertical gradient: strong at the top, fading towards the bottom (used for areas and bars)
    function verticalGradient(chart, top, bottom) {
        // The drawing area (only known after the first layout)
        const area = chart.chartArea;
        if (!area) return top;                                    // first render: no size yet
        // Gradient from the top of the chart to the bottom
        const g = chart.ctx.createLinearGradient(0, area.top, 0, area.bottom);
        // Colour at the top (0) and at the bottom (1)
        g.addColorStop(0, top);
        g.addColorStop(1, bottom);
        return g;
    }

    // Draws a rounded rectangle (used by the value bubble)
    function roundRect(ctx, x, y, w, h, r) {
        // Four sides with rounded corners (arcTo)
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
        // A Chart.js plugin is an object with an id and drawing hooks
        return {
            id: 'htBubble',
            // Runs after the lines are drawn, so the bubble is on top
            afterDatasetsDraw(chart) {
                // Drawn points of the first dataset
                const meta = chart.getDatasetMeta(0);
                // The last point
                const point = meta.data[meta.data.length - 1];
                if (!point) return;
                // Its value
                const value = chart.data.datasets[0].data[meta.data.length - 1];
                // "1 log" but "2 logs": drop the plural s when the value is exactly 1
                const word = value === 1 && /[a-z]s$/.test(unit || '') ? unit.slice(0, -1) : unit;
                // e.g. "78.4 kg"
                const text = value + (word ? ' ' + word : '');
                // The canvas drawing context
                const ctx = chart.ctx;
                // Save the drawing settings (restored at the end)
                ctx.save();
                // Font for the bubble text
                ctx.font = '600 12px Inter, system-ui, sans-serif';
                // Bubble size from the text width
                const w = ctx.measureText(text).width + 16, h = 22;
                // Centre the bubble over the point
                let x = point.x - w / 2;
                x = Math.max(chart.chartArea.left, Math.min(x, chart.chartArea.right - w));   // keep it inside the chart
                // 10px above the point
                const y = point.y - h - 10;
                // Draw the pill and fill it orange
                roundRect(ctx, x, y, w, h, 11);
                ctx.fillStyle = colors[0];
                ctx.fill();
                ctx.fillStyle = '#0E0E10';                       // dark text on orange (7:1 contrast)
                // Centre the text in the pill
                ctx.textAlign = 'center';
                ctx.textBaseline = 'middle';
                ctx.fillText(text, x + w / 2, y + h / 2 + 1);
                // Put the drawing settings back
                ctx.restore();
            }
        };
    }

    // Writes each bar's value at its end (above a vertical bar, right of a horizontal one)
    function barValues(datasetIndex, horizontal) {
        return {
            id: 'htBarValues',
            afterDatasetsDraw(chart) {
                // Bars and values of the chosen dataset
                const meta = chart.getDatasetMeta(datasetIndex);
                const data = chart.data.datasets[datasetIndex].data;
                const ctx = chart.ctx;
                ctx.save();
                ctx.font = '600 12px Inter, system-ui, sans-serif';
                // Light text colour
                ctx.fillStyle = ink;
                // One label per bar
                meta.data.forEach(function (bar, i) {
                    // Horizontal bars: value to the right of the bar end
                    if (horizontal) {
                        ctx.textAlign = 'left';
                        ctx.textBaseline = 'middle';
                        ctx.fillText(data[i], bar.x + 8, bar.y);
                    } else {
                        // Vertical bars: value above the bar
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
                // The donut slices
                const meta = chart.getDatasetMeta(0);
                // Nothing to draw without data
                if (!meta.data.length) return;
                // Add up all the values
                const total = chart.data.datasets[0].data.reduce(function (a, b) { return a + b; }, 0);
                // Centre of the donut
                const x = meta.data[0].x, y = meta.data[0].y;
                const ctx = chart.ctx;
                ctx.save();
                ctx.textAlign = 'center';
                ctx.textBaseline = 'middle';
                // The total: big white number
                ctx.fillStyle = '#FFFFFF';
                ctx.font = '700 28px Oswald, "Arial Narrow", sans-serif';
                ctx.fillText(total, x, y - 8);
                // The caption: small grey text under it
                ctx.fillStyle = muted;
                ctx.font = '500 12px Inter, system-ui, sans-serif';
                ctx.fillText(caption, x, y + 16);
                ctx.restore();
            }
        };
    }

    // The public helpers, used as HT.line(...), HT.bars(...) etc. in the views
    return {
        // Shared so the views can reuse the palette
        colors: colors,

        // Smooth area line for one measure over time (e.g. weight). unit is shown on the axis and in the bubble.
        // fromZero: start the y axis at 0 (for counts and minutes, which can never be negative).
        line: function (id, labels, values, label, unit, fromZero) {
            // The <canvas> with this id
            const canvas = find(id);
            // Stop if the canvas is not on the page (e.g. an empty state) or Chart.js is missing
            if (!canvas || !window.Chart) return;
            // Shared settings, no legend (one series)
            const options = baseOptions(false);
            // x = labels (dates), y = values
            options.scales = { x: categoryAxis(), y: valueAxis(unit, fromZero) };
            // Tooltip text, e.g. " Weight: 78.4 kg"
            options.plugins.tooltip.callbacks = {
                label: function (c) { return ' ' + label + ': ' + c.formattedValue + (unit ? ' ' + unit : ''); }
            };
            // Create the chart in the canvas
            new Chart(canvas, {
                type: 'line',
                data: {
                    labels: labels,
                    datasets: [{
                        label: label,
                        data: values,
                        // Orange line, 3px wide
                        borderColor: colors[0],
                        borderWidth: 3,
                        // Curved line
                        tension: 0.4,
                        cubicInterpolationMode: 'monotone',   // smooth, but never dips below the real values
                        // Area under the line, orange fading to transparent
                        fill: true,
                        backgroundColor: function (c) { return verticalGradient(c.chart, rgba(colors[0], 0.45), rgba(colors[0], 0)); },
                        // Hide the dots when there are many points
                        pointRadius: values.length > 14 ? 0 : 4,
                        pointHoverRadius: 7,
                        pointBackgroundColor: '#FFFFFF',      // white centre with an orange ring
                        pointBorderColor: colors[0],
                        pointBorderWidth: 3
                    }]
                },
                options: options,
                // Value bubble on the last point (only when there is data)
                plugins: values.length ? [lastValueBubble(unit)] : []
            });
        },

        // Vertical bars. series = [{ label, values }], one colour per series in palette order.
        // A single series with up to 8 bars also gets its values printed on top.
        bars: function (id, labels, series, unit) {
            const canvas = find(id);
            if (!canvas || !window.Chart) return;
            // A legend only when there is more than one series
            const options = baseOptions(series.length > 1);
            // Bars always start at 0
            options.scales = { x: categoryAxis(), y: valueAxis(unit, true) };
            const showValues = series.length === 1 && labels.length <= 8;
            new Chart(canvas, {
                type: 'bar',
                data: {
                    labels: labels,
                    // One dataset per series, coloured in palette order
                    datasets: series.map(function (s, i) {
                        return {
                            label: s.label,
                            data: s.values,
                            // Gradient from the tint to the colour
                            backgroundColor: function (c) { return verticalGradient(c.chart, tints[i], rgba(colors[i], 0.75)); },
                            hoverBackgroundColor: tints[i],
                            borderRadius: { topLeft: 8, topRight: 8 },   // rounded end, flat on the baseline
                            borderSkipped: 'start',
                            // Thinner bars when they are grouped
                            maxBarThickness: series.length > 1 ? 18 : 30,
                            // Space between the groups of bars
                            categoryPercentage: 0.62,
                            barPercentage: 0.9
                        };
                    })
                },
                options: options,
                // Values on top only for a single short series
                plugins: showValues ? [barValues(0, false)] : []
            });
        },

        // Ranked list as slim pill bars on a soft track (like progress bars). One colour: the axis names each bar.
        ranked: function (id, labels, values, label) {
            const canvas = find(id);
            if (!canvas || !window.Chart) return;
            // The longest bar (at least 1, so empty data still works)
            const max = Math.max.apply(null, values.concat([1]));
            const options = baseOptions(false);
            // 'y' makes the bars horizontal
            options.indexAxis = 'y';
            // Digits in the biggest value
            const digits = Math.max.apply(null, values.map(function (v) { return String(v).length; }).concat([1]));
            options.layout = { padding: { right: 16 + digits * 8 } };   // room for the value at the end (wider for big numbers)
            // Hover the nearest row
            options.interaction = { mode: 'nearest', axis: 'y', intersect: false };
            options.plugins.tooltip.filter = function (item) { return item.datasetIndex === 1; };   // no tooltip for the track
            // No x-axis (the value is printed at the end); names on the y-axis
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
                            // Every track is full length
                            data: values.map(function () { return max; }),
                            backgroundColor: 'rgba(255, 255, 255, 0.06)',
                            borderRadius: 999, borderSkipped: false, grouped: false,
                            barThickness: 12, order: 2
                        },
                        {   // the value bar, orange fading to a lighter orange
                            label: label,
                            data: values,
                            // Left-to-right gradient for the value bar
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
                // Print each value at the end of its bar (dataset 1 = the value bar)
                plugins: [barValues(1, true)]
            });
        },

        // Donut for parts of a whole, with the total in the middle.
        // opts.colors: own colours (e.g. an ordered ramp); opts.caption: text under the total.
        donut: function (id, labels, values, opts) {
            const canvas = find(id);
            if (!canvas || !window.Chart) return;
            // opts is optional
            opts = opts || {};
            // Donuts show a legend so each colour is named
            const options = baseOptions(true);
            // Size of the hole in the middle (room for the total)
            options.cutout = '74%';
            options.layout = { padding: 8 };
            // Tooltip for the slice under the mouse
            options.interaction = { mode: 'nearest', intersect: true };
            new Chart(canvas, {
                type: 'doughnut',
                data: {
                    labels: labels,
                    datasets: [{
                        data: values,
                        // The given colours, or the palette in order
                        backgroundColor: opts.colors || colors.slice(0, values.length),
                        borderColor: surface,
                        borderWidth: 3,           // gap between slices
                        borderRadius: 8,          // rounded slice ends
                        // The slice moves out a little on hover
                        hoverOffset: 6
                    }]
                },
                options: options,
                // Total in the middle; the caption defaults to "Total"
                plugins: [centerTotal(opts.caption || 'Total')]
            });
        },

        // Tiny trend line inside a stat card (no axes; the card shows the number itself)
        spark: function (id, values) {
            const canvas = find(id);
            // A trend needs at least two values
            if (!canvas || !window.Chart || values.length < 2) return;
            new Chart(canvas, {
                type: 'line',
                data: {
                    // Labels are just 0, 1, 2 ... (the axes are hidden)
                    labels: values.map(function (_, i) { return i; }),
                    datasets: [{
                        data: values,
                        borderColor: colors[0],
                        borderWidth: 2,
                        tension: 0.4,
                        cubicInterpolationMode: 'monotone',
                        // No dots
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
                    // No axes
                    scales: { x: { display: false }, y: { display: false } }
                }
            });
        }
    };
})();
