function loadUserData(userId) {
    fetch(`/Staff/GetUserById?userId=${userId}`)
        .then((res) => res.json())
        .then((data) => {
            if (data.success) {
                const user = data.user
                document.getElementById("user-id").value = user.userId
                document.getElementById("user-name").value = user.name
                document.getElementById("user-email").value = user.email
                document.getElementById("user-role").value = user.role

                document.getElementById("editUserPanelTitle").textContent = "Edit User"
            } else {
                showNotification("Error loading user: " + data.error, "error")
            }
        })
        .catch(() => showNotification("Error loading user", "error"))
}

function showAddUserPanel() {
    document.getElementById("user-form").reset()
    document.getElementById("user-id").value = ""

    // Show backdrop when opening add user panel
    const backdrop = document.getElementById("popout-backdrop")
    if (backdrop) {
        backdrop.classList.add("open")
    }

    openPopout("editUserPanel")
    document.getElementById("editUserPanelTitle").textContent = "Add New User"
}

function saveUser(event) {
    event.preventDefault()

    const userId = document.getElementById("user-id").value
    const name = document.getElementById("user-name").value
    const email = document.getElementById("user-email").value
    const role = document.getElementById("user-role").value

    if (!name || !email || !role) {
        showNotification("Please fill in all required fields", "error")
        return
    }

    const userData = { userId, name, email, role }

    fetch("/Staff/UpdateUser", {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(userData),
    })
        .then((res) => res.json())
        .then((data) => {
            if (data.success) {
                showNotification("User saved successfully", "success")
                closePopout("editUserPanel")
                location.reload()
            } else {
                showNotification("Error saving user: " + data.error, "error")
            }
        })
        .catch(() => {
            showNotification("Error saving user", "error")
        })
}

function addUser(event) {
    event.preventDefault();

    const userData = {
        userId: document.getElementById('new-user-id').value,
        name: document.getElementById('new-user-name').value,
        email: document.getElementById('new-user-email').value,
        password: document.getElementById('new-user-password').value,
        role: document.getElementById('new-user-role').value
    };

    fetch('/Staff/AddUser', {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
        },
        body: JSON.stringify(userData)
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                alert('User created successfully!');
                closePopout('addUserPanel');
                document.getElementById('add-user-form').reset();
                loadUsers(); // Refresh the users table
            } else {
                alert('Error: ' + data.error);
            }
        })
        .catch(error => {
            console.error('Error:', error);
            alert('An error occurred while creating the user.');
        });
}

// ================= USER PAGINATION AND SORTING =================
let currentUserPage = 1
let currentUserPageSize = 20
let currentUserSortBy = "name"
let currentUserSortOrder = "asc"
let currentUserRole = "all"

function loadUsers(page = 1) {
    currentUserPage = page
    currentUserPageSize = 20
    currentUserSortBy = document.getElementById("userSortBy")?.value || "name"
    currentUserSortOrder = document.getElementById("userSortOrder")?.value || "asc"
    currentUserRole = document.getElementById("roleFilter")?.value || "all"

    // Show loading state
    const tbody = document.getElementById("users-table-body")
    if (tbody) {
        tbody.innerHTML =
            '<tr><td colspan="5" style="text-align: center; padding: 40px; color: #6c757d;"><div style="display: inline-flex; align-items: center; gap: 10px;"><div style="width: 20px; height: 20px; border: 2px solid #007bff; border-top: 2px solid transparent; border-radius: 50%; animation: spin 1s linear infinite;"></div>Loading users...</div></td></tr>'
    }

    const params = new URLSearchParams({
        page: currentUserPage,
        pageSize: currentUserPageSize,
        sortBy: currentUserSortBy,
        sortOrder: currentUserSortOrder,
        role: currentUserRole,
    })

    fetch(`/Staff/GetUsersData?${params}`)
        .then((res) => res.json())
        .then((data) => {
            if (data.success) {
                updateUsersTable(data.users)
                updateUserPagination(data.currentPage, data.totalPages, data.totalCount)
            } else {
                showNotification("Error loading users: " + data.error, "error")
                if (tbody) {
                    tbody.innerHTML =
                        '<tr><td colspan="5" style="text-align: center; padding: 40px; color: #dc3545;">Error loading users. Please try again.</td></tr>'
                }
            }
        })
        .catch(() => {
            showNotification("Error loading users", "error")
            if (tbody) {
                tbody.innerHTML =
                    '<tr><td colspan="5" style="text-align: center; padding: 40px; color: #dc3545;">Network error. Please check your connection and try again.</td></tr>'
            }
        })
}

