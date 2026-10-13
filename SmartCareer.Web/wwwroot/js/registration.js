(() => {
    const debounce = (callback, delay) => {
        let timer;
        return (...args) => {
            clearTimeout(timer);
            timer = setTimeout(() => callback(...args), delay);
        };
    };

    document.querySelectorAll("[data-autocomplete-url]").forEach((input) => {
        const list = document.getElementById(input.getAttribute("aria-controls"));
        let items = [];
        let activeIndex = -1;
        let searchVersion = 0;

        const close = () => {
            items = [];
            activeIndex = -1;
            list.replaceChildren();
            input.setAttribute("aria-expanded", "false");
            input.removeAttribute("aria-activedescendant");
        };

        const select = (index) => {
            if (!items[index]) return;
            input.value = items[index].value;
            input.dispatchEvent(new Event("change", { bubbles: true }));
            close();
            input.focus();
        };

        const render = (suggestions) => {
            close();
            items = suggestions;
            suggestions.forEach((suggestion, index) => {
                const option = document.createElement("button");
                option.type = "button";
                option.id = `${list.id}-option-${index}`;
                option.className = "autocomplete-option";
                option.setAttribute("role", "option");
                option.textContent = suggestion.label;
                option.addEventListener("mousedown", (event) => event.preventDefault());
                option.addEventListener("click", () => select(index));
                list.appendChild(option);
            });
            input.setAttribute("aria-expanded", String(suggestions.length > 0));
        };

        const search = debounce(async () => {
            const version = searchVersion;
            const query = input.value.trim();
            if (query.length < 2) return close();
            try {
                const response = await fetch(`${input.dataset.autocompleteUrl}?q=${encodeURIComponent(query)}`, {
                    headers: { Accept: "application/json" }
                });
                if (response.ok) {
                    const suggestions = await response.json();
                    if (version === searchVersion) render(suggestions);
                }
            }
            catch
            {
                close();
            }
        }, 250);

        input.addEventListener("input", () => {
            searchVersion++;
            search();
        });
        input.addEventListener("keydown", (event) => {
            if (!items.length) return;
            if (event.key === "ArrowDown" || event.key === "ArrowUp") {
                event.preventDefault();
                activeIndex = event.key === "ArrowDown"
                    ? (activeIndex + 1) % items.length
                    : (activeIndex - 1 + items.length) % items.length;
                list.querySelectorAll("[role=option]").forEach((option, index) => {
                    option.setAttribute("aria-selected", String(index === activeIndex));
                });
                input.setAttribute("aria-activedescendant", `${list.id}-option-${activeIndex}`);
            } else if (event.key === "Enter" && activeIndex >= 0) {
                event.preventDefault();
                select(activeIndex);
            } else if (event.key === "Escape") {
                close();
            }
        });
        input.addEventListener("blur", () => setTimeout(close, 100));
    });

    const password = document.querySelector("[data-password-primary]");
    const confirmation = document.querySelector("[data-password-confirm]");
    if (password) {
        const rules = {
            length: (value) => value.length >= 8,
            upper: (value) => /[A-Z]/.test(value),
            lower: (value) => /[a-z]/.test(value),
            digit: (value) => /\d/.test(value),
            symbol: (value) => /[^A-Za-z0-9]/.test(value)
        };
        const update = () => {
            const passed = Object.entries(rules).filter(([name, check]) => {
                const valid = check(password.value);
                document.querySelector(`[data-password-rule=${name}]`)?.classList.toggle("is-valid", valid);
                return valid;
            }).length;
            const strength = document.querySelector("[data-password-strength]");
            strength.textContent = `Strength: ${passed < 3 ? "weak" : passed < 5 ? "moderate" : "strong"}`;
            const match = document.querySelector("[data-password-match]");
            if (confirmation?.value) {
                const matches = password.value === confirmation.value;
                match.textContent = matches ? "Passwords match." : "Passwords do not match.";
                match.className = `small mt-1 ${matches ? "text-success" : "text-danger"}`;
            } else {
                match.textContent = "";
            }
        };
        password.addEventListener("input", update);
        confirmation?.addEventListener("input", update);
    }
})();

document.addEventListener("click", (event) => {
    const button = event.target.closest("[data-toggle-password]");
    if (!button) return;
    const input = document.getElementById(button.dataset.togglePassword);
    if (!input) return;
    const showing = input.type === "text";
    input.type = showing ? "password" : "text";
    button.textContent = showing ? "Show" : "Hide";
});
