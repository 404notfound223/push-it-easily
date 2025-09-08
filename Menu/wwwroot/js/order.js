function addToOrder(id, name, price) {
    const order = JSON.parse(localStorage.getItem("orderItems")) || []

    // Check if item already exists in order
    const existingItem = order.find((item) => item.id === id)
    if (existingItem) {
        existingItem.quantity += 1
    } else {
        order.push({ id, name, price: Number.parseFloat(price), quantity: 1 })
    }

    localStorage.setItem("orderItems", JSON.stringify(order))
    showNotification(name + " added to order!", "success")

    // Update order count if there's a counter element
    updateOrderCount()
}

function removeFromOrder(id) {
    let order = JSON.parse(localStorage.getItem("orderItems")) || []
    order = order.filter((item) => item.id !== id)
    localStorage.setItem("orderItems", JSON.stringify(order))
    renderOrder()
    updateOrderCount()
}

function updateQuantity(id, newQuantity) {
    const order = JSON.parse(localStorage.getItem("orderItems")) || []
    const item = order.find((item) => item.id === id)
    if (item) {
        if (newQuantity <= 0) {
            removeFromOrder(id)
        } else {
            item.quantity = newQuantity
            localStorage.setItem("orderItems", JSON.stringify(order))
            renderOrder()
        }
    }
}

function renderOrder() {
    const order = JSON.parse(localStorage.getItem("orderItems")) || []
    const orderListElement = document.getElementById("order-list")
    const emptyOrderElement = document.getElementById("empty-order")
    const orderSummaryElement = document.getElementById("order-summary")

    if (!orderListElement) return // Exit if not on order page

    let html = ""
    let subtotal = 0

    if (order.length === 0) {
        orderListElement.style.display = "none"
        if (emptyOrderElement) emptyOrderElement.style.display = "block"
        if (orderSummaryElement) orderSummaryElement.style.display = "none"
        return
    }

    orderListElement.style.display = "block"
    if (emptyOrderElement) emptyOrderElement.style.display = "none"
    if (orderSummaryElement) orderSummaryElement.style.display = "block"

    order.forEach((item) => {
        const itemTotal = item.price * item.quantity
        subtotal += itemTotal
        html += `
      <div class="order-item">
        <div class="item-info">
          <span class="item-name">${item.name}</span>
          <span class="item-price">$${item.price.toFixed(2)} each</span>
        </div>
        <div class="item-controls">
          <button onclick="updateQuantity('${item.id}', ${item.quantity - 1})" class="qty-btn">-</button>
          <span class="quantity">${item.quantity}</span>
          <button onclick="updateQuantity('${item.id}', ${item.quantity + 1})" class="qty-btn">+</button>
          <button onclick="removeFromOrder('${item.id}')" class="remove-btn">Remove</button>
        </div>
        <div class="item-total">$${itemTotal.toFixed(2)}</div>
      </div>
    `
    })

    orderListElement.innerHTML = html

    // Calculate tax and total
    const taxRate = 0.085 // 8.5% tax
    const tax = subtotal * taxRate
    const total = subtotal + tax

    // Update summary
    const subtotalElement = document.getElementById("order-subtotal")
    const taxElement = document.getElementById("order-tax")
    const totalElement = document.getElementById("order-total")

    if (subtotalElement) subtotalElement.textContent = `$${subtotal.toFixed(2)}`
    if (taxElement) taxElement.textContent = `$${tax.toFixed(2)}`
    if (totalElement) totalElement.textContent = `$${total.toFixed(2)}`
}

function updateOrderCount() {
    const order = JSON.parse(localStorage.getItem("orderItems")) || []
    const totalItems = order.reduce((sum, item) => sum + item.quantity, 0)

    const orderCountElement = document.getElementById("order-count")
    const orderCountFixedElement = document.getElementById("order-count-fixed")

    if (orderCountElement) {
        orderCountElement.textContent = totalItems
        orderCountElement.style.display = totalItems > 0 ? "inline" : "none"
    }

    if (orderCountFixedElement) {
        orderCountFixedElement.textContent = totalItems
        orderCountFixedElement.style.display = totalItems > 0 ? "inline" : "none"
    }
}

function showPaymentOptions() {
    const subtotal = Number.parseFloat(document.getElementById("order-subtotal").textContent.replace("$", ""))
    const tax = Number.parseFloat(document.getElementById("order-tax").textContent.replace("$", ""))
    const total = Number.parseFloat(document.getElementById("order-total").textContent.replace("$", ""))

    const paymentModal = `
    <div id="payment-modal" class="modal">
      <div class="modal-content payment-modal-content">
        <span class="close" onclick="closePaymentModal()">&times;</span>
        <h2>Choose Payment Method</h2>
        <div class="payment-summary">
          <div class="summary-row">
            <span>Subtotal:</span>
            <span>$${subtotal.toFixed(2)}</span>
          </div>
          <div class="summary-row">
            <span>Tax (8.5%):</span>
            <span>$${tax.toFixed(2)}</span>
          </div>
          <div class="summary-row total-row">
            <span>Total Amount:</span>
            <span>$${total.toFixed(2)}</span>
          </div>
        </div>
        <div class="payment-options">
          <button onclick="processPayment('counter')" class="btn btn-primary payment-option-btn">
            <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <path d="M3 3h18v18H3zM9 9h6v6H9z"/>
            </svg>
            <div>
              <strong>Pay at Counter</strong>
              <small>Get a payment number and pay in person</small>
            </div>
          </button>
          <button onclick="processPayment('stripe')" class="btn btn-success payment-option-btn">
            <svg width="24" height="24" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2">
              <rect x="1" y="4" width="22" height="16" rx="2" ry="2"></rect>
              <line x1="1" y1="10" x2="23" y2="10"></line>
            </svg>
            <div>
              <strong>Pay Online</strong>
              <small>Secure payment with Stripe</small>
            </div>
          </button>
        </div>
        <button onclick="closePaymentModal()" class="btn btn-secondary cancel-btn">Cancel</button>
      </div>
    </div>
  `

    document.body.insertAdjacentHTML("beforeend", paymentModal)
}

