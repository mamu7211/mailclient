window.blazorViewport = {
    getWidth: () => window.innerWidth,
    addResizeListener: (dotnetRef) => {
        const handler = () => dotnetRef.invokeMethodAsync('OnViewportResized', window.innerWidth);
        window.addEventListener('resize', handler);
        window.__blazorViewportHandler = handler;
    },
    removeResizeListener: () => {
        if (window.__blazorViewportHandler) {
            window.removeEventListener('resize', window.__blazorViewportHandler);
            delete window.__blazorViewportHandler;
        }
    }
};
