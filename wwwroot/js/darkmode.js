
window.clipboardHelper = {
    copy: function (text) {
        return navigator.clipboard.writeText(text);
    }
};

window.darkMode = {
    toggle: function () {
        const isDark = document.documentElement.classList.toggle('dark');
        localStorage.setItem('darkMode', isDark);
        return isDark;
    },
    get: function () {
        return document.documentElement.classList.contains('dark');
    }
};