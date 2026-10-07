(function () {
    'use strict';

    function updateUnreadCount() {
        var count = document.querySelectorAll('.notification-item[data-is-read="false"]').length;
        var badge = document.getElementById('notification-unread-badge');
        var summary = document.getElementById('notification-unread-summary');
        if (badge) {
            badge.textContent = count > 0 ? String(count) : '';
            badge.classList.toggle('d-none', count === 0);
        }
        if (summary) summary.textContent = count + ' Unread';
    }

    function openLocalTarget(value) {
        if (!value) return;
        var target = new URL(value, window.location.href);
        if (target.origin === window.location.origin) window.location.assign(target.href);
    }

    // The theme stops bubbling inside dropdown menus; handle these clicks first.
    document.addEventListener('click', async function (event) {
        var item = event.target.closest('.notification-item');
        if (!item) return;
        event.preventDefault();
        event.stopPropagation();
        if (item.dataset.reading === 'true') return;
        if (item.dataset.isRead === 'true') {
            openLocalTarget(item.dataset.returnUrl);
            return;
        }

        var error = item.querySelector('.notification-read-error');
        item.dataset.reading = 'true';
        item.setAttribute('aria-busy', 'true');
        if (error) error.classList.add('d-none');
        try {
            var response = await fetch(item.dataset.readUrl, {
                credentials: 'same-origin',
                headers: { 'X-Requested-With': 'XMLHttpRequest', 'Accept': 'application/json' }
            });
            if (!response.ok) throw new Error('Read failed');
            var result = await response.json();
            if ((result.isRead ?? result.IsRead) !== true) throw new Error('Read was not confirmed');

            item.dataset.isRead = 'true';
            item.classList.remove('is-unread');
            item.classList.add('is-read');
            item.querySelector('.notification-read-status').textContent = 'Read';
            var avatar = item.querySelector('.avatar');
            if (avatar) {
                avatar.classList.remove('bg-light-warning');
                avatar.classList.add('bg-light-success');
                avatar.querySelector('.avatar-content').innerHTML = '<svg xmlns="http://www.w3.org/2000/svg" width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2"><polyline points="20 6 9 17 4 12"></polyline></svg>';
            }
            updateUnreadCount();
            openLocalTarget(result.returnUrl ?? result.ReturnUrl);
        } catch (_) {
            if (error) {
                error.textContent = 'Unable to mark as read. Please try again.';
                error.classList.remove('d-none');
            }
        } finally {
            delete item.dataset.reading;
            item.removeAttribute('aria-busy');
        }
    }, true);
})();
