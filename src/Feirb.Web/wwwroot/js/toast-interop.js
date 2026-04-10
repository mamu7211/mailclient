window.toastInterop = {
    _dotNetRef: null,

    init: function (dotNetRef) {
        this._dotNetRef = dotNetRef;
        document.addEventListener('animationend', function (e) {
            if (e.target.classList.contains('toast-indicator-fill') && e.animationName.startsWith('toast-drain')) {
                var toastId = e.target.getAttribute('data-toast-id');
                if (toastId && window.toastInterop._dotNetRef) {
                    window.toastInterop._dotNetRef.invokeMethodAsync('OnAnimationEnd', toastId);
                }
            }
        });
    }
};
