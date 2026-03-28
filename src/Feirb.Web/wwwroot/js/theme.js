window.blazorTheme = {
    get: () => localStorage.getItem('BlazorTheme') || 'green-light',
    set: (value) => {
        localStorage.setItem('BlazorTheme', value);
        document.documentElement.setAttribute('data-theme', value);
    }
};
