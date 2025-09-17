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
    const panel = document.getElementById(panelId)
    if (!panel) return

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
}

// Close popout when clicking outside
document.addEventListener("click", (event) => {
    const popouts = document.querySelectorAll(".popout-panel.open")
    popouts.forEach((popout) => {
        if (!popout.contains(event.target) && !event.target.closest('[onclick*="openPopout"]')) {
            popout.classList.remove("open")
        }
    })
})

// ================= ORDERS =================
function updateOrderStatus(orderId, status) {
    fetch("/Staff/UpdateOrderStatus", {
        method: "POST",
        headers: { "Content-Type": "application/x-www-form-urlencoded" },
        body: `orderId=${orderId}&status=${status}`,
    })
        .then((res) => res.json())
        .then((data) => {
            if (data.success) {
                showNotification("Order status updated successfully", "success")
            } else {
                showNotification("Error updating order status: " + data.error, "error")
            }
        })
        .catch(() => {
            showNotification("Error updating order status", "error")
        })
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
                        <span class="status-badge status-${order.status.toLowerCase()}">${order.status}</span>
                    </div>
                    <div class="info-row">
                        <strong>Total:</strong> 
                        <span class="total-amount">$${order.totalAmount.toFixed(2)}</span>
                    </div>
                </div>
                
                <div class="order-actions">
                    <button class="btn btn-primary" onclick="enableOrderEdit('${order.orderId}')">
                        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="margin-right: 4px;">
                            <path d="M11 4H4a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2v-7"></path>
                            <path d="M18.5 2.5a2.121 2.121 0 0 1 3 3L12 15l-4 1 1-4 9.5-9.5z"></path>
                        </svg>
                        Edit Order
                    </button>
                    <button class="btn btn-success" onclick="saveOrderChanges('${order.orderId}')" id="save-order-btn" style="display: none;">
                        <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="margin-right: 4px;">
                            <polyline points="20,6 9,17 4,12"></polyline>
                        </svg>
                        Save Changes
                    </button>
                    <button class="btn btn-secondary" onclick="cancelOrderEdit()" id="cancel-edit-btn" style="display: none;">Cancel</button>
                </div>
                
                <h4>Order Items:</h4>
                <div id="order-items-container">
                    <table class="order-items-table" id="order-items-table">
                        <thead>
                            <tr>
                                <th>Item</th><th>Qty</th><th>Unit Price</th><th>Total</th><th id="actions-header" style="display: none;">Actions</th>
                            </tr>
                        </thead>
                        <tbody id="order-items-tbody">
            `

                order.orderDetails.forEach((item, index) => {
                    detailsHtml += `
                    <tr data-item-id="${item.id || index}">
                        <td class="item-name">${item.product.name}</td>
                        <td class="item-quantity">
                            <span class="quantity-display">${item.quantity}</span>
                            <input type="number" class="quantity-edit" value="${item.quantity}" min="1" style="display: none;" onchange="updateItemTotal(this, ${item.unitPrice})">
                        </td>
                        <td class="item-price">$${item.unitPrice.toFixed(2)}</td>
                        <td class="item-total">$${(item.quantity * item.unitPrice).toFixed(2)}</td>
                        <td class="item-actions" style="display: none;">
                            <button class="btn btn-sm btn-danger" onclick="removeOrderItem(this)">Remove</button>
                        </td>
                    </tr>
                `
                })

                detailsHtml += `
                        </tbody>
                    </table>
                </div>
                
                <div class="order-summary">
                    <div class="summary-row">
                        <strong>Order Total: <span id="order-total">$${order.totalAmount.toFixed(2)}</span></strong>
                    </div>
                </div>
            `

                document.getElementById("order-details-content").innerHTML = detailsHtml
            } else {
                showNotification("Error loading order details: " + data.error, "error")
                document.getElementById("order-details-content").innerHTML = `
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
            document.getElementById("order-details-content").innerHTML = `
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

    openPopout("editProductPanel")
    document.getElementById("editProductPanelTitle").textContent = "Add New Product"
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
function showNotification(message, type) {
    const notification = document.createElement("div")
    notification.className = `notification ${type}`
    notification.textContent = message
    document.body.appendChild(notification)
    setTimeout(() => notification.remove(), 4000)
}

// ================= CONFIRM DELETE =================
function confirmDelete(type, id) {
    const typeNames = {
        order: "order",
        user: "user",
        product: "product",
    }

    const message = `Are you sure you want to delete this ${typeNames[type]}? This action cannot be undone.`

    if (confirm(message)) {
        if (type === "order") deleteOrder(id)
        else if (type === "user") deleteUser(id)
        else if (type === "product") deleteProduct(id)
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
    const pageNumbers = document.getElementById("page-numbers")
    const prevButton = document.getElementById("prev-page")
    const nextButton = document.getElementById("next-page")

    if (paginationInfo) {
        const startItem = (currentPage - 1) * currentPageSize + 1
        const endItem = Math.min(currentPage * currentPageSize, totalCount)
        paginationInfo.innerHTML = `<strong>Showing ${startItem}-${endItem}</strong> of <strong>${totalCount}</strong> products`
    }

    if (prevButton) {
        prevButton.disabled = currentPage <= 1
        prevButton.innerHTML = `
      <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="margin-right: 4px;">
        <polyline points="15,18 9,12 15,6"></polyline>
      </svg>
      Previous
    `
    }

    if (nextButton) {
        nextButton.disabled = currentPage >= totalPages
        nextButton.innerHTML = `
      Next
      <svg width="16" height="16" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" style="margin-left: 4px;">
        <polyline points="9,18 15,12 9,6"></polyline>
      </svg>
    `
    }

    if (pageNumbers) {
        pageNumbers.innerHTML = ""

        // Show page numbers with better visual design
        const startPage = Math.max(1, currentPage - 2)
        const endPage = Math.min(totalPages, startPage + 4)

        // Add first page and ellipsis if needed
        if (startPage > 1) {
            const firstPageBtn = document.createElement("button")
            firstPageBtn.textContent = "1"
            firstPageBtn.className = "page-btn"
            firstPageBtn.onclick = () => loadProducts(1)
            pageNumbers.appendChild(firstPageBtn)

            if (startPage > 2) {
                const ellipsis = document.createElement("span")
                ellipsis.textContent = "..."
                ellipsis.style.padding = "8px 4px"
                ellipsis.style.color = "#6c757d"
                pageNumbers.appendChild(ellipsis)
            }
        }

        // Add page numbers
        for (let i = startPage; i <= endPage; i++) {
            const pageButton = document.createElement("button")
            pageButton.textContent = i
            pageButton.className = `page-btn ${i === currentPage ? "active" : ""}`
            pageButton.onclick = () => loadProducts(i)
            pageNumbers.appendChild(pageButton)
        }

        // Add last page and ellipsis if needed
        if (endPage < totalPages) {
            if (endPage < totalPages - 1) {
                const ellipsis = document.createElement("span")
                ellipsis.textContent = "..."
                ellipsis.style.padding = "8px 4px"
                ellipsis.style.color = "#6c757d"
                pageNumbers.appendChild(ellipsis)
            }

            const lastPageBtn = document.createElement("button")
            lastPageBtn.textContent = totalPages
            lastPageBtn.className = "page-btn"
            lastPageBtn.onclick = () => loadProducts(totalPages)
            pageNumbers.appendChild(lastPageBtn)
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
        row.innerHTML = `
            <td>${order.orderId.substring(0, 8).toUpperCase()}</td>
            <td>${order.user?.name || "Guest"}</td>
            <td>${new Date(order.orderDate).toLocaleDateString("en-US", {
            month: "short",
            day: "numeric",
            year: "numeric",
            hour: "2-digit",
            minute: "2-digit",
        })}</td>
            <td>$${order.totalAmount.toFixed(2)}</td>
            <td>
                <select class="status-select" onchange="updateOrderStatus('${order.orderId}', this.value)">
                    <option value="Pending" ${order.status === "Pending" ? "selected" : ""}>Pending</option>
                    <option value="Preparing" ${order.status === "Preparing" ? "selected" : ""}>Preparing</option>
                    <option value="Ready" ${order.status === "Ready" ? "selected" : ""}>Ready</option>
                    <option value="Completed" ${order.status === "Completed" ? "selected" : ""}>Completed</option>
                    <option value="Cancelled" ${order.status === "Cancelled" ? "selected" : ""}>Cancelled</option>
                </select>
            </td>
            <td>
                <button class="btn btn-sm btn-info" onclick="openPopout('orderDetailPanel','${order.orderId}')">View</button>
                <button class="btn btn-sm btn-warning" onclick="editOrder('${order.orderId}')">Edit</button>
                <button class="btn btn-sm btn-danger" onclick="confirmDelete('order', '${order.orderId}')">Delete</button>
            </td>
        `
        tbody.appendChild(row)
    })
}

function updateOrderPagination(currentPage, totalPages, totalCount) {
    const paginationInfo = document.getElementById("order-pagination-info")
    const pageNumbers = document.getElementById("order-page-numbers")
    const prevButton = document.getElementById("order-prev-page")
    const nextButton = document.getElementById("order-next-page")

    if (paginationInfo) {
        const startItem = (currentPage - 1) * currentOrderPageSize + 1
        const endItem = Math.min(currentPage * currentOrderPageSize, totalCount)
        paginationInfo.innerHTML = `<strong>Showing ${startItem}-${endItem}</strong> of <strong>${totalCount}</strong> orders`
    }

    if (prevButton) {
        prevButton.disabled = currentPage <= 1
    }

    if (nextButton) {
        nextButton.disabled = currentPage >= totalPages
    }

    if (pageNumbers) {
        pageNumbers.innerHTML = ""

        const startPage = Math.max(1, currentPage - 2)
        const endPage = Math.min(totalPages, startPage + 4)

        if (startPage > 1) {
            const firstPageBtn = document.createElement("button")
            firstPageBtn.textContent = "1"
            firstPageBtn.className = "page-btn"
            firstPageBtn.onclick = () => loadOrders(1)
            pageNumbers.appendChild(firstPageBtn)

            if (startPage > 2) {
                const ellipsis = document.createElement("span")
                ellipsis.textContent = "..."
                ellipsis.style.padding = "8px 4px"
                ellipsis.style.color = "#6c757d"
                pageNumbers.appendChild(ellipsis)
            }
        }

        for (let i = startPage; i <= endPage; i++) {
            const pageButton = document.createElement("button")
            pageButton.textContent = i
            pageButton.className = `page-btn ${i === currentPage ? "active" : ""}`
            pageButton.onclick = () => loadOrders(i)
            pageNumbers.appendChild(pageButton)
        }

        if (endPage < totalPages) {
            if (endPage < totalPages - 1) {
                const ellipsis = document.createElement("span")
                ellipsis.textContent = "..."
                ellipsis.style.padding = "8px 4px"
                ellipsis.style.color = "#6c757d"
                pageNumbers.appendChild(ellipsis)
            }

            const lastPageBtn = document.createElement("button")
            lastPageBtn.textContent = totalPages
            lastPageBtn.className = "page-btn"
            lastPageBtn.onclick = () => loadOrders(totalPages)
            pageNumbers.appendChild(lastPageBtn)
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
        row.innerHTML = `
            <td>${user.userId.substring(0, 8).toUpperCase()}</td>
            <td>${user.name}</td>
            <td>${user.email}</td>
            <td><span class="role-badge role-${user.role.toLowerCase()}">${user.role}</span></td>
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
    const pageNumbers = document.getElementById("user-page-numbers")
    const prevButton = document.getElementById("user-prev-page")
    const nextButton = document.getElementById("user-next-page")

    if (paginationInfo) {
        const startItem = (currentPage - 1) * currentUserPageSize + 1
        const endItem = Math.min(currentPage * currentUserPageSize, totalCount)
        paginationInfo.innerHTML = `<strong>Showing ${startItem}-${endItem}</strong> of <strong>${totalCount}</strong> users`
    }

    if (prevButton) {
        prevButton.disabled = currentPage <= 1
    }

    if (nextButton) {
        nextButton.disabled = currentPage >= totalPages
    }

    if (pageNumbers) {
        pageNumbers.innerHTML = ""

        const startPage = Math.max(1, currentPage - 2)
        const endPage = Math.min(totalPages, startPage + 4)

        if (startPage > 1) {
            const firstPageBtn = document.createElement("button")
            firstPageBtn.textContent = "1"
            firstPageBtn.className = "page-btn"
            firstPageBtn.onclick = () => loadUsers(1)
            pageNumbers.appendChild(firstPageBtn)

            if (startPage > 2) {
                const ellipsis = document.createElement("span")
                ellipsis.textContent = "..."
                ellipsis.style.padding = "8px 4px"
                ellipsis.style.color = "#6c757d"
                pageNumbers.appendChild(ellipsis)
            }
        }

        for (let i = startPage; i <= endPage; i++) {
            const pageButton = document.createElement("button")
            pageButton.textContent = i
            pageButton.className = `page-btn ${i === currentPage ? "active" : ""}`
            pageButton.onclick = () => loadUsers(i)
            pageNumbers.appendChild(pageButton)
        }

        if (endPage < totalPages) {
            if (endPage < totalPages - 1) {
                const ellipsis = document.createElement("span")
                ellipsis.textContent = "..."
                ellipsis.style.padding = "8px 4px"
                ellipsis.style.color = "#6c757d"
                pageNumbers.appendChild(ellipsis)
            }

            const lastPageBtn = document.createElement("button")
            lastPageBtn.textContent = totalPages
            lastPageBtn.className = "page-btn"
            lastPageBtn.onclick = () => loadUsers(totalPages)
            pageNumbers.appendChild(lastPageBtn)
        }
    }
}

