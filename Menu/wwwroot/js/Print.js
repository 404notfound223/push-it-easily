
document.addEventListener('DOMContentLoaded', function() {
    const now = new Date();
const dateElement = document.getElementById('receipt-date');
const timeElement = document.getElementById('receipt-time');

if (dateElement) {
    dateElement.textContent = now.toLocaleDateString();
    }
if (timeElement) {
    timeElement.textContent = now.toLocaleTimeString();
    }
});

function printOnlineReceipt() {
    const receiptContent = document.getElementById('online-receipt').innerHTML;
const printWindow = window.open('', '_blank');

printWindow.document.write(`
<!DOCTYPE html>
<html>
    <head>
        <title>Payment Receipt</title>
        <style>
            body {
                font - family: 'Courier New', monospace;
            max-width: 400px;
            margin: 0 auto;
            padding: 20px;
            line-height: 1.4;
                }
            .receipt-header {
                text - align: center;
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
            .receipt-items h4 {
                margin: 10px 0 5px 0;
            font-size: 14px;
            text-align: center;
                }
            .total-row {
                font - size: 14px;
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
                font - size: 11px;
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
`);

printWindow.document.close();
printWindow.focus();
printWindow.print();

    printWindow.onafterprint = () => {
    printWindow.close();
    };

    // Fallback for browsers that don't support onafterprint
    setTimeout(() => {
        if (!printWindow.closed) {
    printWindow.close();
        }
    }, 5000);
}

function createAnotherOrder() {
    window.location.href = '/Menu/All';
}

function goToMenu() {
    window.location.href = '/Menu/All';
}

function logout() {
    if (confirm('Are you sure you want to logout?')) {
    fetch('/Login/Logout', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        }
    }).then(() => {
        window.location.href = '/Login/Login';
    });
    }
}