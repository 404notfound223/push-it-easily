function showTab(tabName) {
    // Hide all tabs
    document.querySelectorAll('.tab-content').forEach(tab => {
        tab.classList.remove('active');
    });
    document.querySelectorAll('.tab-button').forEach(btn => {
    btn.classList.remove('active');
    });

// Show selected tab
document.getElementById(tabName + '-tab').classList.add('active');
event.target.classList.add('active');
}

function updateOrderStatus(orderId, status) {
    fetch('/Staff/UpdateOrderStatus', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
        },
        body: `orderId=${orderId}&status=${status}`
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                showNotification('Order status updated successfully', 'success');
            } else {
                showNotification('Error updating order status: ' + data.error, 'error');
            }
        })
        .catch(error => {
            showNotification('Error updating order status', 'error');
        });
}

function viewOrderDetails(orderId) {
    fetch(`/Staff/GetOrderDetails?orderId=${orderId}`)
        .then(response => response.json())
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
                            <th>Item</th>
                            <th>Quantity</th>
                            <th>Unit Price</th>
                            <th>Total</th>
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
        .catch(error => {
            showNotification('Error loading order details', 'error');
        });
}

function closeOrderModal() {
    document.getElementById('order-details-modal').style.display = 'none';
}

function editUser(userId, name, email, role) {
    document.getElementById('user-id').value = userId;
document.getElementById('user-name').value = name;
document.getElementById('user-email').value = email;
document.getElementById('user-role').value = role;
document.getElementById('user-modal-title').textContent = 'Edit User';
document.getElementById('user-edit-modal').style.display = 'block';
}

function showAddUserModal() {
    document.getElementById('user-form').reset();
document.getElementById('user-id').value = '';
document.getElementById('user-modal-title').textContent = 'Add New User';
document.getElementById('user-edit-modal').style.display = 'block';
}

function closeUserModal() {
    document.getElementById('user-edit-modal').style.display = 'none';
}

function saveUser() {
    const userId = document.getElementById('user-id').value;
const name = document.getElementById('user-name').value;
const email = document.getElementById('user-email').value;
const role = document.getElementById('user-role').value;

const userData = {
    userId: userId,
name: name,
email: email,
role: role
    };

fetch('/Staff/UpdateUser', {
    method: 'POST',
headers: {
    'Content-Type': 'application/json',
        },
body: JSON.stringify(userData)
    })
    .then(response => response.json())
    .then(data => {
        if (data.success) {
    showNotification('User updated successfully', 'success');
closeUserModal();
location.reload();
        } else {
    showNotification('Error updating user: ' + data.error, 'error');
        }
    })
    .catch(error => {
    showNotification('Error updating user', 'error');
    });
}

function deleteUser(userId) {
    if (confirm('Are you sure you want to delete this user?')) {
    fetch('/Staff/DeleteUser', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/x-www-form-urlencoded',
        },
        body: `userId=${userId}`
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                showNotification('User deleted successfully', 'success');
                location.reload();
            } else {
                showNotification('Error deleting user: ' + data.error, 'error');
            }
        })
        .catch(error => {
            showNotification('Error deleting user', 'error');
        });
    }
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

