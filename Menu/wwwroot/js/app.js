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

function navigateTo(url) {
    window.location.href = url;
    }

function showProfile() {
    document.getElementById('profile-modal').style.display = 'block';
    }

function closeProfile() {
    document.getElementById('profile-modal').style.display = 'none';
    }

function viewOrderHistory() {
    window.location.href = '/Order/OrderHistory';
    }

function logout() {
        if (confirm('Are you sure you want to logout?')) {
        fetch('/Login/Logout', {
            method: 'POST',
            headers: {
                'Content-Type': 'application/json',
            }
        }).then(() => {
            window.location.href = '/Login/Login';
        });
        }
    }

// Update order count on page load
document.addEventListener('DOMContentLoaded', function() {
        if (typeof updateOrderCount === 'function') {
            updateOrderCount();
        }
});

function toggleProfileMenu(event) {
    event.stopPropagation(); // prevent click from bubbling up
    const dropdown = document.getElementById("profileDropdown");
    dropdown.classList.toggle("show");
}

// close dropdown when clicking outside
document.addEventListener("click", function (e) {
    const dropdown = document.getElementById("profileDropdown");
    if (dropdown && dropdown.classList.contains("show")) {
        if (!e.target.closest(".profile-container")) {
            dropdown.classList.remove("show");
        }
    }
});

//function scrollToTop() {
//    window.scrollTo({ top: 0, behavior: 'smooth' });
//}
