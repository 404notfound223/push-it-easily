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

// ================= ORDERS =================
function updateOrderStatus(orderId, status) {
    // Close the dropdown
    const menu = document.getElementById(`status-menu-${orderId}`)
    if (menu) {
        menu.classList.remove("open")
    }

    // Find the status badge and update it
    const statusBadge = document.querySelector(`tr[data-order-id="${orderId}"] .status-badge-clickable`)

    if (statusBadge) {
        statusBadge.classList.add("status-updating")
    }

    fetch("/Staff/UpdateOrderStatus", {
        method: "POST",
        headers: {
            "Content-Type": "application/x-www-form-urlencoded",
            "X-Requested-With": "XMLHttpRequest",
        },
        body: `orderId=${orderId}&status=${status}`,
    })
        .then((res) => {
            if (!res.ok) {
                throw new Error(`HTTP error! status: ${res.status}`)
            }
            return res.json()
        })
        .then((data) => {
            if (data.success) {
                // Update the status badge with new styling
                if (statusBadge) {
                    statusBadge.classList.remove("status-updating")
                    statusBadge.className = `status-badge-clickable ${status}`
                    statusBadge.textContent = status.toUpperCase()
                }

                showNotification(`Order ${orderId.substring(0, 8)} status updated to ${status.toUpperCase()}`, "success")
            } else {
                showNotification("Failed to update order status: " + (data.error || "Unknown error"), "error")
                if (statusBadge) {
                    statusBadge.classList.remove("status-updating")
                }
            }
        })
        .catch((error) => {
            console.error("Error updating order status:", error)
            showNotification("Error updating order status. Please try again.", "error")
            if (statusBadge) {
                statusBadge.classList.remove("status-updating")
            }
        })
}

function updateOrderStats() {
    fetch("/Staff/GetOrderStats")
        .then((res) => res.json())
        .then((data) => {
            if (data.success && data.stats) {
                // Update stat cards if they exist
                const statCards = document.querySelectorAll(".stat-card")
                statCards.forEach((card) => {
                    const label = card.querySelector(".stat-label").textContent.toLowerCase()
                    if (data.stats[label] !== undefined) {
                        const numberElement = card.querySelector(".stat-number")
                        const currentValue = Number.parseInt(numberElement.textContent)
                        const newValue = data.stats[label]

                        if (currentValue !== newValue) {
                            // Animate the number change
                            animateNumberChange(numberElement, currentValue, newValue)
                        }
                    }
                })
            }
        })
        .catch((error) => {
            console.error("Failed to update order stats:", error)
        })
}

function animateNumberChange(element, from, to) {
    const duration = 1000 // 1 second
    const steps = 30
    const stepValue = (to - from) / steps
    const stepDuration = duration / steps
    let currentStep = 0

    const interval = setInterval(() => {
        currentStep++
        const currentValue = Math.round(from + stepValue * currentStep)
        element.textContent = currentValue

        if (currentStep >= steps) {
            clearInterval(interval)
            element.textContent = to // Ensure final value is exact
        }
    }, stepDuration)
}

