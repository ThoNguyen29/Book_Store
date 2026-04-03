(function () {
    const formatter = new Intl.NumberFormat("vi-VN");

    function toInt(value, fallback) {
        const parsed = Number.parseInt(value, 10);
        return Number.isFinite(parsed) ? parsed : fallback;
    }

    function toPositiveInt(value, fallback) {
        const parsed = toInt(value, fallback);
        return parsed > 0 ? parsed : fallback;
    }

    function toNumber(value, fallback) {
        const parsed = Number(value);
        return Number.isFinite(parsed) ? parsed : fallback;
    }

    function formatVnd(value) {
        return formatter.format(toNumber(value, 0)) + " ₫";
    }

    async function postJson(url) {
        try {
            const response = await fetch(url, {
                method: "POST",
                headers: {
                    "X-Requested-With": "XMLHttpRequest"
                }
            });

            if (response.redirected) {
                window.location.href = response.url;
                return { success: false, count: 0, total: 0, quantity: 0, removed: false, requiresLogin: true };
            }

            if (!response.ok) {
                throw new Error("Request failed");
            }

            const contentType = response.headers.get("content-type") || "";
            if (!contentType.includes("application/json")) {
                window.location.href = "/Account/Login";
                return { success: false, count: 0, total: 0, quantity: 0, removed: false, requiresLogin: true };
            }

            const data = await response.json();
            if (data && data.requiresLogin) {
                window.location.href = data.loginUrl || "/Account/Login";
            }

            return data;
        } catch {
            return { success: false, count: 0, total: 0, quantity: 0, removed: false };
        }
    }

    window.cartApi = window.cartApi || {};
    window.cartApi.isBlazorConnected = false;

    window.cartApi.markConnected = function () {
        window.cartApi.isBlazorConnected = true;
        document.documentElement.setAttribute("data-cart-blazor", "connected");
    };

    window.cartApi.setQuantity = function (id, quantity) {
        const safeQty = !quantity || quantity < 1 ? 1 : quantity;
        return postJson("/Cart/UpdateQuantityAjax?id=" + encodeURIComponent(id) + "&quantity=" + encodeURIComponent(safeQty));
    };

    window.cartApi.removeItem = function (id) {
        return postJson("/Cart/RemoveAjax?id=" + encodeURIComponent(id));
    };

    window.cartApi.syncMiniCart = function (count, total) {
        const badge = document.getElementById("cartCount");
        if (badge) {
            badge.textContent = String(count || 0);
        }

        const totalEl = document.getElementById("cartTotal");
        if (totalEl) {
            totalEl.textContent = formatter.format(total || 0) + "₫";
        }
    };

    function hasBlazorConnection() {
        return window.cartApi.isBlazorConnected === true;
    }

    function findRow(productId) {
        return document.querySelector('tr[data-product-id="' + productId + '"]');
    }

    function setRowBusy(row, busy) {
        if (!row) {
            return;
        }

        const controls = row.querySelectorAll(".js-cart-decrease, .js-cart-increase, .js-cart-remove, .cart-qty-input");
        controls.forEach(function (control) {
            control.disabled = busy;
        });
    }

    function updateLineTotal(row, quantity) {
        if (!row) {
            return;
        }

        const unitPrice = toNumber(row.getAttribute("data-unit-price"), 0);
        const lineTotalEl = row.querySelector(".js-cart-line-total");
        if (lineTotalEl) {
            lineTotalEl.textContent = formatVnd(unitPrice * quantity);
        }
    }

    function setInputQuantity(row, quantity) {
        const input = row ? row.querySelector(".cart-qty-input") : null;
        if (input) {
            input.value = String(quantity);
        }
    }

    function updatePageTotal(total) {
        const totalEl = document.getElementById("cartPageTotal");
        if (totalEl) {
            totalEl.textContent = formatVnd(total);
        }
    }

    function hasAnyCartRow() {
        return document.querySelectorAll(".js-cart-page tbody tr[data-product-id]").length > 0;
    }

    async function applyQuantity(productId, nextQuantity) {
        const row = findRow(productId);
        if (!row) {
            return;
        }

        const input = row.querySelector(".cart-qty-input");
        const oldQuantity = toPositiveInt(input ? input.value : "1", 1);
        const safeQuantity = toPositiveInt(nextQuantity, oldQuantity);

        setInputQuantity(row, safeQuantity);
        setRowBusy(row, true);

        const response = await window.cartApi.setQuantity(productId, safeQuantity);
        if (response && response.success) {
            if (response.removed) {
                row.remove();
                window.cartApi.syncMiniCart(response.count, response.total);
                updatePageTotal(response.total);

                if (!hasAnyCartRow()) {
                    window.location.reload();
                }

                return;
            }

            const confirmedQuantity = toPositiveInt(response.quantity, safeQuantity);
            setInputQuantity(row, confirmedQuantity);
            updateLineTotal(row, confirmedQuantity);
            window.cartApi.syncMiniCart(response.count, response.total);
            updatePageTotal(response.total);
        } else {
            setInputQuantity(row, oldQuantity);
            updateLineTotal(row, oldQuantity);
        }

        setRowBusy(row, false);
    }

    async function removeItem(productId) {
        const row = findRow(productId);
        if (!row) {
            return;
        }

        setRowBusy(row, true);

        const response = await window.cartApi.removeItem(productId);
        if (response && response.success) {
            row.remove();
            window.cartApi.syncMiniCart(response.count, response.total);
            updatePageTotal(response.total);

            if (!hasAnyCartRow()) {
                window.location.reload();
            }

            return;
        }

        setRowBusy(row, false);
    }

    document.addEventListener("click", function (event) {
        if (hasBlazorConnection()) {
            return;
        }

        const decreaseBtn = event.target.closest(".js-cart-decrease");
        if (decreaseBtn) {
            event.preventDefault();
            event.stopPropagation();

            const productId = toInt(decreaseBtn.getAttribute("data-product-id"), 0);
            if (productId > 0) {
                const row = findRow(productId);
                const input = row ? row.querySelector(".cart-qty-input") : null;
                const currentQty = toPositiveInt(input ? input.value : "1", 1);
                applyQuantity(productId, Math.max(1, currentQty - 1));
            }
            return;
        }

        const increaseBtn = event.target.closest(".js-cart-increase");
        if (increaseBtn) {
            event.preventDefault();
            event.stopPropagation();

            const productId = toInt(increaseBtn.getAttribute("data-product-id"), 0);
            if (productId > 0) {
                const row = findRow(productId);
                const input = row ? row.querySelector(".cart-qty-input") : null;
                const currentQty = toPositiveInt(input ? input.value : "1", 1);
                applyQuantity(productId, currentQty + 1);
            }
            return;
        }

        const removeBtn = event.target.closest(".js-cart-remove");
        if (removeBtn) {
            event.preventDefault();
            event.stopPropagation();

            const productId = toInt(removeBtn.getAttribute("data-product-id"), 0);
            if (productId > 0) {
                removeItem(productId);
            }
        }
    });

    document.addEventListener("change", function (event) {
        if (hasBlazorConnection()) {
            return;
        }

        const qtyInput = event.target.closest(".js-cart-page .cart-qty-input");
        if (!qtyInput) {
            return;
        }

        const productId = toInt(qtyInput.getAttribute("data-product-id"), 0);
        if (productId <= 0) {
            return;
        }

        const qty = toPositiveInt(qtyInput.value, 1);
        applyQuantity(productId, qty);
    });
})();
