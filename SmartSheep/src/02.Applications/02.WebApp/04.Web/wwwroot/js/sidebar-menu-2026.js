(function (window, document, $) {
    "use strict";

    if (!$) return;

    var tooltip;
    var activeAnchor;

    function isCollapsed() {
        return document.body.classList.contains("menu-collapsed");
    }

    function getTitle(anchor) {
        var title = anchor.querySelector(".menu-title");
        return title ? title.textContent.trim() : "";
    }

    function ensureTooltip() {
        if (tooltip) return tooltip;

        tooltip = document.createElement("div");
        tooltip.className = "ptk-sidebar-tooltip";
        tooltip.setAttribute("role", "tooltip");
        tooltip.setAttribute("aria-hidden", "true");
        document.body.appendChild(tooltip);
        return tooltip;
    }

    function hideTooltip() {
        if (!tooltip) return;
        tooltip.classList.remove("is-visible");
        tooltip.setAttribute("aria-hidden", "true");
        activeAnchor = null;
    }

    function showTooltip(anchor) {
        if (!isCollapsed()) {
            hideTooltip();
            return;
        }

        var title = anchor.dataset.sidebarTitle || getTitle(anchor);
        if (!title) return;

        var element = ensureTooltip();
        var bounds = anchor.getBoundingClientRect();
        element.textContent = title;
        element.style.left = Math.round(bounds.right + 10) + "px";
        element.style.top = Math.round(bounds.top + (bounds.height / 2)) + "px";
        element.setAttribute("aria-hidden", "false");
        element.classList.add("is-visible");
        activeAnchor = anchor;
    }

    function prepareMenuTitles() {
        document.querySelectorAll("#main-menu-navigation > li > a").forEach(function (anchor) {
            var title = getTitle(anchor);
            if (title) {
                anchor.dataset.sidebarTitle = title;
                anchor.setAttribute("aria-label", title);
            }
        });
    }

    function getTransitionTime(element) {
        if (!element) return 0;

        var style = window.getComputedStyle(element);
        var durations = style.transitionDuration.split(",");
        var delays = style.transitionDelay.split(",");
        var longest = 0;

        function toMilliseconds(value) {
            value = value.trim();
            return parseFloat(value) * (value.indexOf("ms") > -1 ? 1 : 1000) || 0;
        }

        durations.forEach(function (duration, index) {
            var delay = delays[index] || delays[delays.length - 1] || "0s";
            longest = Math.max(longest, toMilliseconds(duration) + toMilliseconds(delay));
        });

        return longest;
    }

    function openSubmenu(item) {
        item.siblings(".open").trigger("close.app.menu");
        if (!item.hasClass("open")) item.trigger("open.app.menu");
        item.addClass("sidebar-group-active");
        item.children("a").attr("aria-expanded", "true");
    }

    function openWhenSidebarReady(item) {
        var menu = document.querySelector(".main-menu");

        window.requestAnimationFrame(function () {
            var duration = getTransitionTime(menu);

            // The current PTK layout intentionally expands instantly. Two frames
            // let its width and menu labels settle before measuring the submenu.
            if (duration <= 16) {
                window.requestAnimationFrame(function () {
                    openSubmenu(item);
                });
                return;
            }

            var finished = false;
            var timeout;
            var finish = function () {
                if (finished) return;
                finished = true;
                menu.removeEventListener("transitionend", onTransitionEnd);
                window.clearTimeout(timeout);
                openSubmenu(item);
            };
            var onTransitionEnd = function (transitionEvent) {
                if (transitionEvent.target === menu) finish();
            };

            menu.addEventListener("transitionend", onTransitionEnd);
            timeout = window.setTimeout(finish, duration + 60);
        });
    }

    function expandMenuForSubmenu(event) {
        var anchor = event.target.closest("#main-menu-navigation > li.has-sub > a");
        if (!anchor || !isCollapsed()) return;

        event.preventDefault();
        event.stopPropagation();
        event.stopImmediatePropagation();
        hideTooltip();

        var item = $(anchor).parent("li.has-sub");
        var toggle = $(".menu-toggle:visible, .modern-nav-toggle:visible").first();

        if (toggle.length) {
            toggle.trigger("click");
        } else if ($.app && $.app.menu) {
            $.app.menu.toggle();
            $(window).trigger("resize");
        }

        openWhenSidebarReady(item);
    }

    $(function () {
        prepareMenuTitles();

        $(document)
            .on("mouseenter.ptkSidebarTitle", "#main-menu-navigation > li > a", function () {
                showTooltip(this);
            })
            .on("mouseleave.ptkSidebarTitle", "#main-menu-navigation > li > a", hideTooltip)
            .on("click.ptkSidebarTitle", ".menu-toggle, .modern-nav-toggle", hideTooltip);

        document.addEventListener("click", expandMenuForSubmenu, true);
        window.addEventListener("resize", hideTooltip, { passive: true });
        window.addEventListener("scroll", hideTooltip, { passive: true, capture: true });
    });
})(window, document, window.jQuery);