function loadOrderDetails(orderId) {
    fetch(`/Staff/GetOrderDetails?orderId=${orderId}`)
        .then((res) => res.json())
        .then((data) => {
            if (data.success) {
                const order = data.order
                let detailsHtml = `
                <div class="order-info">
                    <div class="info-row">
                        <strong>Order ID:</strong> 
                        <span>${order.orderId}</span>
                    </div>
                    <div class="info-row">
                        <strong>Customer:</strong> 
                        <span>${order.user ? order.user.name : "Guest"}</span>
                    </div>
                    <div class="info-row">
                        <strong>Email:</strong> 
                        <span>${order.user ? order.user.email : "N/A"}</span>
                    </div>
                    <div class="info-row">
                        <strong>Date:</strong> 
                        <span>${new Date(order.orderDate).toLocaleString()}</span>
                    </div>
                    <div class="info-row">
                        <strong>Status:</strong> 
                        <span class="order-status ${order.status.toLowerCase()}">
                            <span class="status-indicator ${order.status.toLowerCase()}"></span>
                            ${order.status.toUpperCase()}
                        </span>
                    </div>
                    <div class="info-row">
                        <strong>Total:</strong> 
                        <span style="font-size: 1.2em; font-weight: bold; color: #28a745;">$${order.totalAmount.toFixed(2)}</span>
                    </div>
                </div>
                
                <div class="order-actions" style="margin: 20px 0; padding: 15px; background: #f8f9fa; border-radius: 8px;">
                    <label for="status-select" style="display: block; margin-bottom: 8px; font-weight: 600;">Update Status:</label>
                    <select id="status-select" class="status-select" onchange="updateOrderStatus('${order.orderId}', this.value)" style="margin-right: 10px;">
                        <option value="pending" ${order.status === "pending" ? "selected" : ""}>Pending</option>
                        <option value="preparing" ${order.status === "preparing" ? "selected" : ""}>Preparing</option>
                        <option value="ready" ${order.status === "ready" ? "selected" : ""}>Ready</option>
                        <option value="completed" ${order.status === "completed" ? "selected" : ""}>Completed</option>
                        <option value="cancelled" ${order.status === "cancelled" ? "selected" : ""}>Cancelled</option>
                    </select>
                    <button onclick="enableOrderEdit('${order.orderId}')" class="btn btn-info btn-sm">Edit Order</button>
                </div>
                
                <h4 style="margin-top: 25px; color: #495057; border-bottom: 2px solid #e9ecef; padding-bottom: 8px;">Order Items</h4>
                <table class="order-items-table" style="margin-top: 15px;">
                    <thead>
                        <tr>
                            <th>Product</th>
                            <th>Price</th>
                            <th>Quantity</th>
                            <th>Total</th>
                            <th id="actions-header" style="display: none;">Actions</th>
                        </tr>
                    </thead>
                    <tbody id="order-items-tbody">
            `

                order.orderItems.forEach((item) => {
                    detailsHtml += `
                    <tr data-item-id="${item.id}">
                        <td>${item.product.name}</td>
                        <td class="item-price">$${item.unitPrice.toFixed(2)}</td>
                        <td>
                            <span class="quantity-display">${item.quantity}</span>
                            <input type="number" class="quantity-edit" value="${item.quantity}" min="1" 
                                   onchange="updateItemTotal(this, ${item.unitPrice})" style="display: none; width: 60px;">
                        </td>
                        <td class="item-total">$${(item.quantity * item.unitPrice).toFixed(2)}</td>
                        <td class="item-actions" style="display: none;">
                            <button onclick="removeOrderItem('${item.id}')" class="btn btn-danger btn-sm">Remove</button>
                        </td>
                    </tr>
                `
                })

                detailsHtml += `
                    </tbody>
                    <tfoot>
                        <tr style="font-weight: bold; background-color: #f8f9fa;">
                            <td colspan="3" style="text-align: right; padding: 15px;">Total:</td>
                            <td id="order-total" style="font-size: 1.1em; color: #28a745;">$${order.totalAmount.toFixed(2)}</td>
                            <td></td>
                        </tr>
                    </tfoot>
                </table>
                
                <div class="order-edit-actions" style="margin-top: 20px; text-align: right; display: none;">
                    <button id="save-order-btn" onclick="saveOrderChanges('${order.orderId}')" class="btn btn-primary" style="display: none;">Save Changes</button>
                    <button id="cancel-edit-btn" onclick="cancelOrderEdit()" class="btn btn-secondary" style="display: none; margin-left: 10px;">Cancel</button>
                </div>
            `

                document.getElementById("orderDetailContent").innerHTML = detailsHtml
            } else {
                showNotification("Error loading order details: " + data.error, "error")
                document.getElementById("orderDetailContent").innerHTML = `
            <div style="text-align: center; padding: 40px; color: #dc3545;">
                <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="margin-bottom: 16px;">
                    <circle cx="12" cy="12" r="10"></circle>
                    <line x1="15" y1="9" x2="9" y2="15"></line>
                    <line x1="9" y1="9" x2="15" y2="15"></line>
                </svg>
                <h4>Error Loading Order</h4>
                <p>Unable to load order details. Please try again.</p>
                <button class="btn btn-primary" onclick="loadOrderDetails('${orderId}')">Retry</button>
            </div>
        `
            }
        })
        .catch(() => {
            showNotification("Error loading order details", "error")
            document.getElementById("orderDetailContent").innerHTML = `
        <div style="text-align: center; padding: 40px; color: #dc3545;">
            <svg width="48" height="48" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="margin-bottom: 16px;">
                <circle cx="12" cy="12" r="10"></circle>
                <line x1="15" y1="9" x2="9" y2="15"></line>
                <line x1="9" y1="9" x2="15" y2="15"></line>
            </svg>
            <h4>Connection Error</h4>
            <p>Unable to connect to server. Please check your connection and try again.</p>
            <button class="btn btn-primary" onclick="loadOrderDetails('${orderId}')">Retry</button>
        </div>
      `
        })
}

function enableOrderEdit(orderId) {
    // Show edit controls
    document.querySelectorAll(".quantity-display").forEach((el) => (el.style.display = "none"))
    document.querySelectorAll(".quantity-edit").forEach((el) => (el.style.display = "inline-block"))
    document.querySelectorAll(".item-actions").forEach((el) => (el.style.display = "table-cell"))
    document.getElementById("actions-header").style.display = "table-cell"

    // Toggle buttons
    document.querySelector('[onclick*="enableOrderEdit"]').style.display = "none"
    document.getElementById("save-order-btn").style.display = "inline-block"
    document.getElementById("cancel-edit-btn").style.display = "inline-block"

    // Store original order data for cancellation
    window.originalOrderData = {
        orderId: orderId,
        items: [],
    }

    document.querySelectorAll("#order-items-tbody tr").forEach((row) => {
        const itemId = row.dataset.itemId
        const quantity = row.querySelector(".quantity-edit").value
        const unitPrice = Number.parseFloat(row.querySelector(".item-price").textContent.replace("$", ""))

        window.originalOrderData.items.push({
            itemId: itemId,
            quantity: Number.parseInt(quantity),
            unitPrice: unitPrice,
        })
    })
}

