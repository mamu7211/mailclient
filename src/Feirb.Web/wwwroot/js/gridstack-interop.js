window.gridstackInterop = {
    _grid: null,
    _dotNetRef: null,

    init: function (dotNetRef, isEditMode) {
        this._dotNetRef = dotNetRef;

        var el = document.getElementById('dashboard-grid');
        if (!el) {
            console.warn('gridstackInterop.init: #dashboard-grid not found in DOM');
            return;
        }

        this._grid = GridStack.init({
            cellHeight: 120,
            margin: 8,
            column: 12,
            animate: true,
            disableDrag: !isEditMode,
            disableResize: !isEditMode,
            float: false,
        }, el);

        if (!this._grid) return;

        this._grid.on('change', () => {
            if (this._dotNetRef) {
                var layout = this.serialize();
                this._dotNetRef.invokeMethodAsync('OnLayoutChanged', layout);
            }
        });
    },

    setEditMode: function (enabled) {
        if (!this._grid) return;
        if (enabled) {
            this._grid.enableMove(true);
            this._grid.enableResize(true);
        } else {
            this._grid.enableMove(false);
            this._grid.enableResize(false);
        }
    },

    serialize: function () {
        if (!this._grid) return '[]';
        var items = this._grid.getGridItems().map(function (el) {
            var node = el.gridstackNode;
            return {
                id: node.id || el.getAttribute('gs-id'),
                x: node.x,
                y: node.y,
                w: node.w,
                h: node.h,
            };
        });
        return JSON.stringify(items);
    },

    destroy: function () {
        if (this._grid) {
            this._grid.destroy(false);
            this._grid = null;
        }
        this._dotNetRef = null;
    }
};
