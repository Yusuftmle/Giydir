// Token helper for API calls
window.getAuthToken = function () {
    const token = localStorage.getItem('jwtToken');
    return token || null;
};

// Helper to add auth header to fetch requests
window.fetchWithAuth = async function (url, options = {}) {
    const token = localStorage.getItem('jwtToken');

    if (!options.headers) {
        options.headers = {};
    }

    if (token) {
        options.headers['Authorization'] = `Bearer ${token}`;
    }

    return fetch(url, options);
};