function cancelOrderEdit() {
    // Hide edit controls
    document.querySelectorAll(".quantity-display").forEach((el) => (el.style.display = "inline"))
    document.querySelectorAll(".quantity-edit").forEach((el) => (el.style.display = "none"))
    document.querySelectorAll(".item-actions").forEach((el) => (el.style.display = "none"))
    document.getElementById("actions-header").style.display = "none"

    // Toggle buttons
    document.querySelector('[onclick*="enableOrderEdit"]').style.display = "inline-block"
    document.getElementById("save-order-btn").style.display = "none"
    document.getElementById("cancel-edit-btn").style.display = "none"

    // Restore original values
    if (window.originalOrderData) {
        window.originalOrderData.items.forEach((item) => {
            const row = document.querySelector(`tr[data-item-id="${item.itemId}"]`)
            if (row) {
                row.querySelector(".quantity-edit").value = item.quantity
                row.querySelector(".quantity-display").textContent = item.quantity
                row.querySelector(".item-total").textContent = `$${(item.quantity * item.unitPrice).toFixed(2)}`
            }
        })

        // Recalculate total
        updateOrderTotal()
    }
}

function updateItemTotal(quantityInput, unitPrice) {
    const row = quantityInput.closest("tr")
    const quantity = Number.parseInt(quantityInput.value) || 1
    const total = quantity * unitPrice

    // Update display quantity
    row.querySelector(".quantity-display").textContent = quantity

    // Update item total
    row.querySelector(".item-total").textContent = `$${total.toFixed(2)}`

    // Update order total
    updateOrderTotal()
}

function updateOrderTotal() {
    let total = 0
    document.querySelectorAll(".item-total").forEach((el) => {
        const amount = Number.parseFloat(el.textContent.replace("$", ""))
        total += amount
    })

    document.getElementById("order-total").textContent = `$${total.toFixed(2)}`
}

function removeOrderItem(button) {
    if (confirm("Are you sure you want to remove this item from the order?")) {
        const row = button.closest("tr")
        row.remove()
        updateOrderTotal()
    }
}

function saveOrderChanges(orderId) {
    const orderItems = []

    document.querySelectorAll("#order-items-tbody tr").forEach((row) => {
        const itemId = row.dataset.itemId
        const quantity = Number.parseInt(row.querySelector(".quantity-edit").value)
        const unitPrice = Number.parseFloat(row.querySelector(".item-price").textContent.replace("$", ""))

        orderItems.push({
            itemId: itemId,
            quantity: quantity,
            unitPrice: unitPrice,
        })
    })

    const orderData = {
        orderId: orderId,
        items: orderItems,
        totalAmount: Number.parseFloat(document.getElementById("order-total").textContent.replace("$", "")),
    }

    fetch("/Staff/UpdateOrder", {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify(orderData),
    })
        .then((res) => res.json())
        .then((data) => {
            if (data.success) {
                showNotification("Order updated successfully", "success")
                cancelOrderEdit() // Exit edit mode
                loadOrders(currentOrderPage) // Refresh the orders table
            } else {
                showNotification("Error updating order: " + data.error, "error")
            }
        })
        .catch(() => {
            showNotification("Error updating order", "error")
        })
}

function editOrder(orderId) {
    // Open the order details panel and enable edit mode
    openPopout("orderDetailPanel", orderId)
    // Wait for the panel to load, then enable edit mode
    setTimeout(() => {
        enableOrderEdit(orderId)
    }, 500)
}

function deleteOrder(orderId) {
    const formData = new FormData()
    formData.append("orderId", orderId)

    fetch("/Staff/DeleteOrder", {
        method: "POST",
        body: formData,
        credentials: "same-origin",
    })
        .then((res) => res.json())
        .then((data) => {
            if (data.success) {
                showNotification("Order deleted successfully", "success")
                location.reload()
            } else {
                showNotification("Error deleting order: " + data.error, "error")
            }
        })
        .catch(() => showNotification("Error deleting order", "error"))
}

// ================= DELETE =================
function deleteUser(userId) {
    const formData = new FormData()
    formData.append("userId", userId)

    fetch("/Staff/DeleteUser", {
        method: "POST",
        body: formData,
        credentials: "same-origin",
    })
        .then((res) => res.json())
        .then((data) => {
            if (data.success) {
                showNotification("User deleted successfully", "success")
                location.reload()
            } else {
                showNotification("Error deleting user: " + data.error, "error")
            }
        })
        .catch(() => showNotification("Error deleting user", "error"))
}

function deleteProduct(productId) {
    const formData = new FormData()
    formData.append("id", productId)

    fetch("/Staff/DeleteProduct", {
        method: "POST",
        body: formData,
        credentials: "same-origin",
    })
        .then((res) => res.json())
        .then((data) => {
            if (data.success) {
                showNotification("Product deleted successfully", "success")
                loadProducts(currentPage) // Reload products instead of full page
            } else {
                showNotification("Error deleting product: " + data.error, "error")
            }
        })
        .catch(() => showNotification("Error deleting product", "error"))
}

// ================= USERS =================
function loadUserData(userId) {
    fetch(`/Staff/GetUserById?userId=${userId}`)
        .then((res) => res.json())
        .then((data) => {
            if (data.success) {
                const user = data.user
                document.getElementById("user-id").value = user.userId
                document.getElementById("user-name").value = user.name
                document.getElementById("user-email").value = user.email
                document.getElementById("user-role").value = user.role

                document.getElementById("editUserPanelTitle").textContent = "Edit User"
            } else {
                showNotification("Error loading user: " + data.error, "error")
            }
        })
        .catch(() => showNotification("Error loading user", "error"))
}

