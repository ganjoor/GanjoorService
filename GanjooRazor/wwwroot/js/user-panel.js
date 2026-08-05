// User area (پیشخان کاربر) shared UI helpers.
// Provides upConfirm() and upToast() as drop-in, styled replacements for
// window.confirm()/window.alert(), used across the panel's AJAX actions.
// Requires the markup from Areas/User/Pages/Shared/_ConfirmModal.cshtml and
// _Toasts.cshtml to be present on the page (included once by _UserPanelLayout).

/**
 * Shows a styled confirmation dialog and resolves to true/false.
 * Falls back to window.confirm() if the modal markup isn't on the page.
 * @param {string} message
 * @param {{okText?: string, cancelText?: string, danger?: boolean}} [options]
 * @returns {Promise<boolean>}
 */
function upConfirm(message, options) {
    options = options || {};
    return new Promise(function (resolve) {
        var backdrop = document.getElementById('upConfirmBackdrop');
        var titleEl = document.getElementById('upConfirmTitle');
        var okBtn = document.getElementById('upConfirmOk');
        var cancelBtn = document.getElementById('upConfirmCancel');

        if (!backdrop || !titleEl || !okBtn || !cancelBtn) {
            resolve(window.confirm(message));
            return;
        }

        titleEl.textContent = message;
        okBtn.textContent = options.okText || 'تأیید';
        cancelBtn.textContent = options.cancelText || 'انصراف';
        okBtn.className = 'up-btn ' + (options.danger === false ? 'up-btn--success' : 'up-btn--danger');

        backdrop.hidden = false;
        document.body.classList.add('up-modal-open');

        function cleanup(result) {
            backdrop.hidden = true;
            document.body.classList.remove('up-modal-open');
            okBtn.removeEventListener('click', onOk);
            cancelBtn.removeEventListener('click', onCancel);
            backdrop.removeEventListener('click', onBackdropClick);
            document.removeEventListener('keydown', onKeyDown);
            resolve(result);
        }

        function onOk() { cleanup(true); }
        function onCancel() { cleanup(false); }
        function onBackdropClick(e) { if (e.target === backdrop) cleanup(false); }
        function onKeyDown(e) { if (e.key === 'Escape') cleanup(false); }

        okBtn.addEventListener('click', onOk);
        cancelBtn.addEventListener('click', onCancel);
        backdrop.addEventListener('click', onBackdropClick);
        document.addEventListener('keydown', onKeyDown);

        okBtn.focus();
    });
}

/**
 * Shows a transient toast notification.
 * Falls back to window.alert() if the toast container isn't on the page.
 * @param {string} message
 * @param {'success'|'error'|'info'} [type]
 */
function upToast(message, type) {
    type = type || 'success';
    var container = document.getElementById('upToastContainer');

    if (!container) {
        alert(message);
        return;
    }

    var toast = document.createElement('div');
    toast.className = 'up-toast up-toast--' + type;
    toast.setAttribute('role', type === 'error' ? 'alert' : 'status');
    toast.textContent = message;
    container.appendChild(toast);

    requestAnimationFrame(function () {
        toast.classList.add('up-toast--visible');
    });

    setTimeout(function () {
        toast.classList.remove('up-toast--visible');
        setTimeout(function () {
            toast.remove();
        }, 250);
    }, 4000);
}
