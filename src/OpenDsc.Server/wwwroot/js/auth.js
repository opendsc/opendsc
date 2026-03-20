window.loginApi = {
    login: async function (username, password) {
        try {
            const response = await fetch('/api/v1/auth/login', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ username, password })
            });

            if (!response.ok) {
                return { success: false, error: 'Invalid username or password', requirePasswordChange: false };
            }

            const data = await response.json();
            return { success: true, error: null, requirePasswordChange: data.requirePasswordChange };
        } catch {
            return { success: false, error: 'An error occurred during login', requirePasswordChange: false };
        }
    }
};