function showAddUserPanel() {
    document.getElementById("user-form").reset()
    document.getElementById("user-id").value = ""

    // Show backdrop when opening add user panel
    const backdrop = document.getElementById("popout-backdrop")
    if (backdrop) {
        backdrop.classList.add("open")
    }

    openPopout("editUserPanel")
    document.getElementById("editUserPanelTitle").textContent = "Add New User"
}

function saveUser(event) {
    event.preventDefault()

    const userId = document.getElementById("user-id").value
    const name = document.getElementById("user-name").value
    const email = document.getElementById("user-email").value
    const role = document.getElementById("user-role").value

    if (!name || !email || !role) {
        showNotification("Please fill in all required fields", "error")
        return
    }

    const userData = { userId, name, email, role }

    fetch("/Staff/UpdateUser", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(userData),
    })
        .then((res) => res.json())
        .then((data) => {
            if (data.success) {
                showNotification("User saved successfully", "success")
                closePopout("editUserPanel")
                location.reload()
            } else {
                showNotification("Error saving user: " + data.error, "error")
            }
        })
        .catch(() => {
            showNotification("Error saving user", "error")
        })
}

// ================= PRODUCTS =================
function loadProductData(productId) {
    fetch(`/Staff/GetProductById?id=${productId}`)
        .then((res) => res.json())
        .then((data) => {
            if (data.success) {
                const p = data.product
                document.getElementById("product-id").value = p.id
                document.getElementById("product-name").value = p.name
                document.getElementById("product-price").value = p.price
                document.getElementById("product-description").value = p.description
                document.getElementById("product-category").value = p.category
                document.getElementById("product-imagePath").value = p.imagePath

                document.getElementById("editProductPanelTitle").textContent = "Edit Product"
            } else {
                showNotification("Error loading product: " + data.error, "error")
            }
        })
        .catch(() => showNotification("Error loading product", "error"))
}

function showAddProductPanel() {
    document.getElementById("product-form").reset()
    document.getElementById("product-id").value = ""
    document.getElementById("editProductPanelTitle").textContent = "Add New Product"

    const panel = document.getElementById("editProductPanel")
    openPopout("editProductPanel")
}

function saveProduct(event) {
    event.preventDefault()

    const product = {
        id: document.getElementById("product-id").value,
        name: document.getElementById("product-name").value,
        price: Number.parseFloat(document.getElementById("product-price").value),
        description: document.getElementById("product-description").value,
        category: document.getElementById("product-category").value,
        imagePath: document.getElementById("product-imagePath").value,
    }

    if (!product.name || !product.price || !product.category < 0) {
        showNotification("Please fill in all required fields with valid values", "error")
        return
    }

    const url = product.id ? "/Staff/UpdateProduct" : "/Staff/AddProduct"

    fetch(url, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(product),
    })
        .then((res) => res.json())
        .then((data) => {
            if (data.success) {
                showNotification("Product saved successfully", "success")
                closePopout("editProductPanel")
                loadProducts(currentPage) // Reload products instead of full page
            } else {
                showNotification("Error saving product: " + data.error, "error")
            }
        })
        .catch(() => {
            showNotification("Error saving product", "error")
        })
}

// ================= PRODUCT STATUS TOGGLE =================
function toggleProductStatus(productId, disable, buttonElement) {
    const requestData = {
        ProductId: productId,
        Disable: disable,
    }

    fetch("/Staff/ToggleProductDisable", {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
            RequestVerificationToken: getCsrfToken(),
        },
        body: JSON.stringify(requestData),
    })
        .then((res) => res.json())
        .then((data) => {
            if (data.success) {
                const row = document.getElementById("product-row-" + productId)
                const button = buttonElement

                if (data.isDisabled) {
                    // Product is now disabled
                    row.classList.add("product-disabled")
                    button.classList.remove("enabled")
                    button.classList.add("disabled")
                    button.textContent = "Disabled"
                    button.onclick = () => toggleProductStatus(productId, false, button)
                    showNotification("Product disabled - members cannot order this item", "success")
                } else {
                    // Product is now enabled
                    row.classList.remove("product-disabled")
                    button.classList.remove("disabled")
                    button.classList.add("enabled")
                    button.textContent = "Enabled"
                    button.onclick = () => toggleProductStatus(productId, true, button)
                    showNotification("Product enabled - members can now order this item", "success")
                }
            } else {
                showNotification("Error updating product status: " + data.error, "error")
            }
        })
        .catch(() => {
            showNotification("Error updating product status", "error")
        })
}

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

// ================= UTILITY FUNCTIONS =================
function getCsrfToken() {
    const token = document.querySelector('input[name="__RequestVerificationToken"]')
    return token ? token.value : ""
}

// ================= PRODUCT PAGINATION AND SORTING =================
let currentPage = 1
let currentPageSize = 20
let currentSortBy = "name"
let currentSortOrder = "asc"
let currentCategory = "all"

