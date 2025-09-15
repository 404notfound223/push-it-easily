function showForgotPassword() {
    document.getElementById("forgot-password-modal").style.display = "block"
    document.getElementById("forgot-message").innerHTML = ""
    // Clear form fields
    document.getElementById("current-password").value = ""
    document.getElementById("new-password").value = ""
    document.getElementById("confirm-password").value = ""
}

function closeForgotPassword() {
    document.getElementById("forgot-password-modal").style.display = "none"
}

function changePassword() {
    const currentPassword = document.getElementById("current-password").value.trim()
    const newPassword = document.getElementById("new-password").value
    const confirmPassword = document.getElementById("confirm-password").value

    if (!currentPassword) {
        showMessage("Please enter your current password", "error")
        return
    }

    if (!newPassword || newPassword.length < 6) {
        showMessage("New password must be at least 6 characters long", "error")
        return
    }

    if (newPassword !== confirmPassword) {
        showMessage("New passwords do not match", "error")
        return
    }

    fetch("/Login/ChangePassword", {
        method: "POST",
        headers: {
            "Content-Type": "application/x-www-form-urlencoded",
        },
        body: `currentPassword=${encodeURIComponent(currentPassword)}&newPassword=${encodeURIComponent(newPassword)}`,
    })
        .then((response) => response.json())
        .then((data) => {
            if (data.success) {
                showMessage(data.message, "success")
                setTimeout(() => {
                    closeForgotPassword()
                }, 2000)
            } else {
                showMessage("Error: " + data.error, "error")
            }
        })
        .catch((error) => {
            showMessage("Error changing password: " + error, "error")
        })
}

function showMessage(message, type) {
    const messageDiv = document.getElementById("forgot-message")
    messageDiv.innerHTML = `<div class="${type}-message">${message}</div>`
}