function closePaymentModal() {
    const modal = document.getElementById("payment-modal")
    if (modal) {
        modal.remove()
    }
}

function processPayment(method) {
    const order = JSON.parse(localStorage.getItem("orderItems")) || []
    const total = Number.parseFloat(document.getElementById("order-total").textContent.replace("$", ""))

    if (method === "counter") {
        // Create counter payment order
        const orderData = {
            totalAmount: total,
            items: order.map((item) => ({
                productId: item.id,
                quantity: item.quantity,
            })),
        }

        fetch("/Payment/CreateCounterPayment", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify(orderData),
        })
            .then((response) => response.json())
            .then((data) => {
                if (data.success) {
                    localStorage.removeItem("orderItems")
                    updateOrderCount()

                    showNotification(
                        `Order Number: ${data.orderNumber}. Payment Number: ${data.paymentNumber}. Please present this number at the counter to complete your payment.`,
                        "info",
                        10000,
                    )

                    setTimeout(() => {
                        // Check if user is logged in (member)
                        const isLoggedIn = document.querySelector(".user-welcome") !== null

                        if (isLoggedIn) {
                            if (confirm("Would you like to create another order?")) {
                                window.location.href = "/Menu/All"
                            } else {
                                if (confirm("Would you like to logout and return to the main menu?")) {
                                    window.location.href = "/Login/Logout"
                                } else {
                                    window.location.href = "/Menu/All"
                                }
                            }
                        } else {
                            // Guest user - just ask if they want to create another order
                            if (confirm("Would you like to create another order?")) {
                                window.location.href = "/Menu/All"
                            } else {
                                window.location.href = "/"
                            }
                        }
                    }, 3000)
                } else {
                    showNotification("Error creating order: " + data.error, "error")
                }
            })
            .catch((error) => {
                console.error("Error:", error)
                showNotification("Error creating order. Please try again.", "error")
            })
    } else if (method === "stripe") {
        // Create Stripe checkout session
        const orderData = {
            totalAmount: total,
            items: order.map((item) => ({
                productId: item.id,
                quantity: item.quantity,
            })),
        }

        showNotification("Redirecting to Stripe payment...", "info")

        fetch("/Payment/CreateCheckoutSession", {
            method: "POST",
            headers: {
                "Content-Type": "application/json",
            },
            body: JSON.stringify(orderData),
        })
            .then((response) => response.json())
            .then((data) => {
                if (data.success) {
                    localStorage.removeItem("orderItems")
                    updateOrderCount()

                    // Redirect to Stripe Checkout
                    window.location.href = data.checkoutUrl
                } else {
                    showNotification("Error creating payment session: " + data.error, "error")
                }
            })
            .catch((error) => {
                console.error("Error:", error)
                showNotification("Error creating payment session. Please try again.", "error")
            })
    }

    closePaymentModal()
}

function showNotification(message, type = "info", duration = 3000) {
    const notification = document.createElement("div")
    notification.className = `notification ${type}`
    notification.innerHTML = `
    <div class="notification-content">
      <span class="notification-message">${message}</span>
      <button class="notification-close" onclick="this.parentElement.parentElement.remove()">&times;</button>
    </div>
  `

    document.body.appendChild(notification)

    setTimeout(() => {
        if (notification.parentElement) {
            notification.remove()
        }
    }, duration)
}

function updateOrderStatus(orderId, status) {
    fetch("/Order/UpdateOrderStatus", {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
        },
        body: JSON.stringify({ orderId, status }),
    })
        .then((response) => response.json())
        .then((data) => {
            if (data.success) {
                showNotification(`Order status updated to ${status}`, "success")
                location.reload() // Refresh to show updated status
            } else {
                showNotification("Error updating order status: " + data.error, "error")
            }
        })
        .catch((error) => {
            console.error("Error:", error)
            showNotification("Error updating order status", "error")
        })
}

function viewOrderDetails(orderId) {
    window.location.href = `/Order/OrderDetails/${orderId}`
}

// clear orders when non-members wanted to exit menu page or login, register. but prompt a message to notify customer
//function clearOrdersOnMenuExit() {
//    const isLoggedIn = document.querySelector(".user-welcome") !== null

//    if (!isLoggedIn) {
//        // Only clear orders for non-members when leaving menu pages
//        window.addEventListener("beforeunload", () => {
//            const currentPath = window.location.pathname
//            if (currentPath.includes("/Menu/")) {
//                localStorage.removeItem("orderItems")
//            }
//        })
//    }
//}

// Initialize order count on page load
document.addEventListener("DOMContentLoaded", () => {
    updateOrderCount()
    //clearOrdersOnMenuExit()
})
