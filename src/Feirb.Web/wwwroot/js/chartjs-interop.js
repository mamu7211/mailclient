window.chartjsInterop = {
    _charts: {},

    renderBarChart: function (canvasId, labels, data, label) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) return;

        if (this._charts[canvasId]) {
            this._charts[canvasId].destroy();
        }

        var rgb = getComputedStyle(document.documentElement)
            .getPropertyValue('--bs-primary-rgb').trim() || '13, 110, 253';

        this._charts[canvasId] = new Chart(canvas, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [{
                    label: label,
                    data: data,
                    backgroundColor: 'rgba(' + rgb + ', 0.7)',
                    borderColor: 'rgba(' + rgb + ', 1)',
                    borderWidth: 1,
                    borderRadius: 4,
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                scales: {
                    y: {
                        beginAtZero: true,
                        ticks: {
                            stepSize: 1,
                            precision: 0,
                        }
                    }
                },
                plugins: {
                    legend: {
                        display: false,
                    }
                }
            }
        });
    },

    renderStackedBarChart: function (canvasId, labels, datasets) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) return;

        if (this._charts[canvasId]) {
            this._charts[canvasId].destroy();
        }

        this._charts[canvasId] = new Chart(canvas, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: datasets.map(function (ds) {
                    return {
                        label: ds.label,
                        data: ds.data,
                        backgroundColor: ds.color || '#6c757d',
                        borderWidth: 0,
                        borderRadius: 2,
                    };
                })
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                scales: {
                    x: { stacked: true },
                    y: {
                        stacked: true,
                        beginAtZero: true,
                        ticks: {
                            stepSize: 1,
                            precision: 0,
                        }
                    }
                },
                plugins: {
                    legend: {
                        display: datasets.length > 1,
                        position: 'bottom',
                        labels: {
                            boxWidth: 12,
                            padding: 8,
                            font: { size: 11 }
                        }
                    }
                }
            }
        });
    },

    renderDoughnutChart: function (canvasId, labels, data, colors, centerText) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) return;

        if (this._charts[canvasId]) {
            this._charts[canvasId].destroy();
        }

        var centerTextPlugin = {
            id: 'centerText',
            beforeDraw: function (chart) {
                if (!centerText) return;
                var ctx = chart.ctx;
                var chartArea = chart.chartArea;
                var centerX = (chartArea.left + chartArea.right) / 2;
                var centerY = (chartArea.top + chartArea.bottom) / 2;
                var areaSize = Math.min(chartArea.right - chartArea.left, chartArea.bottom - chartArea.top);
                ctx.save();
                var fontSize = areaSize / 5;
                ctx.font = 'bold ' + fontSize + 'px sans-serif';
                ctx.textAlign = 'center';
                ctx.textBaseline = 'middle';
                ctx.fillStyle = getComputedStyle(document.documentElement)
                    .getPropertyValue('--bs-primary').trim() || '#10B981';
                ctx.fillText(centerText, centerX, centerY);
                ctx.restore();
            }
        };

        this._charts[canvasId] = new Chart(canvas, {
            type: 'doughnut',
            data: {
                labels: labels,
                datasets: [{
                    data: data,
                    backgroundColor: colors,
                    borderWidth: 2,
                    borderColor: getComputedStyle(document.documentElement)
                        .getPropertyValue('--bs-body-bg').trim() || '#ffffff',
                }]
            },
            options: {
                responsive: true,
                maintainAspectRatio: false,
                cutout: '55%',
                plugins: {
                    legend: {
                        display: true,
                        position: 'bottom',
                        labels: {
                            boxWidth: 12,
                            padding: 8,
                            font: { size: 11 }
                        }
                    }
                }
            },
            plugins: [centerTextPlugin]
        });
    },

    destroyChart: function (canvasId) {
        if (this._charts[canvasId]) {
            this._charts[canvasId].destroy();
            delete this._charts[canvasId];
        }
    }
};
