// Menu functionality with cart management
const cart = JSON.parse(localStorage.getItem("cart")) || []

function addToCart(productId, productName, price) {
    // Check if product is disabled before adding
    const productCard = document.querySelector(`[data-product-id="${productId}"]`)
    if (productCard && productCard.classList.contains("product-disabled")) {
        showNotification("This item is currently unavailable", "error")
        return
    }

    const existingItem = cart.find((item) => item.id === productId)

    if (existingItem) {
        existingItem.quantity += 1
    } else {
        cart.push({
            id: productId,
            name: productName,
            price: price,
            quantity: 1,
        })
    }

    localStorage.setItem("cart", JSON.stringify(cart))
    updateCartDisplay()
    showNotification(`${productName} added to cart!`, "success")
}

function updateCartDisplay() {
    const cartCount = cart.reduce((total, item) => total + item.quantity, 0)
    const cartBadge = document.querySelector(".cart-badge")
    if (cartBadge) {
        cartBadge.textContent = cartCount
        cartBadge.style.display = cartCount > 0 ? "block" : "none"
    }
}

function showNotification(message, type) {
    const notification = document.createElement("div")
    notification.className = `notification ${type}`
    notification.textContent = message

    // Style the notification
    notification.style.cssText = `
        position: fixed;
        top: 20px;
        right: 20px;
        padding: 15px 20px;
        border-radius: 6px;
        color: white;
        font-weight: 500;
        z-index: 1000;
        animation: slideIn 0.3s ease;
        ${type === "success" ? "background-color: #28a745;" : "background-color: #dc3545;"}
    `

    document.body.appendChild(notification)

    setTimeout(() => {
        notification.style.animation = "slideOut 0.3s ease"
        setTimeout(() => notification.remove(), 300)
    }, 3000)
}

// Initialize cart display on page load
document.addEventListener("DOMContentLoaded", () => {
    updateCartDisplay()

    // Add click prevention for disabled products
    document.querySelectorAll(".product-card.product-disabled").forEach((card) => {
        card.style.cursor = "not-allowed"
        card.addEventListener("click", (e) => {
            e.preventDefault()
            e.stopPropagation()
            showNotification("This item is currently being updated and is not available for ordering", "error")
        })
    })
})

// Add CSS animations
const style = document.createElement("style")
style.textContent = `
    @keyframes slideIn {
        from { transform: translateX(100%); opacity: 0; }
        to { transform: translateX(0); opacity: 1; }
    }
    @keyframes slideOut {
        from { transform: translateX(0); opacity: 1; }
        to { transform: translateX(100%); opacity: 0; }
    }
`
document.head.appendChild(style)