function loadProducts(page = 1) {
    currentPage = page
    currentPageSize = 20
    currentSortBy = document.getElementById("sortBy")?.value || "name"
    currentSortOrder = document.getElementById("sortOrder")?.value || "asc"
    currentCategory = document.getElementById("categoryFilter")?.value || "all"

    // Show loading state
    const tbody = document.getElementById("products-table-body")
    if (tbody) {
        tbody.innerHTML =
            '<tr><td colspan="7" style="text-align: center; padding: 40px; color: #6c757d;"><div style="display: inline-flex; align-items: center; gap: 10px;"><div style="width: 20px; height: 20px; border: 2px solid #007bff; border-top: 2px solid transparent; border-radius: 50%; animation: spin 1s linear infinite;"></div>Loading products...</div></td></tr>'
    }

    const params = new URLSearchParams({
        page: currentPage,
        pageSize: currentPageSize,
        sortBy: currentSortBy,
        sortOrder: currentSortOrder,
        category: currentCategory,
    })

    fetch(`/Staff/GetProductsData?${params}`)
        .then((res) => res.json())
        .then((data) => {
            if (data.success) {
                updateProductsTable(data.products)
                updatePagination(data.currentPage, data.totalPages, data.totalCount)
            } else {
                showNotification("Error loading products: " + data.error, "error")
                if (tbody) {
                    tbody.innerHTML =
                        '<tr><td colspan="7" style="text-align: center; padding: 40px; color: #dc3545;">Error loading products. Please try again.</td></tr>'
                }
            }
        })
        .catch(() => {
            showNotification("Error loading products", "error")
            if (tbody) {
                tbody.innerHTML =
                    '<tr><td colspan="7" style="text-align: center; padding: 40px; color: #dc3545;">Network error. Please check your connection and try again.</td></tr>'
            }
        })
}

function updateProductsTable(products) {
    const tbody = document.getElementById("products-table-body")
    if (!tbody) return

    tbody.innerHTML = ""

    products.forEach((product) => {
        const row = document.createElement("tr")
        row.className = product.isDisabled ? "product-disabled" : ""
        row.id = `product-row-${product.id}`

        row.innerHTML = `
            <td>${product.id.substring(0, 8).toUpperCase()}</td>
            <td>${product.category}</td>
            <td>${product.name}</td>
            <td>$${product.price.toFixed(2)}</td>
            <td>
                <button class="btn-toggle-status ${product.isDisabled ? "disabled" : "enabled"}"
                        onclick="toggleProductStatus('${product.id}', ${!product.isDisabled}, this)">
                    ${product.isDisabled ? "Disabled" : "Enabled"}
                </button>
            </td>
            <td>
                <button class="btn btn-sm btn-warning" onclick="openPopout('editProductPanel','${product.id}')">Edit</button>
                <button class="btn btn-sm btn-danger" onclick="confirmDelete('product', '${product.id}')">Delete</button>
            </td>
        `

        tbody.appendChild(row)
    })
}

function updatePagination(currentPage, totalPages, totalCount) {
    const paginationInfo = document.getElementById("pagination-info")
    const prevButton = document.getElementById("prev-page")
    const nextButton = document.getElementById("next-page")
    const pageNumbers = document.getElementById("page-numbers")

    if (paginationInfo) {
        const startItem = (currentPage - 1) * currentPageSize + 1
        const endItem = Math.min(currentPage * currentPageSize, totalCount)
        paginationInfo.textContent = `Showing ${startItem}-${endItem} of ${totalCount} products`
    }

    if (prevButton) {
        prevButton.disabled = currentPage <= 1
    }

    if (nextButton) {
        nextButton.disabled = currentPage >= totalPages
    }

    if (pageNumbers) {
        pageNumbers.innerHTML = ""
        const maxVisiblePages = 5
        let startPage = Math.max(1, currentPage - Math.floor(maxVisiblePages / 2))
        const endPage = Math.min(totalPages, startPage + maxVisiblePages - 1)

        if (endPage - startPage + 1 < maxVisiblePages) {
            startPage = Math.max(1, endPage - maxVisiblePages + 1)
        }

        for (let i = startPage; i <= endPage; i++) {
            const pageBtn = document.createElement("button")
            pageBtn.className = `page-btn ${i === currentPage ? "active" : ""}`
            pageBtn.textContent = i
            pageBtn.onclick = () => loadProducts(i)
            pageNumbers.appendChild(pageBtn)
        }
    }
}

function changePage(direction) {
    const newPage = currentPage + direction
    if (newPage >= 1) {
        loadProducts(newPage)
    }
}

// ================= ORDER PAGINATION AND SORTING =================
let currentOrderPage = 1
let currentOrderPageSize = 20
let currentOrderSortBy = "date"
let currentOrderSortOrder = "desc"
let currentOrderStatus = "all"

