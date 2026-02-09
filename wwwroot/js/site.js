// ===== GLOBAL VARIABLES =====
let currentSlide = 0;
let cartTotal = 0;
let cartCount = 0;
let sliderInterval;

function toggleCategoryDropdown(e){
  e.stopPropagation();
  const dd = document.getElementById("categoryDropdown");
  if (!dd) return;
  dd.classList.toggle("active");
}

// Click ra ngoài thì tự đóng dropdown
document.addEventListener("click", () => {
  const dd = document.getElementById("categoryDropdown");
  if (dd) dd.classList.remove("active");
});


// ===== HERO SLIDER =====
function initSlider() {
    const sliderContainer = document.getElementById('sliderContainer');
    const dots = document.querySelectorAll('.slider-dot');
    const totalSlides = 3;
    
    // Auto slide every 5 seconds
    sliderInterval = setInterval(() => {
        currentSlide = (currentSlide + 1) % totalSlides;
        updateSlider();
    }, 5000);
}

function goToSlide(slideIndex) {
    currentSlide = slideIndex;
    updateSlider();
    
    // Reset auto slide timer
    clearInterval(sliderInterval);
    sliderInterval = setInterval(() => {
        currentSlide = (currentSlide + 1) % 3;
        updateSlider();
    }, 5000);
}

function updateSlider() {
    const sliderContainer = document.getElementById('sliderContainer');
    const dots = document.querySelectorAll('.slider-dot');
    
    // Move slider
    sliderContainer.style.transform = `translateX(-${currentSlide * 100}%)`;
    
    // Update dots
    dots.forEach((dot, index) => {
        if (index === currentSlide) {
            dot.classList.add('active');
        } else {
            dot.classList.remove('active');
        }
    });
}

// ===== SHOPPING CART =====
function addToCart(price) {
    cartTotal += price;
    cartCount += 1;
    
    updateCartDisplay();
    showNotification('Đã thêm sản phẩm vào giỏ hàng!');
}

function updateCartDisplay() {
    const cartTotalElement = document.getElementById('cartTotal');
    const cartCountElement = document.getElementById('cartCount');
    
    if (cartTotalElement && cartCountElement) {
        cartTotalElement.textContent = formatCurrency(cartTotal);
        cartCountElement.textContent = cartCount;
        
        // Add animation
        cartCountElement.style.animation = 'none';
        setTimeout(() => {
            cartCountElement.style.animation = 'bounce 0.5s';
        }, 10);
    }
}

function formatCurrency(amount) {
    return amount.toString().replace(/\B(?=(\d{3})+(?!\d))/g, ".");
}

// ===== WISHLIST =====
document.addEventListener('click', function(e) {
    if (e.target.closest('.wishlist-btn')) {
        const btn = e.target.closest('.wishlist-btn');
        const icon = btn.querySelector('i');
        
        if (icon.classList.contains('far')) {
            icon.classList.remove('far');
            icon.classList.add('fas');
            btn.style.borderColor = '#ff6b6b';
            btn.style.color = '#ff6b6b';
            showNotification('Đã thêm vào danh sách yêu thích!');
        } else {
            icon.classList.remove('fas');
            icon.classList.add('far');
            btn.style.borderColor = '#e1e8ed';
            btn.style.color = '#333';
            showNotification('Đã xóa khỏi danh sách yêu thích!');
        }
    }
});

// ===== NOTIFICATION =====
function showNotification(message) {
    // Remove existing notification
    const existingNotification = document.querySelector('.notification');
    if (existingNotification) {
        existingNotification.remove();
    }
    
    // Create notification
    const notification = document.createElement('div');
    notification.className = 'notification';
    notification.innerHTML = `
        <i class="fas fa-check-circle"></i>
        <span>${message}</span>
    `;
    
    // Add styles
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
    
    // Remove after 3 seconds
    setTimeout(() => {
        notification.remove();
    }, 3000);
}

// ===== NEWSLETTER SUBSCRIPTION =====
function subscribeNewsletter(event) {
    event.preventDefault();
    
    const emailInput = event.target.querySelector('input[type="email"]');
    const email = emailInput.value;
    
    if (email) {
        showNotification(`Đã đăng ký thành công với email: ${email}`);
        emailInput.value = '';
    }
    
    return false;
}

// ===== SMOOTH SCROLL =====
document.querySelectorAll('a[href^="#"]').forEach(anchor => {
    anchor.addEventListener('click', function (e) {
        e.preventDefault();
        const target = document.querySelector(this.getAttribute('href'));
        if (target) {
            target.scrollIntoView({
                behavior: 'smooth',
                block: 'start'
            });
        }
    });
});

