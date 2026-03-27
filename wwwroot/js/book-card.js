(function () {
    const currencyFormatter = new Intl.NumberFormat("vi-VN");

    function updateMiniCart(data) {
        const badge = document.getElementById("cartCount");
        if (badge && typeof data.count !== "undefined") {
            badge.textContent = String(data.count);
        }

        const totalEl = document.getElementById("cartTotal");
        if (totalEl && typeof data.total !== "undefined") {
            totalEl.textContent = currencyFormatter.format(data.total) + "₫";
        }
    }

    function markButtonAdded(button) {
        if (!button) {
            return;
        }

        button.classList.add("is-added");
        const icon = button.querySelector("i");
        if (icon) {
            icon.className = "fas fa-check";
        }

        window.setTimeout(function () {
            button.classList.remove("is-added");
            const revertIcon = button.querySelector("i");
            if (revertIcon) {
                revertIcon.className = "fas fa-shopping-cart";
            }
        }, 1000);
    }

    async function add(bookId, qty, button) {
        if (!bookId || Number.isNaN(bookId)) {
            return false;
        }

        const quantity = !qty || Number.isNaN(qty) || qty < 1 ? 1 : qty;

        if (button) {
            button.disabled = true;
        }

        try {
            const response = await fetch("/Cart/AddToCart?id=" + bookId + "&qty=" + quantity);
            if (!response.ok) {
                throw new Error("Cannot add to cart");
            }

            const data = await response.json();
            updateMiniCart(data);
            markButtonAdded(button);
            return true;
        } catch (error) {
            console.error(error);
            return false;
        } finally {
            if (button) {
                window.setTimeout(function () {
                    button.disabled = false;
                }, 200);
            }
        }
    }

    function handleAddToCart(event) {
        const button = event.target.closest(".js-add-to-cart");
        if (!button) {
            return false;
        }

        event.preventDefault();
        event.stopPropagation();

        if (button.disabled) {
            return true;
        }

        const bookId = parseInt(button.dataset.bookId || "0", 10);
        const qty = parseInt(button.dataset.qty || "1", 10);
        add(bookId, qty, button);
        return true;
    }

    function handleCarouselNav(event) {
        const nav = event.target.closest(".js-carousel-nav");
        if (!nav) {
            return false;
        }

        event.preventDefault();

        const targetId = nav.dataset.target;
        const track = targetId ? document.getElementById(targetId) : null;
        if (!track) {
            return true;
        }

        const direction = nav.dataset.direction === "prev" ? -1 : 1;
        const step = Math.max(track.clientWidth * 0.8, 220);
        track.scrollBy({ left: direction * step, behavior: "smooth" });
        return true;
    }

    document.addEventListener("click", function (event) {
        if (handleAddToCart(event)) {
            return;
        }

        handleCarouselNav(event);
    });

    window.bookStoreCart = {
        add: add
    };
})();
