window.buildStorage = {
    save: function (key, value) {
        localStorage.setItem(key, JSON.stringify(value));
    },
    load: function (key) {
        const v = localStorage.getItem(key);
        return v ? JSON.parse(v) : null;
    },
    clear: function (key) {
        localStorage.removeItem(key);
    }
};
