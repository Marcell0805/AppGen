window.appgen = {
    copyToClipboard: function (text) {
        return navigator.clipboard.writeText(text);
    },
    saveSettings: function (settings) {
        localStorage.setItem('appgen.settings.v3', JSON.stringify(settings));
    },
    loadSettings: function () {
        const raw = localStorage.getItem('appgen.settings.v3')
            ?? localStorage.getItem('appgen.settings.v2');
        if (!raw) return null;
        const settings = JSON.parse(raw);
        if (settings.includeBlazorWeb !== undefined && settings.includeMvcWeb === undefined)
            settings.includeMvcWeb = settings.includeBlazorWeb;
        return settings;
    }
};
