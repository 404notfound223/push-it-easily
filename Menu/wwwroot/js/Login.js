// Password strength meter for Register view
document.addEventListener("DOMContentLoaded", function () {
    // Password strength meter
    const passwordInput = document.getElementById("password");
    const strengthBar = document.getElementById("passwordStrengthBar");

    if (passwordInput && strengthBar) {
        passwordInput.addEventListener("input", function () {
            const val = passwordInput.value;
            let score = 0;

            // Basic strength rules
            if (val.length >= 8) score++;
            if (/[A-Z]/.test(val)) score++;
            if (/[a-z]/.test(val)) score++;
            if (/[0-9]/.test(val)) score++;
            if (/[^A-Za-z0-9]/.test(val)) score++;

            // Set bar width and color
            let width = (score / 5) * 100;
            strengthBar.style.width = width + "%";

            if (score <= 2) {
                strengthBar.style.backgroundColor = "#e74c3c"; // weak
            } else if (score === 3 || score === 4) {
                strengthBar.style.backgroundColor = "#f1c40f"; // medium
            } else if (score === 5) {
                strengthBar.style.backgroundColor = "#2ecc71"; // strong
            } else {
                strengthBar.style.backgroundColor = "#e0e0e0";
            }
        });
    }

    // Password validation
    const registerForm = document.getElementById('registerForm');
    if (registerForm) {
        registerForm.addEventListener('submit', function (e) {
            const password = document.getElementById('password').value;
            const confirmPassword = document.getElementById('confirmPassword').value;
            const errorDiv = document.getElementById('passwordError');
            errorDiv.style.display = 'none';
            errorDiv.textContent = '';

            if (password.length < 8 || password.length > 16) {
                errorDiv.textContent = 'Password must be between 8 and 16 characters.';
                errorDiv.style.display = 'block';
                e.preventDefault();
                return;
            }
            if (password !== confirmPassword) {
                errorDiv.textContent = 'Passwords do not match.';
                errorDiv.style.display = 'block';
                e.preventDefault();
                return;
            }
        });
    }
//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!! BROKEN HERE NEEED FIX !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    // Email verification AJAX
    const verifyEmailBtn = document.getElementById('verifyEmailBtn');
    if (verifyEmailBtn) {
        verifyEmailBtn.addEventListener('click', function () {
            const email = document.getElementById('email').value;
            if (!email) {
                alert('Please enter your email first.');
                return;
            }
            fetch('/Login/SendVerificationCode', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify({ email: email })
            })
            .then(response => {
                // Check if response is JSON
                const contentType = response.headers.get("content-type");
                if (contentType && contentType.indexOf("application/json") !== -1) {
                    return response.json();
                } else {
                    return response.text().then(text => { throw new Error(text); });
                }
            })
            .then(data => {
                if (data.success) {
                    alert('Verification code sent to ' + email);
                } else {
                    alert(data.error || 'Failed to send code.');
                }
            })
            .catch(error => {
                console.error('Error:', error);
                alert('Something went wrong. Please try again.\n' + error);
            });
        });
    }
});