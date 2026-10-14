(() => {
    const page = document.querySelector("[data-profile-page]"); if (!page) return;
    const form = page.querySelector("form"); const fields = [...page.querySelectorAll("[data-profile-field]")]; const controls = [...page.querySelectorAll("[data-profile-control]")]; const editButton = page.querySelector("[data-profile-edit]"); const actions = page.querySelector("[data-profile-actions]");
    const setEditing = (editing) => { page.classList.toggle("is-editing", editing); [...fields, ...controls].forEach((field) => { field.disabled = !editing; }); editButton.hidden = editing; actions.hidden = !editing; page.dispatchEvent(new CustomEvent("profile-edit-state", { detail: { editing } })); if (editing && page.dataset.editing !== "true") fields.find(field => field.type !== "hidden")?.focus(); };
    editButton?.addEventListener("click", () => setEditing(true)); page.querySelector("[data-profile-cancel]")?.addEventListener("click", () => window.location.assign(window.location.pathname));
    form?.addEventListener("submit", () => { fields.forEach((field) => { field.disabled = false; }); });
    setEditing(page.dataset.editing === "true");
})();
