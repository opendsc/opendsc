window.openDscEditor = window.openDscEditor || {
    getCursorPosition: function (inputId) {
        if (!inputId) {
            return 0;
        }

        const element = document.getElementById(inputId);
        if (!element) {
            return 0;
        }

        if (typeof element.selectionStart === "number") {
            return element.selectionStart;
        }

        return 0;
    }
};
