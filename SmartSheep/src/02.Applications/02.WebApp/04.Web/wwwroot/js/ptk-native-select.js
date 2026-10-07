(function () {
    'use strict';

    function closeAll(except) {
        document.querySelectorAll('.ptk-select.is-open').forEach(function (widget) {
            if (widget !== except) {
                widget.classList.remove('is-open');
                widget.querySelector('.ptk-select-trigger').setAttribute('aria-expanded', 'false');
            }
        });
    }

    function enhance(select) {
        if (!select || select.dataset.ptkSelectReady === 'true' || select.multiple || select.size > 1) return;
        if (select.classList.contains('aims-native-select-hidden') || select.closest('.select2-container')) return;

        select.dataset.ptkSelectReady = 'true';
        select.classList.add('ptk-native-select-source');

        var widget = document.createElement('div');
        widget.className = 'ptk-select';

        var trigger = document.createElement('button');
        trigger.type = 'button';
        trigger.className = 'ptk-select-trigger';
        trigger.setAttribute('aria-haspopup', 'listbox');
        trigger.setAttribute('aria-expanded', 'false');

        var value = document.createElement('span');
        value.className = 'ptk-select-value';
        var arrow = document.createElement('span');
        arrow.className = 'ptk-select-arrow';
        arrow.setAttribute('aria-hidden', 'true');
        trigger.append(value, arrow);

        var menu = document.createElement('div');
        menu.className = 'ptk-select-menu';
        menu.setAttribute('role', 'listbox');
        widget.append(trigger, menu);
        select.insertAdjacentElement('afterend', widget);

        function render() {
            value.textContent = select.selectedOptions[0] ? select.selectedOptions[0].textContent.trim() : 'Select a value...';
            menu.innerHTML = '';
            Array.from(select.options).forEach(function (option) {
                var item = document.createElement('button');
                item.type = 'button';
                item.className = 'ptk-select-option' + (option.selected ? ' is-selected' : '');
                item.textContent = option.textContent;
                item.dataset.value = option.value;
                item.disabled = option.disabled;
                item.setAttribute('role', 'option');
                item.setAttribute('aria-selected', option.selected ? 'true' : 'false');
                item.addEventListener('click', function () {
                    select.value = option.value;
                    select.dispatchEvent(new Event('change', { bubbles: true }));
                    render();
                    widget.classList.remove('is-open');
                    trigger.setAttribute('aria-expanded', 'false');
                    trigger.focus();
                });
                menu.appendChild(item);
            });
            trigger.disabled = select.disabled;
        }

        trigger.addEventListener('click', function () {
            var opening = !widget.classList.contains('is-open');
            closeAll(widget);
            widget.classList.toggle('is-open', opening);
            trigger.setAttribute('aria-expanded', opening ? 'true' : 'false');
        });

        trigger.addEventListener('keydown', function (event) {
            if (event.key === 'Escape') {
                widget.classList.remove('is-open');
                trigger.setAttribute('aria-expanded', 'false');
            }
        });

        select.addEventListener('change', render);
        if (select.form) select.form.addEventListener('reset', function () { window.setTimeout(render, 0); });
        render();
    }

    function enhanceAll(root) {
        (root || document).querySelectorAll('select.form-select, select.custom-select').forEach(enhance);
    }

    document.addEventListener('click', function (event) {
        if (!event.target.closest('.ptk-select')) closeAll();
    });

    document.addEventListener('DOMContentLoaded', function () {
        enhanceAll(document);
        new MutationObserver(function (mutations) {
            mutations.forEach(function (mutation) {
                mutation.addedNodes.forEach(function (node) {
                    if (node.nodeType === 1) {
                        if (node.matches && node.matches('select.form-select, select.custom-select')) enhance(node);
                        enhanceAll(node);
                    }
                });
            });
        }).observe(document.body, { childList: true, subtree: true });
    });
})();
