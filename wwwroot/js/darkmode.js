
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