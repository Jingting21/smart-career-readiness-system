(() => {
    const root = document.documentElement;
    const toggle = document.querySelector("[data-sidebar-toggle]");
    const closeButton = document.querySelector("[data-sidebar-close]");
    const closeSidebar = () => { root.classList.remove("sidebar-open"); toggle?.setAttribute("aria-expanded", "false"); };
    toggle?.addEventListener("click", () => { const open = root.classList.toggle("sidebar-open"); toggle.setAttribute("aria-expanded", String(open)); });
    closeButton?.addEventListener("click", closeSidebar);
    document.querySelectorAll(".app-sidebar a").forEach((link) => link.addEventListener("click", closeSidebar));
    document.addEventListener("keydown", (event) => { if (event.key === "Escape") closeSidebar(); });
})();
