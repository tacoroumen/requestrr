async function readResponse(response, fallbackError) {
    const text = await response.text();
    let data = null;

    try {
        data = JSON.parse(text);
    } catch {
        data = text;
    }

    if (response.ok) {
        return { ok: true, data: data };
    }

    return { ok: false, error: typeof (data) === "string" && data ? data : fallbackError };
}

export function getLinkedUsers() {
    return (dispatch, getState) => {
        const state = getState();

        return fetch("../api/linkedusers", {
            method: 'GET',
            headers: {
                'Accept': 'application/json',
                'Authorization': `Bearer ${state.user.token}`
            }
        })
            .then(response => readResponse(response, "Could not load the linked users."))
            .then(result => result.ok ? { ok: true, userLinks: result.data.userLinks || [] } : result)
            .catch(() => ({ ok: false, error: "Could not load the linked users." }));
    };
};

export function saveLinkedUsers(userLinks) {
    return (dispatch, getState) => {
        const state = getState();

        return fetch("../api/linkedusers", {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json',
                'Authorization': `Bearer ${state.user.token}`
            },
            body: JSON.stringify({ userLinks: userLinks })
        })
            .then(response => readResponse(response, "An unknown error occurred while saving."))
            .catch(() => ({ ok: false, error: "An unknown error occurred while saving." }));
    };
};

export function getOverseerrUsers() {
    return (dispatch, getState) => {
        const state = getState();

        return fetch("../api/linkedusers/overseerr-users", {
            method: 'GET',
            headers: {
                'Accept': 'application/json',
                'Authorization': `Bearer ${state.user.token}`
            }
        })
            .then(response => readResponse(response, "Could not load the Seerr users."))
            .then(result => result.ok && Array.isArray(result.data) ? { ok: true, users: result.data } : { ok: false, error: result.error || "Could not load the Seerr users." })
            .catch(() => ({ ok: false, error: "Could not load the Seerr users." }));
    };
};