function changeUserPage(direction) {
    const newPage = currentUserPage + direction
    if (newPage >= 1) {
        loadUsers(newPage)
    }
}

// Initialize event listeners when DOM is loaded
document.addEventListener("DOMContentLoaded", () => {
    // Close popout when clicking the close button
    document.querySelectorAll(".close-popout").forEach((button) => {
        button.addEventListener("click", function () {
            const panelId = this.getAttribute("data-close") || this.closest(".popout-panel").id
            closePopout(panelId)
        })
    })

    // Prevent form submission from refreshing the page
    document.querySelectorAll("form").forEach((form) => {
        form.addEventListener("submit", (e) => {
            e.preventDefault()
        })
    })

    // Load orders when orders tab is shown
    const ordersTab = document.querySelector('[onclick*="orders"]')
    if (ordersTab) {
        ordersTab.addEventListener("click", () => {
            setTimeout(() => loadOrders(1), 100)
        })
    }

    const usersTab = document.querySelector('[onclick*="users"]')
    if (usersTab) {
        usersTab.addEventListener("click", () => {
            setTimeout(() => loadUsers(1), 100)
        })
    }

    // Load products when products tab is shown
    const productsTab = document.querySelector('[onclick*="products"]')
    if (productsTab) {
        productsTab.addEventListener("click", () => {
            setTimeout(() => loadProducts(1), 100)
        })
    }

    // Load orders initially if orders tab is active
    if (document.getElementById("orders-tab")?.classList.contains("active")) {
        loadOrders(1)
    }

    if (document.getElementById("users-tab")?.classList.contains("active")) {
        loadUsers(1)
    }

    // Load products initially if products tab is active
    if (document.getElementById("products-tab")?.classList.contains("active")) {
        loadProducts(1)
    }
})
