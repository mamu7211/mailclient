// Closes a DropdownButton when the user clicks outside or presses Escape.
// Returns a handle with a `detach()` method so .NET can clean up.
window.feirbDropdown = {
    attach: function (rootElement, dotNetRef) {
        const onPointerDown = function (e) {
            if (rootElement && !rootElement.contains(e.target)) {
                dotNetRef.invokeMethodAsync('CloseAsync');
            }
        };
        const onKeyDown = function (e) {
            if (e.key === 'Escape') {
                dotNetRef.invokeMethodAsync('CloseAsync');
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
