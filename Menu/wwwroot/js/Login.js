// Password strength meter for Register view
document.addEventListener("DOMContentLoaded", function () {
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
});