function updateUsersTable(users) {
    const tbody = document.getElementById("users-table-body")
    if (!tbody) return

    tbody.innerHTML = ""

    users.forEach((user) => {
        const row = document.createElement("tr")
        row.id = `user-row-${user.userId}`

        row.innerHTML = `
            <td>${user.userId}</td>
            <td>${user.name}</td>
            <td>${user.email}</td>
            <td><span class="role-badge role-${user.role}">${user.role.toUpperCase()}</span></td>
            <td>
                <button class="btn btn-sm btn-warning" onclick="openPopout('editUserPanel','${user.userId}')">Edit</button>
                <button class="btn btn-sm btn-danger" onclick="confirmDelete('user', '${user.userId}')">Delete</button>
            </td>
        `

        tbody.appendChild(row)
    })
}

function updateUserPagination(currentPage, totalPages, totalCount) {
    const paginationInfo = document.getElementById("user-pagination-info")
    const prevButton = document.getElementById("user-prev-page")
    const nextButton = document.getElementById("user-next-page")
    const pageNumbers = document.getElementById("user-page-numbers")

    if (paginationInfo) {
        const startItem = (currentPage - 1) * currentUserPageSize + 1
        const endItem = Math.min(currentPage * currentUserPageSize, totalCount)
        paginationInfo.textContent = `Showing ${startItem}-${endItem} of ${totalCount} users`
    }

    if (prevButton) {
        prevButton.disabled = currentPage <= 1
    }

    if (nextButton) {
        nextButton.disabled = currentPage >= totalPages
    }

    if (pageNumbers) {
        pageNumbers.innerHTML = ""
        const maxVisiblePages = 5
        let startPage = Math.max(1, currentPage - Math.floor(maxVisiblePages / 2))
        const endPage = Math.min(totalPages, startPage + maxVisiblePages - 1)

        if (endPage - startPage + 1 < maxVisiblePages) {
            startPage = Math.max(1, endPage - maxVisiblePages + 1)
        }

        for (let i = startPage; i <= endPage; i++) {
            const pageBtn = document.createElement("button")
            pageBtn.className = `page-btn ${i === currentPage ? "active" : ""}`
            pageBtn.textContent = i
            pageBtn.onclick = () => loadUsers(i)
            pageNumbers.appendChild(pageBtn)
        }
    }
}

function changeUserPage(direction) {
    const newPage = currentUserPage + direction
    if (newPage >= 1) {
        loadUsers(newPage)
    }
}

// ================= SEARCH FILTERS =================
function searchUsers() {
    const searchTerm = document.getElementById("userSearch").value.toLowerCase()
    const rows = document.querySelectorAll("#users-table-body tr")

    rows.forEach((row) => {
        const userName = row.cells[1].textContent.toLowerCase() // User name is in the second column
        if (userName.includes(searchTerm)) {
            row.style.display = ""
        } else {
            row.style.display = "none"
        }
    })
}

// ================= DELETE =================
function deleteUser(userId) {
    const formData = new FormData()
    formData.append("userId", userId)

    fetch("/Staff/DeleteUser", {
        method: "POST",
        body: formData,
        credentials: "same-origin",
    })
        .then((res) => res.json())
        .then((data) => {
            if (data.success) {
                showNotification("User deleted successfully", "success")
                location.reload()
            } else {
                showNotification("Error deleting user: " + data.error, "error")
            }
        })
        .catch(() => showNotification("Error deleting user", "error"))
}