function loadOrders(page = 1) {
    currentOrderPage = page
    currentOrderPageSize = 20
    currentOrderSortBy = document.getElementById("orderSortBy")?.value || "date"
    currentOrderSortOrder = document.getElementById("orderSortOrder")?.value || "desc"
    currentOrderStatus = document.getElementById("statusFilter")?.value || "all"

    // Show loading state
    const tbody = document.getElementById("orders-table-body")
    if (tbody) {
        tbody.innerHTML =
            '<tr><td colspan="6" style="text-align: center; padding: 40px; color: #6c757d;"><div style="display: inline-flex; align-items: center; gap: 10px;"><div style="width: 20px; height: 20px; border: 2px solid #007bff; border-top: 2px solid transparent; border-radius: 50%; animation: spin 1s linear infinite;"></div>Loading orders...</div></td></tr>'
    }

    const params = new URLSearchParams({
        page: currentOrderPage,
        pageSize: currentOrderPageSize,
        sortBy: currentOrderSortBy,
        sortOrder: currentOrderSortOrder,
        status: currentOrderStatus,
    })

    fetch(`/Staff/GetOrdersData?${params}`)
        .then((res) => res.json())
        .then((data) => {
            if (data.success) {
                updateOrdersTable(data.orders)
                updateOrderPagination(data.currentPage, data.totalPages, data.totalCount)
            } else {
                showNotification("Error loading orders: " + data.error, "error")
                if (tbody) {
                    tbody.innerHTML =
                        '<tr><td colspan="6" style="text-align: center; padding: 40px; color: #dc3545;">Error loading orders. Please try again.</td></tr>'
                }
            }
        })
        .catch(() => {
            showNotification("Error loading orders", "error")
            if (tbody) {
                tbody.innerHTML =
                    '<tr><td colspan="6" style="text-align: center; padding: 40px; color: #dc3545;">Network error. Please check your connection and try again.</td></tr>'
            }
        })
}

function updateOrdersTable(orders) {
    const tbody = document.getElementById("orders-table-body")
    if (!tbody) return

    tbody.innerHTML = ""

    orders.forEach((order) => {
        const row = document.createElement("tr")
        row.setAttribute("data-order-id", order.orderId)

        row.innerHTML = `
            <td>${order.orderId.substring(0, 8).toUpperCase()}</td>
            <td>${order.user?.name || "Guest"}</td>
            <td>${new Date(order.orderDate).toLocaleDateString("en-US", {
            month: "short",
            day: "numeric",
            year: "numeric",
        })}</td>
            <td>
                <div class="status-dropdown">
                    <div class="status-badge-clickable ${order.status}" onclick="toggleStatusDropdown('${order.orderId}')">
                        ${order.status.toUpperCase()}
                    </div>
                    <div class="status-dropdown-menu" id="status-menu-${order.orderId}">
                        <div class="status-dropdown-item" onclick="updateOrderStatus('${order.orderId}', 'pending')">Pending</div>
                        <div class="status-dropdown-item" onclick="updateOrderStatus('${order.orderId}', 'preparing')">Preparing</div>
                        <div class="status-dropdown-item" onclick="updateOrderStatus('${order.orderId}', 'ready')">Ready</div>
                        <div class="status-dropdown-item" onclick="updateOrderStatus('${order.orderId}', 'completed')">Completed</div>
                        <div class="status-dropdown-item" onclick="updateOrderStatus('${order.orderId}', 'cancelled')">Cancelled</div>
                    </div>
                </div>
            </td>
            <td style="font-weight: 600; color: #28a745;">$${order.totalAmount.toFixed(2)}</td>
            <td>
                <button onclick="openPopout('orderDetailPanel', '${order.orderId}')" class="btn btn-info btn-sm">
                    View Details
                </button>
            </td>
        `
        tbody.appendChild(row)
    })
}

function updateOrderPagination(currentPage, totalPages, totalCount) {
    const paginationInfo = document.getElementById("order-pagination-info")
    const prevButton = document.getElementById("order-prev-page")
    const nextButton = document.getElementById("order-next-page")
    const pageNumbers = document.getElementById("order-page-numbers")

    if (paginationInfo) {
        const startItem = (currentPage - 1) * currentOrderPageSize + 1
        const endItem = Math.min(currentPage * currentOrderPageSize, totalCount)
        paginationInfo.textContent = `Showing ${startItem}-${endItem} of ${totalCount} orders`
    }

    if (prevButton) {
        prevButton.disabled = currentPage <= 1
    }

    if (nextButton) {
        nextButton.disabled = currentPage >= totalPages
    }

    if (pageNumbers) {
        pageNumbers.innerHTML = ""
        const maxVisiblePages = 5
        let startPage = Math.max(1, currentPage - Math.floor(maxVisiblePages / 2))
        const endPage = Math.min(totalPages, startPage + maxVisiblePages - 1)

        if (endPage - startPage + 1 < maxVisiblePages) {
            startPage = Math.max(1, endPage - maxVisiblePages + 1)
        }

        for (let i = startPage; i <= endPage; i++) {
            const pageBtn = document.createElement("button")
            pageBtn.className = `page-btn ${i === currentPage ? "active" : ""}`
            pageBtn.textContent = i
            pageBtn.onclick = () => loadOrders(i)
            pageNumbers.appendChild(pageBtn)
        }
    }
}

function changeOrderPage(direction) {
    const newPage = currentOrderPage + direction
    if (newPage >= 1) {
        loadOrders(newPage)
    }
}

// ================= USER PAGINATION AND SORTING =================
let currentUserPage = 1
let currentUserPageSize = 20
let currentUserSortBy = "name"
let currentUserSortOrder = "asc"
let currentUserRole = "all"

