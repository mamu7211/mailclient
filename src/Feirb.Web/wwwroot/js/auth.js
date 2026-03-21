window.blazorAuth = {
    getAccessToken: () => localStorage.getItem('AccessToken'),
    getRefreshToken: () => localStorage.getItem('RefreshToken'),
    setTokens: (accessToken, refreshToken) => {
        localStorage.setItem('AccessToken', accessToken);
        localStorage.setItem('RefreshToken', refreshToken);
    },
    clearTokens: () => {
        localStorage.removeItem('AccessToken');
        localStorage.removeItem('RefreshToken');
    }
};
