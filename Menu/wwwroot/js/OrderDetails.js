function reorder() {
    if (confirm('Add all items from this order to your current cart?')) {
    showNotification('Items added to cart!', 'success');
        // Implementation would add items to localStorage cart
    }
}

function trackOrder() {
    showNotification('Order tracking updates will be sent to your email.', 'info');
}

function printOrder() {
    window.print();
}

function showNotification(message, type) {
    const notification = document.createElement('div');
notification.className = `notification ${type}`;
notification.textContent = message;
document.body.appendChild(notification);
        
    setTimeout(() => {
    notification.remove();
    }, 3000);
}