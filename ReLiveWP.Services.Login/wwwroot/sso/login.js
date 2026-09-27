// progressive enhancement only. the form works without any of this.
(function () {
    var form = document.querySelector('.auth-form');
    if (!form) return;

    form.onsubmit = function () {
        var submit = form.querySelector('.submit');
        if (submit) {
            submit.disabled = true;
            submit.value = 'Signing in...';
        }
    };

    var fields = form.querySelectorAll('input.textbox');
    for (var i = 0; i < fields.length; i++) {
        if (!fields[i].value) {
            fields[i].focus();
            break;
        }
    }
})();
