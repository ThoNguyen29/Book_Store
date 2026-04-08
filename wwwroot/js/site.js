// ===== GLOBAL VARIABLES =====
var currentBannerSlide = 0;
var cartTotal = 0;
var cartCount = 0;
var sliderInterval;
var storeChatState = null;

function toggleCategoryDropdown(e) {
    e.stopPropagation();
    const dd = document.getElementById("categoryDropdown");
    if (!dd) return;
    dd.classList.toggle("active");
}

document.addEventListener("click", () => {
    const dd = document.getElementById("categoryDropdown");
    if (dd) dd.classList.remove("active");
});

function initSlider() {
    const sliderContainer = document.getElementById("sliderContainer");
    const dots = document.querySelectorAll(".slider-dot");
    const totalSlides = 3;

    if (!sliderContainer || !dots.length) {
        return;
    }

    sliderInterval = setInterval(() => {
        currentBannerSlide = (currentBannerSlide + 1) % totalSlides;
        updateSlider();
    }, 5000);
}

function goToSlide(slideIndex) {
    currentBannerSlide = slideIndex;
    updateSlider();

    clearInterval(sliderInterval);
    sliderInterval = setInterval(() => {
        currentBannerSlide = (currentBannerSlide + 1) % 3;
        updateSlider();
    }, 5000);
}

function updateSlider() {
    const sliderContainer = document.getElementById("sliderContainer");
    const dots = document.querySelectorAll(".slider-dot");

    if (!sliderContainer) {
        return;
    }

    sliderContainer.style.transform = `translateX(-${currentBannerSlide * 100}%)`;

    dots.forEach((dot, index) => {
        if (index === currentBannerSlide) {
            dot.classList.add("active");
        } else {
            dot.classList.remove("active");
        }
    });
}

function addToCart(price) {
    cartTotal += price;
    cartCount += 1;

    updateCartDisplay();
    showNotification("Đã thêm sản phẩm vào giỏ hàng!");
}

function updateCartDisplay() {
    const cartTotalElement = document.getElementById("cartTotal");
    const cartCountElement = document.getElementById("cartCount");

    if (cartTotalElement && cartCountElement) {
        cartTotalElement.textContent = formatCurrency(cartTotal);
        cartCountElement.textContent = cartCount;
        cartCountElement.style.animation = "none";
        setTimeout(() => {
            cartCountElement.style.animation = "bounce 0.5s";
        }, 10);
    }
}

function formatCurrency(amount) {
    return amount.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ".");
}

document.addEventListener("click", function (e) {
    if (e.target.closest(".wishlist-btn")) {
        const btn = e.target.closest(".wishlist-btn");
        const icon = btn.querySelector("i");

        if (icon.classList.contains("far")) {
            icon.classList.remove("far");
            icon.classList.add("fas");
            btn.style.borderColor = "#ff6b6b";
            btn.style.color = "#ff6b6b";
            showNotification("Đã thêm vào danh sách yêu thích!");
        } else {
            icon.classList.remove("fas");
            icon.classList.add("far");
            btn.style.borderColor = "#e1e8ed";
            btn.style.color = "#333";
            showNotification("Đã xóa khỏi danh sách yêu thích!");
        }
    }
});

function showNotification(message) {
    const existingNotification = document.querySelector(".notification");
    if (existingNotification) {
        existingNotification.remove();
    }

    const notification = document.createElement("div");
    notification.className = "notification";
    notification.innerHTML = `
        <i class="fas fa-check-circle"></i>
        <span>${message}</span>
    `;

    notification.style.cssText = `
        position: fixed;
        top: 100px;
        right: 20px;
        background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
        color: white;
        padding: 15px 25px;
        border-radius: 10px;
        box-shadow: 0 5px 20px rgba(0,0,0,0.2);
        display: flex;
        align-items: center;
        gap: 10px;
        z-index: 10000;
        animation: slideInRight 0.3s, slideOutRight 0.3s 2.7s;
    `;

    document.body.appendChild(notification);

    setTimeout(() => {
        notification.remove();
    }, 3000);
}

function subscribeNewsletter(event) {
    event.preventDefault();

    const emailInput = event.target.querySelector('input[type="email"]');
    const email = emailInput.value;

    if (email) {
        showNotification(`Đã đăng ký thành công với email: ${email}`);
        emailInput.value = "";
    }

    return false;
}

document.querySelectorAll('a[href^="#"]').forEach((anchor) => {
    anchor.addEventListener("click", function (e) {
        e.preventDefault();
        const target = document.querySelector(this.getAttribute("href"));
        if (target) {
            target.scrollIntoView({
                behavior: "smooth",
                block: "start",
            });
        }
    });
});

