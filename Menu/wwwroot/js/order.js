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

function checkUserMemberStatus() {
    // Check if user is logged in and is a member
    const userRole =
        document.querySelector('meta[name="user-role"]')?.content || sessionStorage.getItem("userRole") || "guest"
    return userRole.toLowerCase() === "member"
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

    const userRole = document.querySelector('meta[name="user-role"]')?.content || "guest"
    const isMember = userRole.toLowerCase() === "member"

    const taxRate = 0.085
    const tax = subtotal * taxRate
    const memberDiscount = isMember ? subtotal * 0.1 : 0
    const total = subtotal + tax - memberDiscount

    // Update summary
    const subtotalElement = document.getElementById("order-subtotal")
    const taxElement = document.getElementById("order-tax")
    const totalElement = document.getElementById("order-total")
    const memberDiscountElement = document.getElementById("member-discount")
    const memberDiscountRow = document.getElementById("member-discount-row")

    if (subtotalElement) subtotalElement.textContent = `$${subtotal.toFixed(2)}`
    if (taxElement) taxElement.textContent = `$${tax.toFixed(2)}`
    if (totalElement) totalElement.textContent = `$${total.toFixed(2)}`

    if (memberDiscountElement && memberDiscountRow) {
        if (isMember && memberDiscount > 0) {
            memberDiscountElement.textContent = `-$${memberDiscount.toFixed(2)}`
            memberDiscountRow.style.display = "flex"
        } else {
            memberDiscountRow.style.display = "none"
        }
    }
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

    const userRole = document.querySelector('meta[name="user-role"]')?.content || "guest"
    const isMember = userRole.toLowerCase() === "member"
    const memberDiscount = isMember ? subtotal * 0.1 : 0

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
          ${memberDiscount > 0
            ? `  
          <div class="summary-row member-discount-row">
            <span>Member Discount (10%):</span>
            <span class="discount-amount">-$${memberDiscount.toFixed(2)}</span>
          </div>
          `
            : ""
        }
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

                    let message = `Order placed successfully! Payment Number: ${data.paymentNumber}. Please present this number at the counter to complete your payment.`

                    if (data.memberDiscount && data.memberDiscount > 0) {
                        message += ` You saved $${data.memberDiscount.toFixed(2)} with your member discount!`
                    }

                    showNotification(message, "info", 10000)

                    showPaymentNumberModal(data.paymentNumber, data.orderId, data.memberDiscount || 0)
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
    window.location.href = `/Order/MemberOrderView/${orderId}`
}

function clearOrder() {
    localStorage.removeItem("orderItems")
    renderOrder()
    updateOrderCount()
    showNotification("Order cleared!", "info")
}

function clearOrdersOnMenuExit() {
    const isLoggedIn = document.querySelector(".user-welcome") !== null
    if (!isLoggedIn) {
        // Clear orders when navigating to login page
        document.body.addEventListener("click", (e) => {
            const target = e.target.closest("a")
            if (target && target.getAttribute("href")) {
                const href = target.getAttribute("href").toLowerCase()
                if (href.includes("/login") || href.includes("/register")) {
                    const orderItems = localStorage.getItem("orderItems")
                    if (orderItems && JSON.parse(orderItems).length > 0) {
                        if (
                            confirm(
                                "You have items in your order. Proceeding to login will clear your current order. Do you want to continue?",
                            )
                        ) {
                            localStorage.removeItem("orderItems")
                            updateOrderCount()
                        } else {
                            e.preventDefault()
                            return false
                        }
                    }
                }
            }
        })

        // Also clear orders when login page loads (in case user navigated directly)
        if (window.location.pathname.toLowerCase().includes("/login")) {
            const orderItems = localStorage.getItem("orderItems")
            if (orderItems && JSON.parse(orderItems).length > 0) {
                localStorage.removeItem("orderItems")
                updateOrderCount()
                showNotification("Your previous order has been cleared.", "info")
            }
        }
    }
}

// Initialize order count on page load
document.addEventListener("DOMContentLoaded", () => {
    updateOrderCount()
    clearOrdersOnMenuExit()
})

