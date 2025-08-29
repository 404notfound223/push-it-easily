document.addEventListener('DOMContentLoaded', function () {
    const sidebarItems = document.querySelectorAll('.sidebar-item');

    sidebarItems.forEach(item => {
        item.addEventListener('click', () => {
            const dropdown = item.nextElementSibling;
            if (dropdown && dropdown.classList.contains('dropdown')) {
                dropdown.classList.toggle('open');

                const arrow = item.querySelector('.arrow');
                if (arrow) {
                    arrow.style.transform = dropdown.classList.contains('open') ? 'rotate(90deg)' : 'rotate(0deg)';
                }
            }
        });
    });

    const backToTopBtn = document.querySelector('.backToTopBtn');
    if (backToTopBtn) {
        window.addEventListener('scroll', () => {
            backToTopBtn.style.display = window.scrollY > 300 ? 'block' : 'none';
        });
    }
});

function scrollToTop() {
    window.scrollTo({ top: 0, behavior: 'smooth' });
}
