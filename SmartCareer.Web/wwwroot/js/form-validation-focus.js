(() => {
    const isVisible = element => !!(element && !element.hidden && element.getClientRects().length);
    const fieldForMessage = (form, message) => {
        const name = message?.dataset.valmsgFor;
        if (!name) return null;
        return Array.from(form.elements).find(element => element.name === name) || null;
    };
    const visibleField = field => {
        if (isVisible(field) && field.type !== "hidden") return field;
        const group = field?.closest("[data-controlled-selection]");
        if (!group) return null;
        const mode = group.querySelector("[data-selection-mode]")?.value;
        return group.querySelector(mode === "Other" ? "[data-custom-field] input" : "[data-catalog-field] input");
    };
    const firstInvalidField = form => {
        const clientInvalid = Array.from(form.querySelectorAll(".input-validation-error, [aria-invalid='true']"))
            .map(visibleField).find(isVisible);
        if (clientInvalid) return clientInvalid;
        return Array.from(form.querySelectorAll("[data-valmsg-for].field-validation-error"))
            .map(message => visibleField(fieldForMessage(form, message))).find(isVisible) || null;
    };
    const focusFirstInvalid = form => {
        const field = firstInvalidField(form);
        if (!field) return;
        const behavior = window.matchMedia?.("(prefers-reduced-motion: reduce)").matches ? "auto" : "smooth";
        field.scrollIntoView({ behavior, block: "center" });
        field.focus({ preventScroll: true });
    };

    document.querySelectorAll("form[data-validation-focus]").forEach(form => {
        form.querySelectorAll("[data-valmsg-for]").forEach((message, index) => {
            const field = fieldForMessage(form, message);
            if (!field) return;
            if (!message.id) message.id = `${form.id || "validated-form"}-error-${index}`;
            const describedBy = new Set((field.getAttribute("aria-describedby") || "").split(/\s+/).filter(Boolean));
            describedBy.add(message.id);
            field.setAttribute("aria-describedby", Array.from(describedBy).join(" "));
        });
        const scheduleFocus = () => window.setTimeout(() => focusFirstInvalid(form), 0);
        form.addEventListener("submit", scheduleFocus);
        form.addEventListener("invalid", scheduleFocus, true);

        const validator = window.jQuery?.(form).data("validator");
        if (validator) {
            const existing = validator.settings.invalidHandler;
            validator.settings.invalidHandler = function (event, instance) {
                if (typeof existing === "function") existing.call(this, event, instance);
                focusFirstInvalid(form);
            };
        }

        if (form.dataset.serverValidationErrors === "true") {
            window.requestAnimationFrame(() => focusFirstInvalid(form));
        }
    });
})();
