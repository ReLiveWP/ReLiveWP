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

    var username = document.getElementById('username');
    var password = document.getElementById('password');
    var focusTarget = username && !username.value ? username : password;
    if (focusTarget) focusTarget.focus();
})();
