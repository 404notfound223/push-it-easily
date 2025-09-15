// Handle tab switching
function showTab(tabName, event) {
    // Hide all tabs
    document.querySelectorAll('.tab-content').forEach(tab => {
        tab.classList.remove('active');
    });
    document.querySelectorAll('.tab-button').forEach(btn => {
        btn.classList.remove('active');
    });

    // Show selected tab
    document.getElementById(tabName + '-tab').classList.add('active');
    if (event) event.target.classList.add('active');
}

//  ORDERS 
function updateOrderStatus(orderId, status) {
    fetch('/Staff/UpdateOrderStatus', {
        method: 'POST',
        headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
        body: `orderId=${orderId}&status=${status}`
    })
        .then(res => res.json())
        .then(data => {
            if (data.success) {
                showNotification('Order status updated successfully', 'success');
            } else {
                showNotification('Error updating order status: ' + data.error, 'error');
            }
        })
        .catch(() => {
            showNotification('Error updating order status', 'error');
        });
}

function viewOrderDetails(orderId) {
    fetch(`/Staff/GetOrderDetails?orderId=${orderId}`)
        .then(res => res.json())
        .then(data => {
            if (data.success) {
                const order = data.order;
                let detailsHtml = `
                    <div class="order-info">
                        <p><strong>Order ID:</strong> ${order.orderId}</p>
                        <p><strong>Customer:</strong> ${order.user.name}</p>
                        <p><strong>Email:</strong> ${order.user.email}</p>
                        <p><strong>Date:</strong> ${new Date(order.orderDate).toLocaleString()}</p>
                        <p><strong>Status:</strong> ${order.status}</p>
                        <p><strong>Total:</strong> $${order.totalAmount.toFixed(2)}</p>
                    </div>
                    <h3>Order Items:</h3>
                    <table class="order-items-table">
                        <thead>
                            <tr>
                                <th>Item</th><th>Quantity</th><th>Unit Price</th><th>Total</th>
                            </tr>
                        </thead>
                        <tbody>
                `;

                order.orderDetails.forEach(item => {
                    detailsHtml += `
                        <tr>
                            <td>${item.product.name}</td>
                            <td>${item.quantity}</td>
                            <td>$${item.unitPrice.toFixed(2)}</td>
                            <td>$${(item.quantity * item.unitPrice).toFixed(2)}</td>
                        </tr>
                    `;
                });

                detailsHtml += '</tbody></table>';

                document.getElementById('order-details-content').innerHTML = detailsHtml;
                document.getElementById('order-details-modal').style.display = 'block';
            } else {
                showNotification('Error loading order details: ' + data.error, 'error');
            }
        })
        .catch(() => {
            showNotification('Error loading order details', 'error');
        });
}

function closeOrderModal() {
    document.getElementById('order-details-modal').style.display = 'none';
}

// USERS 
function editUser(userId, name, email, role) {
    document.getElementById('user-id').value = userId;
    document.getElementById('user-name').value = name;
    document.getElementById('user-email').value = email;
    document.getElementById('user-role').value = role;

    // Open popout panel
    document.getElementById('editUserPanel').classList.add('open');
    document.getElementById('editUserPanelTitle').textContent = 'Edit User';
}

function showAddUserPanel() {
    document.getElementById('user-form').reset();
    document.getElementById('user-id').value = '';

    document.getElementById('editUserPanel').classList.add('open');
    document.getElementById('editUserPanelTitle').textContent = 'Add New User';
}

function closeUserPanel() {
    document.getElementById('editUserPanel').classList.remove('open');
}

function saveUser() {
    const userId = document.getElementById('user-id').value;
    const name = document.getElementById('user-name').value;
    const email = document.getElementById('user-email').value;
    const role = document.getElementById('user-role').value;

    const userData = { userId, name, email, role };

    fetch('/Staff/UpdateUser', {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(userData)
    })
        .then(res => res.json())
        .then(data => {
            if (data.success) {
                showNotification('User saved successfully', 'success');
                closeUserPanel();
                location.reload();
            } else {
                showNotification('Error saving user: ' + data.error, 'error');
            }
        })
        .catch(() => {
            showNotification('Error saving user', 'error');
        });
}

function deleteUser(userId) {
    if (confirm('Are you sure you want to delete this user?')) {
        fetch('/Staff/DeleteUser', {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: `userId=${userId}`
        })
            .then(res => res.json())
            .then(data => {
                if (data.success) {
                    showNotification('User deleted successfully', 'success');
                    location.reload();
                } else {
                    showNotification('Error deleting user: ' + data.error, 'error');
                }
            })
            .catch(() => {
                showNotification('Error deleting user', 'error');
            });
    }
}

// PRODUCTS 
function showAddProductPanel() {
    document.getElementById('product-form').reset();
    document.getElementById('product-id').value = '';

    document.getElementById('addProductPanel').classList.add('open');
}

function closeProductPanel() {
    document.getElementById('addProductPanel').classList.remove('open');
}

function saveProduct() {
    const product = {
        id: document.getElementById('product-id').value,
        name: document.getElementById('product-name').value,
        price: parseFloat(document.getElementById('product-price').value),
        description: document.getElementById('product-description').value,
        category: document.getElementById('product-category').value,
        imagePath: document.getElementById('product-imagePath').value,
        stock: parseInt(document.getElementById('product-stock').value)
    };

    const url = product.id ? '/Staff/UpdateProduct' : '/Staff/AddProduct';

    fetch(url, {
        method: 'POST',
        headers: { 'Content-Type': 'application/json' },
        body: JSON.stringify(product)
    })
        .then(res => res.json())
        .then(data => {
            if (data.success) {
                showNotification('Product saved successfully', 'success');
                closeProductPanel();
                location.reload();
            } else {
                showNotification('Error saving product: ' + data.error, 'error');
            }
        })
        .catch(() => {
            showNotification('Error saving product', 'error');
        });
}

function deleteProduct(productId) {
    if (confirm('Are you sure you want to delete this product?')) {
        fetch('/Staff/DeleteProduct', {
            method: 'POST',
            headers: { 'Content-Type': 'application/x-www-form-urlencoded' },
            body: `id=${productId}`
        })
            .then(res => res.json())
            .then(data => {
                if (data.success) {
                    showNotification('Product deleted successfully', 'success');
                    location.reload();
                } else {
                    showNotification('Error deleting product: ' + data.error, 'error');
                }
            })
            .catch(() => {
                showNotification('Error deleting product', 'error');
            });
    }
}

// NOTIFICATION 
function showNotification(message, type) {
    const notification = document.createElement('div');
    notification.className = `notification ${type}`;
    notification.textContent = message;
    document.body.appendChild(notification);

    setTimeout(() => notification.remove(), 3000);
}
