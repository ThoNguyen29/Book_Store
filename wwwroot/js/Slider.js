let currentSlideIndex = 0;
const slides = document.querySelectorAll('.slide');
const dots = document.querySelectorAll('.dot');

function showSlide(index) {
    // Xóa class active ở tất cả slide và dot
    slides.forEach(slide => slide.classList.remove('active'));
    dots.forEach(dot => dot.classList.remove('active'));

    // Thêm class active cho slide hiện tại
    slides[index].classList.add('active');
    dots[index].classList.add('active');
}

function nextSlide() {
    currentSlideIndex = (currentSlideIndex + 1) % slides.length;
    showSlide(currentSlideIndex);
}

function currentSlide(index) {
    currentSlideIndex = index;
    showSlide(currentSlideIndex);
}

// Tự động chạy slider mỗi 5 giây
let autoSlide = setInterval(nextSlide, 5000);

// Dừng slider khi người dùng click vào dot (optional)
document.querySelector('.dots').addEventListener('click', () => {
    clearInterval(autoSlide);
    autoSlide = setInterval(nextSlide, 5000); 
});