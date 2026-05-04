// Closes a DropdownButton when the user clicks outside or presses Escape.
// Returns a handle with a `detach()` method so .NET can clean up.
window.feirbDropdown = {
    attach: function (rootElement, dotNetRef) {
        const safeClose = function () {
            if (!dotNetRef) {
                return;
            }
            try {
                const promise = dotNetRef.invokeMethodAsync('CloseAsync');
                if (promise && typeof promise.catch === 'function') {
                    // Component disposed or circuit dropped — swallow silently to
                    // avoid unhandled-promise-rejection noise in the browser console.
                    promise.catch(function () { });
                }
            } catch (e) {
                // dotNetRef may have been revoked already.
            }
        };

        const onPointerDown = function (e) {
            if (rootElement && !rootElement.contains(e.target)) {
                safeClose();
            }
        };
        const onKeyDown = function (e) {
            if (e.key === 'Escape') {
                safeClose();
            }
        };

        document.addEventListener('pointerdown', onPointerDown, true);
        document.addEventListener('keydown', onKeyDown, true);

        return {
            detach: function () {
                document.removeEventListener('pointerdown', onPointerDown, true);
                document.removeEventListener('keydown', onKeyDown, true);
            }
        };
    }
};
