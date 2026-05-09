// wwwroot/js/facturacion.js

window.bloquearZoomGlobal = function () {

    document.addEventListener("keydown", function (e) {
        if (e.ctrlKey && (
            e.key === "+" ||
            e.key === "=" ||
            e.key === "-" ||
            e.key === "_" ||
            e.key === "0"
        )) {
            e.preventDefault();
        }
    });

    document.addEventListener("wheel", function (e) {
        if (e.ctrlKey) {
            e.preventDefault();
        }
    }, { passive: false });

};

window.focusInput = function (input) {
    if (input) {
        input.focus();
    }
};

window.focusInputGlobal = function (input) {

    if (!input) return;

    // foco inicial
    input.focus();

    // recuperar foco si se pierde
    document.addEventListener("click", function () {
        setTimeout(() => {
            input.focus();
        }, 0);
    });

};

window.abrirTicket = function (html) {

    const ventana = window.open('', '_blank', 'width=400,height=600');

    ventana.document.open();
    ventana.document.write(html);
    ventana.document.close();
};