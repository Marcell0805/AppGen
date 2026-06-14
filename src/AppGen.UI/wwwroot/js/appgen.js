window.appgen = {
    copyToClipboard: function (text) {
        return navigator.clipboard.writeText(text);
    },
    saveSettings: function (settings) {
        localStorage.setItem('appgen.settings', JSON.stringify(settings));
    },
    loadSettings: function () {
        const raw = localStorage.getItem('appgen.settings');
        return raw ? JSON.parse(raw) : null;
    }
};