function initSearch() {
    const searchInput = document.querySelector(".search-bar input");
    const searchButton = document.querySelector(".search-bar button");

    if (searchButton && searchInput) {
        searchButton.addEventListener("click", function () {
            const query = searchInput.value.trim();
            if (query) {
                performSearch(query);
            }
        });
    }

    if (searchInput) {
        searchInput.addEventListener("keypress", function (e) {
            if (e.key === "Enter") {
                const query = searchInput.value.trim();
                if (query) {
                    performSearch(query);
                }
            }
        });
    }
}

function performSearch(query) {
    console.log("Searching for:", query);
    showNotification(`Đang tìm kiếm: "${query}"`);
}

function initLazyLoading() {
    const images = document.querySelectorAll("img[data-src]");

    const imageObserver = new IntersectionObserver((entries, observer) => {
        entries.forEach((entry) => {
            if (entry.isIntersecting) {
                const img = entry.target;
                img.src = img.dataset.src;
                img.removeAttribute("data-src");
                observer.unobserve(img);
            }
        });
    });

    images.forEach((img) => imageObserver.observe(img));
}

function initProductAnimations() {
    const productCards = document.querySelectorAll(".product-card");

    const cardObserver = new IntersectionObserver((entries) => {
        entries.forEach((entry, index) => {
            if (entry.isIntersecting) {
                setTimeout(() => {
                    entry.target.style.opacity = "1";
                    entry.target.style.transform = "translateY(0)";
                }, index * 100);
            }
        });
    }, {
        threshold: 0.1,
    });

    productCards.forEach((card) => {
        card.style.opacity = "0";
        card.style.transform = "translateY(20px)";
        card.style.transition = "all 0.5s ease";
        cardObserver.observe(card);
    });
}

function initCategoryAnimations() {
    const categoryCards = document.querySelectorAll(".category-icon-card");

    const categoryObserver = new IntersectionObserver((entries) => {
        entries.forEach((entry, index) => {
            if (entry.isIntersecting) {
                setTimeout(() => {
                    entry.target.style.opacity = "1";
                    entry.target.style.transform = "scale(1)";
                }, index * 50);
            }
        });
    }, {
        threshold: 0.1,
    });

    categoryCards.forEach((card) => {
        card.style.opacity = "0";
        card.style.transform = "scale(0.8)";
        card.style.transition = "all 0.4s ease";
        categoryObserver.observe(card);
    });
}

function initScrollToTop() {
    const scrollBtn = document.createElement("button");
    scrollBtn.innerHTML = '<i class="fas fa-arrow-up"></i>';
    scrollBtn.className = "scroll-to-top";
    scrollBtn.style.cssText = `
        position: fixed;
        bottom: 30px;
        right: 30px;
        width: 50px;
        height: 50px;
        background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
        color: white;
        border: none;
        border-radius: 50%;
        cursor: pointer;
        display: none;
        align-items: center;
        justify-content: center;
        font-size: 20px;
        box-shadow: 0 5px 15px rgba(0,0,0,0.3);
        z-index: 1000;
        transition: all 0.3s;
    `;

    document.body.appendChild(scrollBtn);

    window.addEventListener("scroll", () => {
        if (window.pageYOffset > 300) {
            scrollBtn.style.display = "flex";
        } else {
            scrollBtn.style.display = "none";
        }
    });

    scrollBtn.addEventListener("click", () => {
        window.scrollTo({
            top: 0,
            behavior: "smooth",
        });
    });

    scrollBtn.addEventListener("mouseenter", () => {
        scrollBtn.style.transform = "translateY(-5px)";
    });

    scrollBtn.addEventListener("mouseleave", () => {
        scrollBtn.style.transform = "translateY(0)";
    });
}

function addAnimationStyles() {
    const style = document.createElement("style");
    style.textContent = `
        @keyframes bounce {
            0%, 100% { transform: scale(1); }
            50% { transform: scale(1.2); }
        }

        @keyframes slideInRight {
            from {
                transform: translateX(100%);
                opacity: 0;
            }
            to {
                transform: translateX(0);
                opacity: 1;
            }
        }

        @keyframes slideOutRight {
            from {
                transform: translateX(0);
                opacity: 1;
            }
            to {
                transform: translateX(100%);
                opacity: 0;
            }
        }

        .scroll-to-top:hover {
            box-shadow: 0 8px 25px rgba(102, 126, 234, 0.4) !important;
        }
    `;
    document.head.appendChild(style);
}

