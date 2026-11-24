export const GET_SETTINGS = "chatclients:get_settings";

export function setSettings(settings) {
    return {
        type: GET_SETTINGS,
        payload: settings
    };
};

export function getSettings() {
    return (dispatch, getState) => {
        const state = getState();

        return fetch("../api/chatclients", {
            method: 'GET',
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${state.user.token}`
            }
        })
            .then(data => data.json())
            .then(data => {
                return dispatch(setSettings(data));
            });
    };
};

export function testSettings(settings) {
    return (dispatch, getState) => {
        const state = getState();

        return fetch("../api/chatclients/discord/test", {
            method: 'POST',
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${state.user.token}`
            },
            body: JSON.stringify({
                "BotToken": settings.botToken,
            })
        })
            .then(data => data.json())
            .then(data => {
                if (data.ok) {
                    return { ok: true };
                }

                return { ok: false, error: data }
            });
    };
};

export function testMatrixSettings(settings) {
    return (dispatch, getState) => {
        const state = getState();

        return fetch("../api/chatclients/matrix/test", {
            method: 'POST',
            headers: {
                'Accept': 'application/json',
                'Content-Type': 'application/json',
                'Authorization': `Bearer ${state.user.token}`
            },
            body: JSON.stringify({
                "HomeserverUrl": settings.homeserverUrl,
                "AccessToken": settings.accessToken,
            }),
            signal: typeof AbortSignal !== "undefined" && AbortSignal.timeout ? AbortSignal.timeout(30000) : undefined
        })
            .then(async response => {
                const text = await response.text();
                let data = null;

                try {
                    data = JSON.parse(text);
                } catch {
                    data = null;
                }

                if (response.ok && data && data.ok) {
                    return { ok: true };
                }

                if (typeof (data) === "string") {
                    return { ok: false, error: data };
                }

                if (data === null && text && text.length < 500 && !text.trim().startsWith("<")) {
                    return { ok: false, error: text };
                }

                return { ok: false, error: `Requestrr answered with HTTP ${response.status}. Check the container logs.` };
            })
            .catch(error => ({
                ok: false,
                error: error && error.name === "TimeoutError" ? "The test took longer than 30 seconds. Check the container logs." : "Could not reach Requestrr to run the test."
            }));
    };
};

export function save(saveModel) {
    return (dispatch, getState) => {
        const state = getState();

        return fetch("../api/chatclients", {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
                'Accept': 'application/json',
                'Authorization': `Bearer ${state.user.token}`
            },
            body: JSON.stringify({
                'Client': saveModel.client,
                'ClientId': saveModel.clientId,
                'BotToken': saveModel.botToken,
                'StatusMessage': saveModel.statusMessage,
                'MonitoredChannels': saveModel.monitoredChannels,
                'TvShowRoles': saveModel.tvShowRoles,
                'MovieRoles': saveModel.movieRoles,
                'MusicRoles': saveModel.musicRoles,
                'RequestApprovalAdminRoleIds': saveModel.requestApprovalAdminRoleIds,
                'RequestApprovalAdminChannelIds': saveModel.requestApprovalAdminChannelIds,
                'RequestApprovalAdminChannelAllRequests': saveModel.requestApprovalAdminChannelAllRequests,
                'RequestApprovalEmojiApprove': saveModel.requestApprovalEmojiApprove,
                'RequestApprovalEmojiDeny': saveModel.requestApprovalEmojiDeny,
                'RequestApprovalEnabled': saveModel.requestApprovalEnabled,
                'EnableRequestsThroughDirectMessages': saveModel.enableRequestsThroughDirectMessages,
                'AutomaticallyNotifyRequesters': saveModel.automaticallyNotifyRequesters,
                'NotificationMode': saveModel.notificationMode,
                'NotificationChannels': saveModel.notificationChannels,
                'AutomaticallyPurgeCommandMessages': saveModel.automaticallyPurgeCommandMessages,
                'Matrix': saveModel.matrix,
                'Language': saveModel.language,
            })
        })
            .then(data => data.json())
            .then(data => {
                if (data.ok) {
                    dispatch(setSettings({
                        chatClient: saveModel.client,
                        clientId: saveModel.clientId,
                        botToken: saveModel.botToken,
                        statusMessage: saveModel.statusMessage,
                        monitoredChannels: saveModel.monitoredChannels,
                        tvShowRoles: saveModel.tvShowRoles,
                        movieRoles: saveModel.movieRoles,
                        musicRoles: saveModel.musicRoles,
                        requestApprovalAdminRoleIds: saveModel.requestApprovalAdminRoleIds,
                        requestApprovalAdminChannelIds: saveModel.requestApprovalAdminChannelIds,
                        requestApprovalAdminChannelAllRequests: saveModel.requestApprovalAdminChannelAllRequests,
                        requestApprovalEmojiApprove: saveModel.requestApprovalEmojiApprove,
                        requestApprovalEmojiDeny: saveModel.requestApprovalEmojiDeny,
                        requestApprovalEnabled: saveModel.requestApprovalEnabled,
                        enableRequestsThroughDirectMessages: saveModel.enableRequestsThroughDirectMessages,
                        automaticallyNotifyRequesters: saveModel.automaticallyNotifyRequesters,
                        notificationMode: saveModel.notificationMode,
                        notificationChannels: saveModel.notificationChannels,
                        automaticallyPurgeCommandMessages: saveModel.automaticallyPurgeCommandMessages,
                        language: saveModel.language,
                    }));
                    return { ok: true };
                }

                return { ok: false, error: data };
            });
    }
};

export function getOverseerrUsers() {
    return (dispatch, getState) => {
        const state = getState();

        return fetch("../api/chatclients/matrix/overseerr-users", {
            method: 'GET',
            headers: {
                'Accept': 'application/json',
                'Authorization': `Bearer ${state.user.token}`
            }
        })
            .then(async response => {
                const text = await response.text();
                let data = null;

                try {
                    data = JSON.parse(text);
                } catch {
                    data = null;
                }

                if (response.ok && Array.isArray(data)) {
                    return { ok: true, users: data };
                }

                return { ok: false, error: typeof (data) === "string" ? data : "Could not load the Seerr users." };
            })
            .catch(() => ({ ok: false, error: "Could not load the Seerr users." }));
    };
};
