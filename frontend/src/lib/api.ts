import axios from 'axios';
import { goto } from '$app/navigation';

export const api = axios.create({
    baseURL: 'http://localhost:5000/api', // Default ASP.NET Core port
    headers: {
        'Content-Type': 'application/json'
    }
});

api.interceptors.request.use((config) => {
    const token = localStorage.getItem('token');
    if (token) {
        config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
});

api.interceptors.response.use(
    (response) => response,
    (error) => {
        if (error.response && (error.response.status === 401 || error.response.status === 403)) {
            // Token is invalid, user is deleted, or blocked
            localStorage.removeItem('token');
            localStorage.removeItem('user');

            // Extract message from response if available
            const message = error.response.data?.message || 'Session expired or unauthorized.';

            // We use standard window.location to force a hard reload and clear state if needed,
            // or we could use sveltekit's goto. Let's use goto.
            goto(`/login?error=${encodeURIComponent(message)}`);
        }
        return Promise.reject(error);
    }
);
