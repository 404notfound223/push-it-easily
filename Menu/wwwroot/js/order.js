function addToOrder(id, name, price) {
    let order = JSON.parse(localStorage.getItem('order')) || [];
    order.push({ id, name, price });
    localStorage.setItem('order', JSON.stringify(order));
    alert(name + " added to order!");
}

function renderOrder() {
    let order = JSON.parse(localStorage.getItem('order')) || [];
    let html = '';
    let total = 0;
    order.forEach(item => {
        html += `<div>${item.name} - $${item.price}</div>`;
        total += parseFloat(item.price);
    });
    document.getElementById('order-list').innerHTML = html || '<i>No items in order.</i>';
    document.getElementById('order-total').textContent = total.toFixed(2);
}