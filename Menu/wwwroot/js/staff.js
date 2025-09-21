function showTab(tabName, event) {
    // Hide all tabs
    document.querySelectorAll(".tab-content").forEach((tab) => {
        tab.classList.remove("active")
    })
    document.querySelectorAll(".tab-button").forEach((btn) => {
        btn.classList.remove("active")
    })

    // Show selected tab
    document.getElementById(tabName + "-tab").classList.add("active")
    if (event) event.target.classList.add("active")

    //Load data for the selected tab
    if (tabName === 'orders') {
        loadOrders();
    } else if (tabName === 'users') {
        loadUsers();
    } else if (tabName === 'products') {
        loadProducts();
    }
}

// ================= POPOUT MANAGEMENT =================
function openPopout(panelId, itemId) {
    console.log("Opening:", panelId, "ItemId:", itemId)
    const panel = document.getElementById(panelId)
    if (!panel) return

    // Create backdrop if it doesn't exist
    let backdrop = document.getElementById("popout-backdrop")
    if (!backdrop) {
        backdrop = document.createElement("div")
        backdrop.id = "popout-backdrop"
        backdrop.className = "popout-backdrop"
        backdrop.onclick = () => closePopout(panelId)
        document.body.appendChild(backdrop)
    }

    backdrop.classList.add("open")

    // Load data based on panel type
    if (panelId === "editUserPanel" && itemId) {
        loadUserData(itemId)
    } else if (panelId === "editProductPanel" && itemId) {
        loadProductData(itemId)
    } else if (panelId === "orderDetailPanel" && itemId) {
        loadOrderDetails(itemId)
    }

    panel.classList.add("open")
}

function closePopout(panelId) {
    const panel = document.getElementById(panelId)
    if (panel) {
        panel.classList.remove("open")
    }

    const backdrop = document.getElementById("popout-backdrop")
    if (backdrop) {
        backdrop.classList.remove("open")
    }
}

function closeAllPopouts() {
    const popouts = document.querySelectorAll(".popout-panel.open")
    popouts.forEach((popout) => {
        popout.classList.remove("open")
    })

    const backdrop = document.getElementById("popout-backdrop")
    if (backdrop) {
        backdrop.classList.remove("open")
    }
}

// Close popout when clicking outside
//document.addEventListener("click", (event) => {
//    const popouts = document.querySelectorAll(".popout-panel.open")
//    popouts.forEach((popout) => {
//        if (!popout.contains(event.target) && !event.target.closest('[onclick*="openPopout"]')) {
//            popout.classList.remove("open")
//        }
//    })
//})

// ================= LOGOUT =================
function logout() {
    if (confirm("Are you sure you want to logout?")) {
        fetch("/Staff/Logout", {
            method: "POST",
            credentials: "same-origin",
        })
            .then(() => {
                window.location.href = "/Login/Login"
            })
            .catch(() => {
                window.location.href = "/Login/Login"
            })
    }
}

// ================= NOTIFICATION =================
function showNotification(message, type = "info") {
    // Remove any existing notifications
    const existingNotifications = document.querySelectorAll(".notification")
    existingNotifications.forEach((notification) => {
        notification.remove()
    })

    const notification = document.createElement("div")
    notification.className = `notification ${type}`
    notification.innerHTML = `
        <div style="display: flex; align-items: center; gap: 10px;">
            <span style="font-size: 16px;">
                ${type.includes("success") ? "✅" : type.includes("error") ? "❌" : "ℹ️"}
            </span>
            <span>${message}</span>
            <button onclick="this.parentElement.parentElement.remove()" style="background: none; border: none; color: white; font-size: 18px; cursor: pointer; margin-left: auto;">×</button>
        </div>
    `

    document.body.appendChild(notification)

    // Auto-dismiss after 5 seconds for success messages, 8 seconds for errors
    const dismissTime = type.includes("error") ? 8000 : 5000
    setTimeout(() => {
        if (notification.parentElement) {
            notification.style.animation = "slideOut 0.3s ease forwards"
            setTimeout(() => {
                if (notification.parentElement) {
                    notification.remove()
                }
            }, 300)
        }
    }, dismissTime)
}

const style = document.createElement("style")
style.textContent = `
    @keyframes slideOut {
        from {
            transform: translateX(0);
            opacity: 1;
        }
        to {
            transform: translateX(100%);
            opacity: 0;
        }
    }
    @keyframes spin {
        0% { transform: rotate(0deg); }
        100% { transform: rotate(360deg); }
    }
`
document.head.appendChild(style)

// ================= CONFIRM DELETE =================
function confirmDelete(type, id) {
    const typeNames = {
        order: "order",
        user: "user",
        product: "product",
        category: "category",
    }

    const message = `Are you sure you want to delete this ${typeNames[type]}? This action cannot be undone.`

    if (confirm(message)) {
        if (type === "order") deleteOrder(id)
        else if (type === "user") deleteUser(id)
        else if (type === "product") deleteProduct(id)
        else if (type === "category") deleteCategory(id)
    }
}

