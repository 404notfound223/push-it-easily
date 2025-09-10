//function showForgotPassword() {
//    document.getElementById("forgot-password-modal").style.display = "block"
//    document.getElementById("forgot-step-1").style.display = "block"
//    document.getElementById("forgot-step-2").style.display = "none"
//    document.getElementById("forgot-message").innerHTML = ""
//}

//function closeForgotPassword() {
//    document.getElementById("forgot-password-modal").style.display = "none"
//}

//function sendResetCode() {
//    const email = document.getElementById("forgot-email").value.trim()
//    if (!email) {
//        showMessage("Please enter your email address", "error")
//        return
//    }

//    fetch("/Login/ForgotPassword", {
//        method: "POST",
//        headers: {
//            "Content-Type": "application/json",
//        },
//        body: JSON.stringify({ email: email }),
//    })
//        .then((response) => response.json())
//        .then((data) => {
//            if (data.success) {
//                document.getElementById("forgot-step-1").style.display = "none"
//                document.getElementById("forgot-step-2").style.display = "block"
//                showMessage(data.message, "success")
//            } else {
//                showMessage("Error: " + data.error, "error")
//            }
//        })
//        .catch((error) => {
//            showMessage("Error sending reset code: " + error, "error")
//        })
//}

//function resetPasswordWithCode() {
//    const code = document.getElementById("reset-code").value.trim()
//    const newPassword = document.getElementById("new-password").value
//    const confirmPassword = document.getElementById("confirm-password").value

//    if (!code) {
//        showMessage("Please enter the reset code", "error")
//        return
//    }

//    if (!newPassword || newPassword.length < 6) {
//        showMessage("Password must be at least 6 characters long", "error")
//        return
//    }

//    if (newPassword !== confirmPassword) {
//        showMessage("Passwords do not match", "error")
//        return
//    }

//    fetch("/Login/ResetPassword", {
//        method: "POST",
//        headers: {
//            "Content-Type": "application/json",
//        },
//        body: JSON.stringify({ code: code, newPassword: newPassword }),
//    })
//        .then((response) => response.json())
//        .then((data) => {
//            if (data.success) {
//                showMessage(data.message, "success")
//                setTimeout(() => {
//                    closeForgotPassword()
//                    if (window.location.pathname.includes("/Login/Login")) {
//                        // On login page, just close modal
//                    } else {
//                        // On other pages, redirect to login
//                        window.location.href = "/Login/Login"
//                    }
//                }, 2000)
//            } else {
//                showMessage("Error: " + data.error, "error")
//            }
//        })
//        .catch((error) => {
//            showMessage("Error resetting password: " + error, "error")
//        })
//}

//function showMessage(message, type) {
//    const messageDiv = document.getElementById("forgot-message")
//    messageDiv.innerHTML = `<div class="${type}-message">${message}</div>`
//}