function loadUsers(page = 1) {
    currentUserPage = page
    currentUserPageSize = 20
    currentUserSortBy = document.getElementById("userSortBy")?.value || "name"
    currentUserSortOrder = document.getElementById("userSortOrder")?.value || "asc"
    currentUserRole = document.getElementById("roleFilter")?.value || "all"

    // Show loading state
    const tbody = document.getElementById("users-table-body")
    if (tbody) {
        tbody.innerHTML =
            '<tr><td colspan="5" style="text-align: center; padding: 40px; color: #6c757d;"><div style="display: inline-flex; align-items: center; gap: 10px;"><div style="width: 20px; height: 20px; border: 2px solid #007bff; border-top: 2px solid transparent; border-radius: 50%; animation: spin 1s linear infinite;"></div>Loading users...</div></td></tr>'
    }

    const params = new URLSearchParams({
        page: currentUserPage,
        pageSize: currentUserPageSize,
        sortBy: currentUserSortBy,
        sortOrder: currentUserSortOrder,
        role: currentUserRole,
    })

    fetch(`/Staff/GetUsersData?${params}`)
        .then((res) => res.json())
        .then((data) => {
            if (data.success) {
                updateUsersTable(data.users)
                updateUserPagination(data.currentPage, data.totalPages, data.totalCount)
            } else {
                showNotification("Error loading users: " + data.error, "error")
                if (tbody) {
                    tbody.innerHTML =
                        '<tr><td colspan="5" style="text-align: center; padding: 40px; color: #dc3545;">Error loading users. Please try again.</td></tr>'
                }
            }
        })
        .catch(() => {
            showNotification("Error loading users", "error")
            if (tbody) {
                tbody.innerHTML =
                    '<tr><td colspan="5" style="text-align: center; padding: 40px; color: #dc3545;">Network error. Please check your connection and try again.</td></tr>'
            }
        })
}

function updateUsersTable(users) {
    const tbody = document.getElementById("users-table-body")
    if (!tbody) return

    tbody.innerHTML = ""

    users.forEach((user) => {
        const row = document.createElement("tr")
        row.id = `user-row-${user.userId}`

        row.innerHTML = `
            <td>${user.userId}</td>
            <td>${user.name}</td>
            <td>${user.email}</td>
            <td><span class="role-badge role-${user.role}">${user.role.toUpperCase()}</span></td>
            <td>
                <button class="btn btn-sm btn-warning" onclick="openPopout('editUserPanel','${user.userId}')">Edit</button>
                <button class="btn btn-sm btn-danger" onclick="confirmDelete('user', '${user.userId}')">Delete</button>
            </td>
        `

        tbody.appendChild(row)
    })
}

function updateUserPagination(currentPage, totalPages, totalCount) {
    const paginationInfo = document.getElementById("user-pagination-info")
    const prevButton = document.getElementById("user-prev-page")
    const nextButton = document.getElementById("user-next-page")
    const pageNumbers = document.getElementById("user-page-numbers")

    if (paginationInfo) {
        const startItem = (currentPage - 1) * currentUserPageSize + 1
        const endItem = Math.min(currentPage * currentUserPageSize, totalCount)
        paginationInfo.textContent = `Showing ${startItem}-${endItem} of ${totalCount} users`
    }

    if (prevButton) {
        prevButton.disabled = currentPage <= 1
    }

    if (nextButton) {
        nextButton.disabled = currentPage >= totalPages
    }

    if (pageNumbers) {
        pageNumbers.innerHTML = ""
        const maxVisiblePages = 5
        let startPage = Math.max(1, currentPage - Math.floor(maxVisiblePages / 2))
        const endPage = Math.min(totalPages, startPage + maxVisiblePages - 1)

        if (endPage - startPage + 1 < maxVisiblePages) {
            startPage = Math.max(1, endPage - maxVisiblePages + 1)
        }

        for (let i = startPage; i <= endPage; i++) {
            const pageBtn = document.createElement("button")
            pageBtn.className = `page-btn ${i === currentPage ? "active" : ""}`
            pageBtn.textContent = i
            pageBtn.onclick = () => loadUsers(i)
            pageNumbers.appendChild(pageBtn)
        }
    }
}

function changeUserPage(direction) {
    const newPage = currentUserPage + direction
    if (newPage >= 1) {
        loadUsers(newPage)
    }
}

function searchOrders() {
    const searchTerm = document.getElementById("orderSearch").value.toLowerCase()
    const rows = document.querySelectorAll("#orders-table-body tr")

    rows.forEach((row) => {
        const customerName = row.cells[1].textContent.toLowerCase() // Customer name is in the second column
        if (customerName.includes(searchTerm)) {
            row.style.display = ""
        } else {
            row.style.display = "none"
        }
    })
}

function searchUsers() {
    const searchTerm = document.getElementById("userSearch").value.toLowerCase()
    const rows = document.querySelectorAll("#users-table-body tr")

    rows.forEach((row) => {
        const userName = row.cells[1].textContent.toLowerCase() // User name is in the second column
        if (userName.includes(searchTerm)) {
            row.style.display = ""
        } else {
            row.style.display = "none"
        }
    })
}