// ===== SEARCH FUNCTIONALITY =====
function initSearch() {
    const searchInput = document.querySelector('.search-bar input');
    const searchButton = document.querySelector('.search-bar button');
    
    if (searchButton) {
        searchButton.addEventListener('click', function() {
            const query = searchInput.value.trim();
            if (query) {
                performSearch(query);
            }
        });
    }
    
    if (searchInput) {
        searchInput.addEventListener('keypress', function(e) {
            if (e.key === 'Enter') {
                const query = searchInput.value.trim();
                if (query) {
                    performSearch(query);
                }
            }
        });
    }
}

function performSearch(query) {
    console.log('Searching for:', query);
    showNotification(`Đang tìm kiếm: "${query}"`);
    // Implement actual search logic here
    // window.location.href = `/search?q=${encodeURIComponent(query)}`;
}

// ===== LAZY LOADING IMAGES =====
function initLazyLoading() {
    const images = document.querySelectorAll('img[data-src]');
    
    const imageObserver = new IntersectionObserver((entries, observer) => {
        entries.forEach(entry => {
            if (entry.isIntersecting) {
                const img = entry.target;
                img.src = img.dataset.src;
                img.removeAttribute('data-src');
                observer.unobserve(img);
            }
        });
    });
    
    images.forEach(img => imageObserver.observe(img));
}

// ===== PRODUCT CARD ANIMATIONS =====
function initProductAnimations() {
    const productCards = document.querySelectorAll('.product-card');
    
    const cardObserver = new IntersectionObserver((entries) => {
        entries.forEach((entry, index) => {
            if (entry.isIntersecting) {
                setTimeout(() => {
                    entry.target.style.opacity = '1';
                    entry.target.style.transform = 'translateY(0)';
                }, index * 100);
            }
        });
    }, {
        threshold: 0.1
    });
    
    productCards.forEach(card => {
        card.style.opacity = '0';
        card.style.transform = 'translateY(20px)';
        card.style.transition = 'all 0.5s ease';
        cardObserver.observe(card);
    });
}

// ===== CATEGORY ICON ANIMATIONS =====
function initCategoryAnimations() {
    const categoryCards = document.querySelectorAll('.category-icon-card');
    
    const categoryObserver = new IntersectionObserver((entries) => {
        entries.forEach((entry, index) => {
            if (entry.isIntersecting) {
                setTimeout(() => {
                    entry.target.style.opacity = '1';
                    entry.target.style.transform = 'scale(1)';
                }, index * 50);
            }
        });
    }, {
        threshold: 0.1
    });
    
    categoryCards.forEach(card => {
        card.style.opacity = '0';
        card.style.transform = 'scale(0.8)';
        card.style.transition = 'all 0.4s ease';
        categoryObserver.observe(card);
    });
}

// ===== SCROLL TO TOP BUTTON =====
function initScrollToTop() {
    // Create button
    const scrollBtn = document.createElement('button');
    scrollBtn.innerHTML = '<i class="fas fa-arrow-up"></i>';
    scrollBtn.className = 'scroll-to-top';
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
    
    // Show/hide button
    window.addEventListener('scroll', () => {
        if (window.pageYOffset > 300) {
            scrollBtn.style.display = 'flex';
        } else {
            scrollBtn.style.display = 'none';
        }
    });
    
    // Scroll to top on click
    scrollBtn.addEventListener('click', () => {
        window.scrollTo({
            top: 0,
            behavior: 'smooth'
        });
    });
    
    // Hover effect
    scrollBtn.addEventListener('mouseenter', () => {
        scrollBtn.style.transform = 'translateY(-5px)';
    });
    
    scrollBtn.addEventListener('mouseleave', () => {
        scrollBtn.style.transform = 'translateY(0)';
    });
}

// ===== ADD CSS ANIMATIONS =====
function addAnimationStyles() {
    const style = document.createElement('style');
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
        
        @keyframes fadeIn {
            from { opacity: 0; }
            to { opacity: 1; }
        }
        
        .scroll-to-top:hover {
            box-shadow: 0 8px 25px rgba(102, 126, 234, 0.4) !important;
        }
    `;
    document.head.appendChild(style);
}

// ===== INITIALIZATION =====
document.addEventListener('DOMContentLoaded', function() {
    // Initialize all features
    initSlider();
    initSearch();
    initLazyLoading();
    initProductAnimations();
    initCategoryAnimations();
    initScrollToTop();
    addAnimationStyles();
    
    console.log('Website loaded successfully!');
});

// ===== PREVENT FORM SUBMISSION ON ENTER =====
document.addEventListener('keypress', function(e) {
    if (e.key === 'Enter' && e.target.tagName !== 'TEXTAREA') {
        const form = e.target.closest('form');
        if (form && !form.classList.contains('newsletter-form')) {
            e.preventDefault();
        }
    }

});
