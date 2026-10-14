(() => {
    const password = document.querySelector("[data-password-primary]"); const confirmation = document.querySelector("[data-password-confirm]");
    if (password) {
        const rules = { length: (value) => value.length >= 8, upper: (value) => /[A-Z]/.test(value), lower: (value) => /[a-z]/.test(value), digit: (value) => /\d/.test(value), symbol: (value) => /[^A-Za-z0-9]/.test(value) };
        const update = () => {
            const passed = Object.entries(rules).filter(([name, check]) => { const valid = check(password.value); document.querySelector(`[data-password-rule=${name}]`)?.classList.toggle("is-valid", valid); return valid; }).length;
            const strength = document.querySelector("[data-password-strength]"); const level = passed < 3 ? "weak" : passed < 5 ? "moderate" : "strong"; strength.textContent = `Strength: ${level}`;
            const bar = document.querySelector("[data-strength-bar]"); if (bar) bar.className = password.value ? level : "";
            const match = document.querySelector("[data-password-match]"); if (confirmation?.value) { const matches = password.value === confirmation.value; match.textContent = matches ? "Passwords match." : "Passwords do not match."; match.className = `small mt-1 ${matches ? "text-success" : "text-danger"}`; } else { match.textContent = ""; }
        };
        password.addEventListener("input", () => { document.querySelector("[data-password-reentry-notice]")?.remove(); update(); }); confirmation?.addEventListener("input", update);
    }
})();
document.addEventListener("click", (event) => { const button = event.target.closest("[data-toggle-password]"); if (!button) return; const input = document.getElementById(button.dataset.togglePassword); if (!input) return; const showing = input.type === "text"; input.type = showing ? "password" : "text"; button.textContent = showing ? "Show" : "Hide"; button.setAttribute("aria-pressed", String(!showing)); });
