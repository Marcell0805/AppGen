window.appgen = {
    copyToClipboard: function (text) {
        return navigator.clipboard.writeText(text);
    },
    downloadTextFile: function (filename, content, contentType) {
        const blob = new Blob([content], { type: contentType ?? 'application/json' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = filename;
        a.style.display = 'none';
        document.body.appendChild(a);
        a.click();
        a.remove();
        URL.revokeObjectURL(url);
    },
    pickTextFile: function (accept) {
        return new Promise((resolve) => {
            const input = document.createElement('input');
            input.type = 'file';
            input.accept = accept ?? '.json,application/json';
            input.style.display = 'none';
            document.body.appendChild(input);

            input.addEventListener('change', () => {
                const file = input.files && input.files.length > 0 ? input.files[0] : null;
                if (!file) {
                    input.remove();
                    resolve(null);
                    return;
                }

                const reader = new FileReader();
                reader.onload = () => {
                    const text = typeof reader.result === 'string' ? reader.result : null;
                    input.remove();
                    resolve(text);
                };
                reader.onerror = () => {
                    input.remove();
                    resolve(null);
                };
                reader.readAsText(file);
            });

            input.click();
        });
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
