document.addEventListener("DOMContentLoaded", () => {
    const sidebarItems = document.querySelectorAll(".sidebar-item")

    sidebarItems.forEach((item) => {
        item.addEventListener("click", () => {
            const dropdown = item.nextElementSibling
            if (dropdown && dropdown.classList.contains("dropdown")) {
                dropdown.classList.toggle("open")

                const arrow = item.querySelector(".arrow")
                if (arrow) {
                    arrow.style.transform = dropdown.classList.contains("open") ? "rotate(90deg)" : "rotate(0deg)"
                }
            }
        })
    })

    const backToTopBtn = document.querySelector(".backToTopBtn")
    if (backToTopBtn) {
        window.addEventListener("scroll", () => {
            backToTopBtn.style.display = window.scrollY > 300 ? "block" : "none"
        })
    }

    // Update order count on page load
    if (typeof window.updateOrderCount === "function") {
        window.updateOrderCount()
    }
})

function navigateTo(url) {
    window.location.href = url
}

function showProfile() {
    document.getElementById("profile-modal").style.display = "block"
}

function closeProfile() {
    document.getElementById("profile-modal").style.display = "none"
}

function viewOrderHistory() {
    window.location.href = "/Order/OrderHistory"
}

function logout() {
    if (confirm("Are you sure you want to logout?")) {
        fetch("/Login/Logout", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
            },
        }).then(() => {
            window.location.href = "/Login/Login"
        })
    }
}

function toggleProfileDropdown(event) {
    event.stopPropagation()
    const dropdown = document.getElementById("profileDropdown")
    if (dropdown) {
        dropdown.style.display = dropdown.style.display === "block" ? "none" : "block"
    }
}

// Hide dropdown when clicking outside
document.addEventListener("click", (e) => {
    const dropdown = document.getElementById("profileDropdown")
    if (dropdown && dropdown.style.display === "block") {
        if (!e.target.closest(".profile-container")) {
            dropdown.style.display = "none"
        }
    }
})

// function scrollToTop() {
//     window.scrollTo({ top: 0, behavior: 'smooth' });
// }

function resetPassword() {
    closeProfile()
    showForgotPassword()
}

function editUsername() {
    document.getElementById("username-display").style.display = "none"
    document.getElementById("edit-username-btn").style.display = "none"
    document.getElementById("username-edit").style.display = "block"
}

function cancelEditUsername() {
    document.getElementById("username-display").style.display = "inline"
    document.getElementById("edit-username-btn").style.display = "inline"
    document.getElementById("username-edit").style.display = "none"
}

function saveUsername() {
    const newUsername = document.getElementById("new-username").value.trim()
    if (!newUsername) {
        alert("Please enter a valid username")
        return
    }

    fetch("/Login/UpdateUsername", {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify({ newUsername: newUsername }),
    })
        .then((response) => {
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`)
            }
            const contentType = response.headers.get("content-type")
            if (contentType && contentType.indexOf("application/json") !== -1) {
                return response.json()
            } else {
                throw new Error("Server did not return JSON response")
            }
        })
        .then((data) => {
            if (data.success) {
                document.getElementById("username-display").textContent = newUsername
                cancelEditUsername()
                alert("Username updated successfully!")
            } else {
                alert("Error: " + data.error)
            }
        })
        .catch((error) => {
            console.error("Error updating username:", error)
            alert("Error updating username: " + error.message)
        })
}

// Function to show forgot password modal
function showForgotPassword() {
    document.getElementById("forgot-password-modal").style.display = "block"
}
