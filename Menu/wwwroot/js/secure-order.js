// Enhanced order.js with security improvements
let requestToken = null
let lastTokenFetch = 0

// Get or refresh security token
async function getSecurityToken() {
    const now = Date.now()
    // Refresh token every 30 minutes
    if (!requestToken || now - lastTokenFetch > 1800000) {
        try {
            const response = await fetch("/Payment/GetRequestToken")
            const data = await response.json()
            if (data.success) {
                requestToken = data.token
                lastTokenFetch = now
            }
        } catch (error) {
            console.error("Failed to get security token:", error)
        }
    }
    return requestToken
}

// Validate order data before submission
function validateOrderData(order, total) {
    const errors = []

    // Basic validation
    if (!order || order.length === 0) {
        errors.push("Order cannot be empty")
        return { isValid: false, errors }
    }

    if (order.length > 50) {
        errors.push("Order cannot contain more than 50 items")
    }

    let calculatedTotal = 0
    const itemCounts = {}

    for (const item of order) {
        // Validate item structure
        if (!item.id || !item.name || typeof item.price !== "number" || typeof item.quantity !== "number") {
            errors.push(`Invalid item data for ${item.name || "unknown item"}`)
            continue
        }

        // Validate quantity
        if (item.quantity < 1 || item.quantity > 99) {
            errors.push(`Invalid quantity for ${item.name}: ${item.quantity}`)
        }

        // Validate price
        if (item.price < 0.01 || item.price > 1000) {
            errors.push(`Invalid price for ${item.name}: $${item.price}`)
        }

        // Check for duplicate items (should be consolidated)
        if (itemCounts[item.id]) {
            errors.push(`Duplicate item detected: ${item.name}`)
        }
        itemCounts[item.id] = (itemCounts[item.id] || 0) + item.quantity

        calculatedTotal += item.price * item.quantity
    }

    // Calculate tax and discounts
    const userRole = document.querySelector('meta[name="user-role"]')?.content || "guest"
    const isMember = userRole.toLowerCase() === "member"
    const tax = calculatedTotal * 0.085
    const memberDiscount = isMember ? calculatedTotal * 0.1 : 0
    const finalTotal = calculatedTotal + tax - memberDiscount

    // Validate total (with small tolerance for rounding)
    if (Math.abs(total - finalTotal) > 0.02) {
        errors.push(`Total amount mismatch. Expected: $${finalTotal.toFixed(2)}, Got: $${total.toFixed(2)}`)
    }

    return {
        isValid: errors.length === 0,
        errors,
        calculatedTotal: finalTotal,
        memberDiscount,
    }
}

// Sanitize input data
function sanitizeOrderData(order) {
    return order.map((item) => ({
        productId: String(item.id).substring(0, 50), // Limit length
        quantity: Math.max(1, Math.min(99, Number.parseInt(item.quantity) || 1)), // Clamp quantity
        expectedPrice: Math.max(0.01, Math.min(1000, Number.parseFloat(item.price) || 0)), // Clamp price
    }))
}

// Enhanced processPayment function with security
async function processPayment(method) {
    const order = JSON.parse(localStorage.getItem("orderItems")) || []
    const total = Number.parseFloat(document.getElementById("order-total").textContent.replace("$", ""))

    // Validate order data
    const validation = validateOrderData(order, total)
    if (!validation.isValid) {
        showNotification(`Order validation failed: ${validation.errors.join(", ")}`, "error")
        return
    }

    // Get security token
    const token = await getSecurityToken()
    if (!token) {
        showNotification("Security validation failed. Please refresh and try again.", "error")
        return
    }

    // Sanitize order data
    const sanitizedItems = sanitizeOrderData(order)

    // Prepare secure request
    const orderData = {
        totalAmount: validation.calculatedTotal,
        items: sanitizedItems,
        requestToken: token,
    }

    // Add CSRF token
    const csrfToken = document.querySelector('input[name="__RequestVerificationToken"]')?.value

    const headers = {
        "Content-Type": "application/json",
    }

    if (csrfToken) {
        headers["RequestVerificationToken"] = csrfToken
    }

    const endpoint = method === "counter" ? "/Payment/CreateCounterPayment" : "/Payment/CreateCheckoutSession"

    try {
        showNotification("Processing payment...", "info")

        const response = await fetch(endpoint, {
            method: "POST",
            headers: headers,
            body: JSON.stringify(orderData),
        })

        if (!response.ok) {
            throw new Error(`HTTP error! status: ${response.status}`)
        }

        const data = await response.json()

        if (data.success) {
            localStorage.removeItem("orderItems")
            updateOrderCount()

            if (method === "counter") {
                let message = `Order placed successfully! Payment Number: ${data.paymentNumber}. Please present this number at the counter to complete your payment.`

                if (data.memberDiscount && data.memberDiscount > 0) {
                    message += ` You saved $${data.memberDiscount.toFixed(2)} with your member discount!`
                }

                showNotification(message, "success", 10000)
                showPaymentNumberModal(data.paymentNumber, data.orderId, data.memberDiscount || 0)
            } else {
                // Redirect to Stripe Checkout
                window.location.href = data.checkoutUrl
            }
        } else {
            let errorMessage = "Payment processing failed: " + (data.error || "Unknown error")
            if (data.details && Array.isArray(data.details)) {
                errorMessage += "\nDetails: " + data.details.join(", ")
            }
            showNotification(errorMessage, "error")
        }
    } catch (error) {
        console.error("Payment error:", error)
        showNotification("Payment processing failed. Please check your connection and try again.", "error")
    }

    closePaymentModal()
}

// Enhanced notification function with XSS protection
function showNotification(message, type = "info", duration = 3000) {
    // Sanitize message to prevent XSS
    const sanitizedMessage = message.replace(/</g, "&lt;").replace(/>/g, "&gt;")

    const notification = document.createElement("div")
    notification.className = `notification ${type}`
    notification.innerHTML = `
        <div class="notification-content">
            <span class="notification-message">${sanitizedMessage}</span>
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

// Initialize security token on page load
document.addEventListener("DOMContentLoaded", async () => {
    updateOrderCount()
    clearOrdersOnMenuExit()

    // Pre-fetch security token
    await getSecurityToken()

    // Refresh token periodically
    setInterval(getSecurityToken, 1800000) // 30 minutes
})

// ... rest of existing code remains the same ...
