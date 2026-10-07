(function () {
    "use strict";

    const initReportSearchableSelects = function () {
        const selects = Array.from(document.querySelectorAll('.report-filter-toolbar select[data-aims-searchable="true"]'));
        const closeAll = function (except) {
            document.querySelectorAll('.report-filter-toolbar .aims-search-select.is-open').forEach(item => {
                if (item !== except) item.classList.remove('is-open');
            });
        };

        selects.forEach(select => {
            if (select.dataset.aimsSearchReady === 'true') return;
            select.dataset.aimsSearchReady = 'true';
            select.classList.add('native-select-hidden');

            const wrapper = document.createElement('div');
            wrapper.className = 'aims-search-select';

            const input = document.createElement('input');
            input.type = 'text';
            input.className = 'aims-search-select-input';
            input.autocomplete = 'off';
            input.spellcheck = false;
            input.setAttribute('aria-label', select.closest('.aims-filter')?.querySelector('label')?.textContent || 'Filter');

            const menu = document.createElement('div');
            menu.className = 'aims-search-select-menu';

            const getSelectedText = function () {
                const selected = select.options[select.selectedIndex];
                return selected ? selected.text : '';
            };

            const renderOptions = function (filter) {
                const term = (filter || '').trim().toLowerCase();
                menu.innerHTML = '';
                const options = Array.from(select.options).filter(option => !option.hidden && option.text.toLowerCase().includes(term));

                if (!options.length) {
                    const empty = document.createElement('div');
                    empty.className = 'aims-search-select-empty';
                    empty.textContent = 'No data found';
                    menu.appendChild(empty);
                    return;
                }

                options.forEach(option => {
                    const item = document.createElement('div');
                    item.className = 'aims-search-select-option';
                    if (option.value === select.value) item.classList.add('is-active');
                    item.textContent = option.text;
                    item.addEventListener('mousedown', event => {
                        event.preventDefault();
                        select.value = option.value;
                        input.value = option.text;
                        wrapper.classList.remove('is-open');
                        select.dispatchEvent(new Event('change', { bubbles: true }));
                    });
                    menu.appendChild(item);
                });
            };

            const openMenu = function () {
                closeAll(wrapper);
                wrapper.classList.add('is-open');
                input.select();
                renderOptions(input.value === getSelectedText() ? '' : input.value);
            };

            input.value = getSelectedText();
            input.addEventListener('focus', openMenu);
            input.addEventListener('click', openMenu);
            input.addEventListener('input', () => {
                wrapper.classList.add('is-open');
                renderOptions(input.value);
            });
            input.addEventListener('keydown', event => {
                if (event.key === 'Escape') {
                    input.value = getSelectedText();
                    wrapper.classList.remove('is-open');
                    input.blur();
                }
            });
            input.addEventListener('blur', () => {
                window.setTimeout(() => {
                    if (!wrapper.classList.contains('is-open')) return;
                    input.value = getSelectedText();
                    wrapper.classList.remove('is-open');
                }, 120);
            });
            select.addEventListener('change', () => {
                input.value = getSelectedText();
                renderOptions('');
            });

            wrapper.appendChild(input);
            wrapper.appendChild(menu);
            select.insertAdjacentElement('afterend', wrapper);
            renderOptions('');
        });

        document.addEventListener('mousedown', event => {
            if (!event.target.closest('.aims-search-select')) closeAll(null);
        });
    };

    function initializeReportFilters() {
        initReportSearchableSelects();
        document.querySelectorAll(".report-filter-toolbar").forEach(function (toolbar) {
            var farm = toolbar.querySelector(".report-farm");
            var barn = toolbar.querySelector(".report-barn");
            if (!farm || !barn || toolbar.dataset.reportFilterReady === "true") return;

            toolbar.dataset.reportFilterReady = "true";
            var barnOptions = Array.prototype.slice.call(barn.options, 1);

            function filterBarns() {
                var farmId = (farm.value || "").toLowerCase();
                barnOptions.forEach(function (option) {
                    option.hidden = farmId !== "" && (option.dataset.farmId || "").toLowerCase() !== farmId;
                });
                if (barn.selectedOptions.length && barn.selectedOptions[0].hidden) {
                    barn.value = "";
                    barn.dispatchEvent(new Event("change", { bubbles: true }));
                }
            }

            farm.addEventListener("change", filterBarns);
            filterBarns();
            var start = toolbar.querySelector('[name="from"]');
            var end = toolbar.querySelector('[name="to"]');
            if (start && end && typeof flatpickr === "function") {
                var options = { dateFormat: "Y-m-d", allowInput: true, disableMobile: true };
                var endPicker = end._flatpickr || flatpickr(end, options);
                var startPicker = start._flatpickr || flatpickr(start, options);
                startPicker.set("allowInput", true);
                endPicker.set("allowInput", true);
                function syncDates() {
                    endPicker.set("minDate", startPicker.selectedDates[0] || null);
                }
                startPicker.config.onChange.push(syncDates);
                syncDates();
            }
        });
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", initializeReportFilters);
    } else {
        initializeReportFilters();
    }
})();
