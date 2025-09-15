// Order Details JavaScript functionality

function reorder() {
    if (confirm("Add all items from this order to your current cart?")) {
        // Get order items and add to cart
        const orderItems = document.querySelectorAll(".item-row")
        let itemsAdded = 0

        orderItems.forEach((item) => {
            const itemName = item.querySelector(".item-info h4").textContent
            const quantity = Number.parseInt(item.querySelector(".quantity-value").textContent)

            // Add to cart logic would go here
            // For now, just show a notification
            itemsAdded += quantity
        })

        showNotification(`${itemsAdded} items added to your cart!`, "success")

        // Redirect to menu after a short delay
        setTimeout(() => {
            window.location.href = "/Menu/All"
        }, 2000)
    }
}

function trackOrder() {
    const orderId = getOrderIdFromUrl()
    if (orderId) {
        window.location.href = `/Order/TrackOrder?orderId=${orderId}`
    } else {
        showNotification("Unable to track order. Order ID not found.", "error")
    }
}

function printOrder() {
    // Hide buttons and other non-printable elements
    const buttons = document.querySelectorAll(".btn, .order-actions-card")
    buttons.forEach((btn) => (btn.style.display = "none"))

    // Print the page
    window.print()

    // Restore buttons after printing
    setTimeout(() => {
        buttons.forEach((btn) => (btn.style.display = ""))
    }, 1000)
}

function getOrderIdFromUrl() {
    const urlParams = new URLSearchParams(window.location.search)
    return urlParams.get("orderId")
}

function showNotification(message, type) {
    // Create notification element
    const notification = document.createElement("div")
    notification.className = `notification ${type}`
    notification.innerHTML = `
        <div class="notification-content">
            <span class="notification-message">${message}</span>
            <button class="notification-close" onclick="this.parentElement.parentElement.remove()">&times;</button>
        </div>
    `

    // Add styles
    notification.style.cssText = `
        position: fixed;
        top: 20px;
        right: 20px;
        z-index: 1000;
        padding: 15px 20px;
        border-radius: 8px;
        box-shadow: 0 4px 12px rgba(0,0,0,0.15);
        max-width: 400px;
        animation: slideIn 0.3s ease-out;
    `

    // Set colors based on type
    if (type === "success") {
        notification.style.backgroundColor = "#d4edda"
        notification.style.color = "#155724"
        notification.style.border = "1px solid #c3e6cb"
    } else if (type === "error") {
        notification.style.backgroundColor = "#f8d7da"
        notification.style.color = "#721c24"
        notification.style.border = "1px solid #f5c6cb"
    } else {
        notification.style.backgroundColor = "#d1ecf1"
        notification.style.color = "#0c5460"
        notification.style.border = "1px solid #bee5eb"
    }

    document.body.appendChild(notification)

    // Auto remove after 5 seconds
    setTimeout(() => {
        if (notification.parentElement) {
            notification.remove()
        }
    }, 5000)
}

// Add CSS for animations
const style = document.createElement("style")
style.textContent = `
    @keyframes slideIn {
        from {
            transform: translateX(100%);
            opacity: 0;
        }
        to {
            transform: translateX(0);
            opacity: 1;
        }
    }
    
    .notification-content {
        display: flex;
        justify-content: space-between;
        align-items: center;
    }
    
    .notification-close {
        background: none;
        border: none;
        font-size: 18px;
        cursor: pointer;
        margin-left: 10px;
        opacity: 0.7;
    }
    
    .notification-close:hover {
        opacity: 1;
    }
    
    @media print {
        .notification {
            display: none !important;
        }
    }
`
document.head.appendChild(style)