function appendStoreChatMessage(role, text, options = {}) {
    if (!storeChatState?.body) {
        return null;
    }

    const message = document.createElement("div");
    message.className = `store-chat-message ${role}${options.typing ? " typing" : ""}`;

    if (role === "bot") {
        const avatar = document.createElement("div");
        avatar.className = "store-chat-avatar";
        avatar.innerHTML = '<i class="fas fa-book-open"></i>';
        message.appendChild(avatar);
    }

    const bubble = document.createElement("div");
    bubble.className = "store-chat-bubble";

    if (options.typing) {
        bubble.innerHTML = `
            <span class="store-chat-dot"></span>
            <span class="store-chat-dot"></span>
            <span class="store-chat-dot"></span>
        `;
    } else {
        bubble.textContent = text;
    }

    message.appendChild(bubble);
    storeChatState.body.appendChild(message);
    storeChatState.body.scrollTop = storeChatState.body.scrollHeight;
    return message;
}

async function handleStoreChatSubmit(event) {
    event.preventDefault();

    if (!storeChatState || storeChatState.isLoading) {
        return;
    }

    const userMessage = storeChatState.input.value.trim();
    if (!userMessage) {
        return;
    }

    storeChatState.input.value = "";
    storeChatState.messages.push({ role: "user", text: userMessage });
    appendStoreChatMessage("user", userMessage);

    const typingNode = appendStoreChatMessage("bot", "", { typing: true });
    storeChatState.isLoading = true;
    storeChatState.send.disabled = true;

    try {
        const response = await fetch("/Chat/Ask", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify({
                message: userMessage,
                history: storeChatState.messages.slice(-8),
            }),
        });

        const data = await response.json();

        if (!response.ok) {
            throw new Error(data.reply || "Khong the ket noi voi tro ly luc nay.");
        }

        storeChatState.messages.push({ role: "bot", text: data.reply });
        typingNode?.remove();
        appendStoreChatMessage("bot", data.reply);
    } catch (error) {
        typingNode?.remove();
        const errorMessage = error?.message || "Da co loi xay ra. Ban thu lai sau nhe.";
        storeChatState.messages.push({ role: "bot", text: errorMessage });
        appendStoreChatMessage("bot", errorMessage);
    } finally {
        storeChatState.isLoading = false;
        storeChatState.send.disabled = false;
        storeChatState.input.focus();
    }
}

function initStoreChatWidget() {
    const widget = document.querySelector(".store-chat-widget");
    const toggle = document.getElementById("storeChatToggle");
    const close = document.getElementById("storeChatClose");
    const panel = document.getElementById("storeChatPanel");
    const body = document.getElementById("storeChatBody");
    const form = document.getElementById("storeChatForm");
    const input = document.getElementById("storeChatInput");
    const send = document.getElementById("storeChatSend");

    if (!widget || !toggle || !close || !panel || !body || !form || !input || !send) {
        return;
    }

    storeChatState = {
        widget,
        toggle,
        panel,
        body,
        form,
        input,
        send,
        isLoading: false,
        messages: [],
    };

    form.addEventListener("submit", handleStoreChatSubmit);

    close.addEventListener("click", (event) => {
        event.stopPropagation();
    });

    document.addEventListener("keydown", (event) => {
        if (event.key === "Escape" && widget.classList.contains("is-open")) {
            toggleStoreChatWidget();
        }
    });

    document.addEventListener("click", (event) => {
        if (!widget.classList.contains("is-open")) {
            return;
        }

        if (!widget.contains(event.target)) {
            widget.classList.remove("is-open");
            toggle.setAttribute("aria-expanded", "false");
            panel.setAttribute("aria-hidden", "true");
        }
    });
}

function toggleStoreChatWidget(event) {
    if (event) {
        event.preventDefault();
        event.stopPropagation();
    }

    if (!storeChatState) {
        return false;
    }

    const isOpen = storeChatState.widget.classList.contains("is-open");
    storeChatState.widget.classList.toggle("is-open", !isOpen);
    storeChatState.toggle.setAttribute("aria-expanded", String(!isOpen));
    storeChatState.panel.setAttribute("aria-hidden", String(isOpen));

    if (!isOpen) {
        setTimeout(() => storeChatState.input.focus(), 180);
    }

    return false;
}

window.toggleStoreChatWidget = toggleStoreChatWidget;
window.goToSlide = goToSlide;

document.addEventListener("DOMContentLoaded", function () {
    initSlider();
    initSearch();
    initLazyLoading();
    initProductAnimations();
    initCategoryAnimations();
    initScrollToTop();
    initStoreChatWidget();
    addAnimationStyles();
});

document.addEventListener("keypress", function (e) {
    if (e.key === "Enter" && e.target.tagName !== "TEXTAREA") {
        const form = e.target.closest("form");
        if (form && !form.classList.contains("newsletter-form") && form.id !== "storeChatForm") {
            e.preventDefault();
        }
    }
});
