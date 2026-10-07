(function (window, document) {
    "use strict";

    var loaderElement;
    var hideTimer;
    var startupTimer;
    var navigationPending = false;

    function getLoader() {
        if (!loaderElement) {
            loaderElement = document.getElementById("loader");
        }

        return loaderElement;
    }

    function showPageLoader() {
        var loader = getLoader();
        if (!loader) {
            return;
        }

        window.clearTimeout(hideTimer);
        window.clearTimeout(startupTimer);
        navigationPending = true;

        loader.style.display = "flex";
        loader.classList.remove("is-hidden");
        loader.setAttribute("aria-hidden", "false");
        document.documentElement.classList.add("loading");

        if (document.body) {
            document.body.classList.add("aims-loader-lock");
            document.body.setAttribute("aria-busy", "true");
        }
    }

    function hidePageLoader() {
        var loader = getLoader();
        if (!loader) {
            return;
        }

        navigationPending = false;
        loader.classList.add("is-hidden");
        loader.setAttribute("aria-hidden", "true");
        document.documentElement.classList.remove("loading");

        if (document.body) {
            document.body.classList.remove("aims-loader-lock");
            document.body.removeAttribute("aria-busy");
        }

        window.clearTimeout(hideTimer);
        hideTimer = window.setTimeout(function () {
            if (!navigationPending && loader.classList.contains("is-hidden")) {
                loader.style.display = "none";
            }
        }, 360);
    }

    function findClosest(element, selector) {
        return element && element.closest ? element.closest(selector) : null;
    }

    function waitsForConfirmation(form) {
        var handler = (form && form.getAttribute("onsubmit") || "").toLowerCase();

        return /\b(on(save|submit|upload|approve|reject|revise|cancel|complete)[a-z]*data)\s*\(/.test(handler) ||
            handler.indexOf("swal.fire") !== -1;
    }

    function isNavigationLink(event, anchor) {
        var rawHref;
        var target;
        var destination;

        if (!anchor || event.defaultPrevented || event.button !== 0) {
            return false;
        }

        if (event.metaKey || event.ctrlKey || event.shiftKey || event.altKey) {
            return false;
        }

        if (findClosest(anchor, "[data-no-page-loader]") ||
            anchor.matches(".logout-confirm") ||
            anchor.hasAttribute("download")) {
            return false;
        }

        if (anchor.matches("[data-bs-toggle], [data-toggle], .dropdown-toggle, .menu-toggle, .modern-nav-toggle")) {
            return false;
        }

        target = (anchor.getAttribute("target") || "").toLowerCase();
        if (target && target !== "_self") {
            return false;
        }

        rawHref = (anchor.getAttribute("href") || "").trim();
        if (!rawHref || rawHref === "#" || rawHref.charAt(0) === "#") {
            return false;
        }

        if (/^(javascript:|mailto:|tel:)/i.test(rawHref)) {
            return false;
        }

        try {
            destination = new URL(anchor.href, window.location.href);
        } catch (error) {
            return false;
        }

        if (destination.href === window.location.href) {
            return false;
        }

        if (destination.origin === window.location.origin &&
            destination.pathname === window.location.pathname &&
            destination.search === window.location.search &&
            destination.hash) {
            return false;
        }

        return true;
    }

    document.addEventListener("click", function (event) {
        var anchor = findClosest(event.target, "a[href]");

        if (!isNavigationLink(event, anchor)) {
            return;
        }

        showPageLoader();

        window.queueMicrotask(function () {
            if (event.defaultPrevented) {
                hidePageLoader();
            }
        });
    }, true);

    document.addEventListener("submit", function (event) {
        var form = event.target;

        if (!(form instanceof window.HTMLFormElement) ||
            form.matches("[data-no-page-loader]") ||
            waitsForConfirmation(form)) {
            return;
        }

        showPageLoader();

        window.queueMicrotask(function () {
            if (event.defaultPrevented) {
                hidePageLoader();
            }
        });
    }, true);

    if (window.HTMLFormElement && window.HTMLFormElement.prototype.submit) {
        var nativeFormSubmit = window.HTMLFormElement.prototype.submit;

        window.HTMLFormElement.prototype.submit = function () {
            if (!this.matches("[data-no-page-loader]")) {
                showPageLoader();
            }

            return nativeFormSubmit.apply(this, arguments);
        };
    }

    window.addEventListener("beforeunload", showPageLoader);
    window.addEventListener("load", hidePageLoader);
    window.addEventListener("pageshow", hidePageLoader);

    startupTimer = window.setTimeout(hidePageLoader, 3000);

    window.PtkPageLoader = Object.freeze({
        show: showPageLoader,
        hide: hidePageLoader
    });

    document.addEventListener("ptk:page-loading", showPageLoader);
    document.addEventListener("ptk:page-loaded", hidePageLoader);
})(window, document);
