window.chartjsInterop = {
    _charts: {},

    renderBarChart: function (canvasId, labels, data, label) {
        const canvas = document.getElementById(canvasId);
        if (!canvas) return;

        if (this._charts[canvasId]) {
            this._charts[canvasId].destroy();
        }

        this._charts[canvasId] = new Chart(canvas, {
            type: 'bar',
            data: {
                labels: labels,
                datasets: [{
                    label: label,
                    data: data,
                    backgroundColor: 'rgba(13, 110, 253, 0.7)',
                    borderColor: 'rgba(13, 110, 253, 1)',
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

    destroyChart: function (canvasId) {
        if (this._charts[canvasId]) {
            this._charts[canvasId].destroy();
            delete this._charts[canvasId];
        }
    }
};
