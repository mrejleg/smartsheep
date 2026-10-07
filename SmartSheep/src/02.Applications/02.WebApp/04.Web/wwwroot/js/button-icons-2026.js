(function () {
    "use strict";

    var icons = {
        "archive": '<path d="M4 7h16"></path><path d="M6 7l1 13h10l1-13"></path><path d="M9 7V4h6v3"></path><path d="M10 11v5"></path><path d="M14 11v5"></path>',
        "arrow-right": '<path d="M5 12h14"></path><path d="m13 6 6 6-6 6"></path>',
        "bar-chart-2": '<path d="M4 20V10"></path><path d="M10 20V4"></path><path d="M16 20v-7"></path><path d="M22 20H2"></path>',
        "check": '<path d="m5 12 4 4L19 6"></path>',
        "check-square": '<rect x="3" y="3" width="18" height="18" rx="4"></rect><path d="m7.5 12 3 3 6-7"></path>',
        "clock": '<circle cx="12" cy="12" r="9"></circle><path d="M12 7v5l3 2"></path>',
        "download": '<path d="M12 3v11"></path><path d="m7.5 10 4.5 4.5 4.5-4.5"></path><path d="M5 20h14"></path>',
        "edit": '<path d="M4 20h4l11-11a2.8 2.8 0 0 0-4-4L4 16v4Z"></path><path d="m13.5 6.5 4 4"></path>',
        "file": '<path d="M6 3h8l4 4v14H6z"></path><path d="M14 3v5h5"></path><path d="M9 13h6"></path><path d="M9 17h4"></path>',
        "git-branch": '<circle cx="6" cy="5" r="2"></circle><circle cx="18" cy="7" r="2"></circle><circle cx="6" cy="19" r="2"></circle><path d="M6 7v10"></path><path d="M8 7h6a4 4 0 0 1 4 4v4"></path>',
        "list": '<path d="M9 6h11"></path><path d="M9 12h11"></path><path d="M9 18h11"></path><circle cx="4" cy="6" r="1"></circle><circle cx="4" cy="12" r="1"></circle><circle cx="4" cy="18" r="1"></circle>',
        "plus": '<path d="M12 5v14"></path><path d="M5 12h14"></path>',
        "power": '<path d="M12 2v10"></path><path d="M6.3 5.7a8 8 0 1 0 11.4 0"></path>',
        "refresh-cw": '<path d="M20 7v5h-5"></path><path d="M4 17v-5h5"></path><path d="M6.1 8a7 7 0 0 1 11.8-1L20 12"></path><path d="m4 12 2.1 5a7 7 0 0 0 11.8-1"></path>',
        "repeat": '<path d="m17 2 4 4-4 4"></path><path d="M3 11V9a3 3 0 0 1 3-3h15"></path><path d="m7 22-4-4 4-4"></path><path d="M21 13v2a3 3 0 0 1-3 3H3"></path>',
        "search": '<circle cx="11" cy="11" r="6.5"></circle><path d="m16 16 4.5 4.5"></path>',
        "send": '<path d="m3 11 18-8-8 18-2.5-7.5L3 11Z"></path><path d="m10.5 13.5 5-5"></path>',
        "settings": '<circle cx="12" cy="12" r="3"></circle><path d="M19.4 15a1.7 1.7 0 0 0 .3 1.9l.1.1-2.8 2.8-.1-.1a1.7 1.7 0 0 0-1.9-.3 1.7 1.7 0 0 0-1 1.6v.2h-4V21a1.7 1.7 0 0 0-1-1.6 1.7 1.7 0 0 0-1.9.3l-.1.1L4.2 17l.1-.1a1.7 1.7 0 0 0 .3-1.9A1.7 1.7 0 0 0 3 14H2.8v-4H3a1.7 1.7 0 0 0 1.6-1 1.7 1.7 0 0 0-.3-1.9L4.2 7 7 4.2l.1.1A1.7 1.7 0 0 0 9 4.6 1.7 1.7 0 0 0 10 3V2.8h4V3a1.7 1.7 0 0 0 1 1.6 1.7 1.7 0 0 0 1.9-.3l.1-.1L19.8 7l-.1.1a1.7 1.7 0 0 0-.3 1.9 1.7 1.7 0 0 0 1.6 1h.2v4H21a1.7 1.7 0 0 0-1.6 1Z"></path>',
        "share": '<circle cx="18" cy="5" r="2.5"></circle><circle cx="6" cy="12" r="2.5"></circle><circle cx="18" cy="19" r="2.5"></circle><path d="m8.2 10.8 7.6-4.5"></path><path d="m8.2 13.2 7.6 4.5"></path>',
        "slash": '<circle cx="12" cy="12" r="9"></circle><path d="m5.6 5.6 12.8 12.8"></path>',
        "upload": '<path d="M12 21V10"></path><path d="m7.5 14.5 4.5-4.5 4.5 4.5"></path><path d="M5 4h14"></path>',
        "video": '<path d="m23 7-7 5 7 5V7Z"></path><rect x="1" y="5" width="15" height="14" rx="2" ry="2"></rect>',
        "x": '<path d="m6 6 12 12"></path><path d="m18 6-12 12"></path>'
    };

    function iconName(svg) {
        var classes = Array.prototype.slice.call(svg.classList);
        var featherClass = classes.find(function (name) {
            return name.indexOf("feather-") === 0 && name !== "feather-icons" && name !== "feather-icon";
        });
        return featherClass ? featherClass.substring(8) : "";
    }

    function replaceIcon(svg) {
        if (!svg || svg.dataset.ptkButtonIcon === "true" || !svg.closest(".btn")) return;

        var name = iconName(svg);
        if (!icons[name]) return;

        svg.dataset.ptkButtonIcon = "true";
        svg.setAttribute("viewBox", "0 0 24 24");
        svg.setAttribute("fill", "none");
        svg.setAttribute("stroke", "currentColor");
        svg.setAttribute("stroke-width", "1.8");
        svg.setAttribute("stroke-linecap", "round");
        svg.setAttribute("stroke-linejoin", "round");
        svg.innerHTML = icons[name];
    }

    function refresh(root) {
        if (!root || (root.nodeType !== 1 && root.nodeType !== 9)) return;
        if (root.nodeType === 1 && root.matches(".btn svg[class*='feather-']")) replaceIcon(root);
        root.querySelectorAll(".btn svg[class*='feather-']").forEach(replaceIcon);
    }

    function initialize() {
        refresh(document);
        new MutationObserver(function (mutations) {
            mutations.forEach(function (mutation) {
                mutation.addedNodes.forEach(refresh);
            });
        }).observe(document.body, { childList: true, subtree: true });
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", initialize);
    } else {
        initialize();
    }
})();
