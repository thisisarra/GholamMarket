document.querySelectorAll("form[method='post']").forEach((form) => {
    form.addEventListener("submit", () => {
        const button = form.querySelector("button[type='submit']");
        if (!button || button.dataset.loading === "off" || button.disabled) return;
        button.disabled = true;
        button.insertAdjacentHTML("afterbegin", "<span class='spinner-border spinner-border-sm ms-2' aria-hidden='true'></span>");
    });
});

document.querySelectorAll("[data-product-image]").forEach((image) => {
    image.addEventListener("error", () => {
        const fallback = document.createElement("span");
        fallback.className = "media-fallback";
        fallback.innerHTML = "<i class='bi bi-image' aria-hidden='true'></i>";
        image.replaceWith(fallback);
    });
});
