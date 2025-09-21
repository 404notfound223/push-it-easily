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

// ================= SEARCH FILTERS =================
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