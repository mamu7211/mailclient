window.toastInterop = {
    _dotNetRef: null,
    _initialized: false,

    _onAnimationEnd: function (e) {
        if (e.target.classList.contains('toast-indicator-fill') && e.animationName.startsWith('toast-drain')) {
            var toastId = e.target.getAttribute('data-toast-id');
            if (toastId && window.toastInterop._dotNetRef) {
                window.toastInterop._dotNetRef.invokeMethodAsync('OnAnimationEnd', toastId);
            }
        }
    },

    init: function (dotNetRef) {
        this._dotNetRef = dotNetRef;
        if (!this._initialized) {
            document.addEventListener('animationend', this._onAnimationEnd);
            this._initialized = true;
        }
    },

    clearRef: function () {
        this._dotNetRef = null;
    }
};
