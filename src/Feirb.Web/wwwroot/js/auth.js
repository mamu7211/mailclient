window.blazorAuth = {
    getAccessToken: () => localStorage.getItem('AccessToken'),
    setAccessToken: (accessToken) => {
        localStorage.setItem('AccessToken', accessToken);
    },
    clearTokens: () => {
        localStorage.removeItem('AccessToken');
    }
};
