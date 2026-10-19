// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Chart.js text and grid colours that suit the dark copper theme.
// Chart.js is loaded in _Layout just before this file, so it is ready here.
if (window.Chart) {
    Chart.defaults.color = '#A8A29E';
    Chart.defaults.borderColor = 'rgba(255, 255, 255, 0.08)';
    Chart.defaults.font.family = 'Inter, system-ui, sans-serif';
}