function showPaymentNumberModal(paymentNumber, orderId, memberDiscount) {
    const currentDate = new Date().toLocaleDateString()
    const currentTime = new Date().toLocaleTimeString()

    const modal = `
    <div id="payment-number-modal" class="modal" style="display: block;">
      <div class="modal-content payment-number-modal-content">
        <div class="payment-receipt" id="payment-receipt">
          <div class="receipt-header">
            <h2>The Secret Restaurant</h2>
            <p>Payment Receipt</p>
            <hr>
          </div>
          <div class="receipt-body">
            <div class="receipt-row">
              <span>Date:</span>
              <span>${currentDate}</span>
            </div>
            <div class="receipt-row">
              <span>Time:</span>
              <span>${currentTime}</span>
            </div>
            <div class="receipt-row">
              <span>Order ID:</span>
              <span>${orderId}</span>
            </div>
            <div class="receipt-row payment-number-row">
              <span><strong>Payment Number:</strong></span>
              <span class="payment-number"><strong>${paymentNumber}</strong></span>
            </div>
            ${memberDiscount > 0
            ? `  
            <div class="receipt-row discount-row">
              <span>Member Discount:</span>
              <span class="discount-amount">-$${memberDiscount.toFixed(2)}</span>
            </div>
            `
            : ""
        }
            <hr>
            <p class="receipt-instructions">
              Please present this payment number at the counter to complete your payment.
              Keep this receipt for your records.
            </p>
          </div>
        </div>
        <div class="modal-actions">
          <button onclick="printPaymentNumber()" class="btn btn-primary">
            <i class="fas fa-print"></i> Print Receipt
          </button>
          <button onclick="closePaymentNumberModalAndContinue()" class="btn btn-secondary">
            Close
          </button>
        </div>
      </div>
    </div>
  `

    document.body.insertAdjacentHTML("beforeend", modal)
}

function closePaymentNumberModalAndContinue() {
    const modal = document.getElementById("payment-number-modal")
    if (modal) {
        modal.remove()
    }

    // Show continue order prompt after modal is closed
    setTimeout(() => {
        showContinueOrderPrompt()
    }, 500)
}

function closePaymentNumberModal() {
    const modal = document.getElementById("payment-number-modal")
    if (modal) {
        modal.remove()
    }
}

function showContinueOrderPrompt() {
    const isLoggedIn = checkIfUserIsLoggedIn()

    if (isLoggedIn) {
        if (confirm("Would you like to create another order?")) {
            window.location.href = "/Menu/All"
        } else {
            window.location.href = "/Order/OrderHistory"
        }
    } else {
        // Guest user - just ask if they want to create another order
        if (confirm("Would you like to create another order?")) {
            window.location.href = "/Menu/All"
        } else {
            window.location.href = "/"
        }
    }
}

function checkIfUserIsLoggedIn() {
    // Check multiple indicators of logged in status
    const profileButton = document.querySelector(".btn-profile")
    const userWelcome = document.querySelector(".user-welcome")
    const logoutButton = document.querySelector(".btn-logout")

    return profileButton !== null || userWelcome !== null || logoutButton !== null
}

function printPaymentNumber() {
    const receiptContent = document.getElementById("payment-receipt").innerHTML
    const printWindow = window.open("", "_blank")

    printWindow.document.write(`  
    <!DOCTYPE html>
    <html>
    <head>
      <title>Payment Receipt</title>
      <style>
        body {
          font-family: 'Courier New', monospace;
          max-width: 300px;
          margin: 0 auto;
          padding: 20px;
          line-height: 1.4;
        }
        .receipt-header {
          text-align: center;
          margin-bottom: 20px;
        }
        .receipt-header h2 {
          margin: 0;
          font-size: 18px;
          font-weight: bold;
        }
        .receipt-header p {
          margin: 5px 0;
          font-size: 14px;
        }
        .receipt-row {
          display: flex;
          justify-content: space-between;
          margin: 8px 0;
          font-size: 12px;
        }
        .payment-number-row {
          font-size: 14px;
          font-weight: bold;
          background: #f0f0f0;
          padding: 8px;
          margin: 15px 0;
        }
        .discount-row {
          color: #28a745;
          font-weight: bold;
        }
        .receipt-instructions {
          font-size: 11px;
          text-align: center;
          margin-top: 15px;
          padding: 10px;
          border: 1px dashed #ccc;
        }
        hr {
          border: none;
          border-top: 1px dashed #333;
          margin: 15px 0;
        }
        @media print {
          body {
            margin: 0;
            padding: 10px;
          }
        }
      </style>
    </head>
    <body>
      ${receiptContent}
    </body>
    </html>
  `)

    printWindow.document.close()
    printWindow.focus()
    printWindow.print()

    printWindow.onafterprint = () => {
        printWindow.close()
        // Small delay to ensure print dialog is fully closed
        setTimeout(() => {
            showContinueOrderPrompt()
        }, 1000)
    }

    // Fallback for browsers that don't support onafterprint
    setTimeout(() => {
        if (!printWindow.closed) {
            printWindow.close()
        }
    }, 5000)
}