function searchProducts() {
    const searchTerm = document.getElementById("productSearch").value.toLowerCase()
    const rows = document.querySelectorAll("#products-table-body tr")

    rows.forEach((row) => {
        const productName = row.cells[2].textContent.toLowerCase() // Product name is in the third column
        if (productName.includes(searchTerm)) {
            row.style.display = ""
        } else {
            row.style.display = "none"
        }
    })
}

// ================= CATEGORY MANAGEMENT =================
let currentCategoryPage = 1
const currentCategoryPageSize = 20

function loadCategories(page = 1) {
    currentCategoryPage = page

    const tbody = document.getElementById("categories-table-body")
    if (tbody) {
        tbody.innerHTML =
            '<tr><td colspan="7" style="text-align: center; padding: 40px; color: #6c757d;"><div style="display: inline-flex; align-items: center; gap: 10px;"><div style="width: 20px; height: 20px; border: 2px solid #007bff; border-top: 2px solid transparent; border-radius: 50%; animation: spin 1s linear infinite;"></div>Loading categories...</div></td></tr>'
    }

    const params = new URLSearchParams({
        page: currentCategoryPage,
        pageSize: currentCategoryPageSize,
    })

    fetch(`/Staff/GetCategoriesData?${params}`)
        .then((res) => res.json())
        .then((data) => {
            if (data.success) {
                updateCategoriesTable(data.categories)
                updateCategoryPagination(data.currentPage, data.totalPages, data.totalCount)
            } else {
                showNotification("Error loading categories: " + data.error, "error")
            }
        })
        .catch(() => {
            showNotification("Error loading categories", "error")
        })
}

function updateCategoriesTable(categories) {
    const tbody = document.getElementById("categories-table-body")
    if (!tbody) return

    tbody.innerHTML = ""

    categories.forEach((category) => {
        const row = document.createElement("tr")
        row.id = `category-row-${category.id}`

        row.innerHTML = `
            <td>${category.id}</td>
            <td>${category.name}</td>
            <td>${category.prefix}</td>
            <td>${category.description || "N/A"}</td>
            <td><span class="btn-toggle-status ${category.isActive ? "enabled" : "disabled"}">${category.isActive ? "Active" : "Inactive"}</span></td>
            <td>${new Date(category.createdAt).toLocaleDateString()}</td>
            <td>
                <button class="btn btn-sm btn-warning" onclick="editCategory('${category.id}')">Edit</button>
                <button class="btn btn-sm btn-danger" onclick="confirmDelete('category', '${category.id}')">Delete</button>
            </td>
        `

        tbody.appendChild(row)
    })
}

function updateCategoryPagination(currentPage, totalPages, totalCount) {
    const paginationInfo = document.getElementById("category-pagination-info")
    const prevButton = document.getElementById("category-prev-page")
    const nextButton = document.getElementById("category-next-page")
    const pageNumbers = document.getElementById("category-page-numbers")

    if (paginationInfo) {
        const startItem = (currentPage - 1) * currentCategoryPageSize + 1
        const endItem = Math.min(currentPage * currentCategoryPageSize, totalCount)
        paginationInfo.textContent = `Showing ${startItem}-${endItem} of ${totalCount} categories`
    }

    if (prevButton) {
        prevButton.disabled = currentPage <= 1
    }

    if (nextButton) {
        nextButton.disabled = currentPage >= totalPages
    }

    if (pageNumbers) {
        pageNumbers.innerHTML = ""
        for (let i = 1; i <= totalPages; i++) {
            const pageBtn = document.createElement("button")
            pageBtn.className = `page-btn ${i === currentPage ? "active" : ""}`
            pageBtn.textContent = i
            pageBtn.onclick = () => loadCategories(i)
            pageNumbers.appendChild(pageBtn)
        }
    }
}

function changeCategoryPage(direction) {
    const newPage = currentCategoryPage + direction
    if (newPage >= 1) {
        loadCategories(newPage)
    }
}

function editCategory(categoryId) {
    fetch(`/Staff/GetCategoryById?id=${categoryId}`)
        .then((res) => res.json())
        .then((data) => {
            if (data.success) {
                const category = data.category
                document.getElementById("edit-category-id").value = category.id
                document.getElementById("edit-category-name").value = category.name
                document.getElementById("edit-category-prefix").value = category.prefix
                document.getElementById("edit-category-description").value = category.description || ""
                document.getElementById("edit-category-active").checked = category.isActive

                openPopout("editCategoryPanel")
            } else {
                showNotification("Error loading category: " + data.error, "error")
            }
        })
        .catch(() => showNotification("Error loading category", "error"))
}

function deleteCategory(categoryId) {
    const formData = new FormData()
    formData.append("id", categoryId)

    fetch("/Staff/DeleteCategory", {
        method: "POST",
        body: formData,
        credentials: "same-origin",
    })
        .then((res) => res.json())
        .then((data) => {
            if (data.success) {
                showNotification("Category deleted successfully", "success")
                loadCategories(currentCategoryPage) // Refresh the categories table
            } else {
                showNotification("Error deleting category: " + data.error, "error")
            }
        })
        .catch(() => showNotification("Error deleting category", "error"))
}

// Initialize the page
document.addEventListener("DOMContentLoaded", () => {
    showTab("orders")
    loadOrders()
})
