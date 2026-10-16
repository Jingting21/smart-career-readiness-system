document.addEventListener("click", (event) => {
    const button = event.target.closest("[data-toggle-password]");
    if (!button) return;

    const input = document.getElementById(button.dataset.togglePassword);
    if (!input) return;

    const showing = input.type === "text";
    input.type = showing ? "password" : "text";
    button.textContent = showing ? "Show" : "Hide";
    button.setAttribute("aria-pressed", String(!showing));
    const fieldName = button.dataset.passwordLabel || "password";
    button.setAttribute("aria-label", `${showing ? "Show" : "Hide"} ${fieldName}`);
});
