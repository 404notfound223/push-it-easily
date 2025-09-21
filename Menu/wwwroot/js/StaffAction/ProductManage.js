function loadProductData(productId) {
    fetch(`/Staff/GetProductById?id=${productId}`)
        .then((res) => res.json())
        .then((data) => {
            if (data.success) {
                const p = data.product
                document.getElementById("product-id").value = p.id
                document.getElementById("product-name").value = p.name
                document.getElementById("product-price").value = p.price
                document.getElementById("product-description").value = p.description
                document.getElementById("product-category").value = p.category
                document.getElementById("product-imagePath").value = p.imagePath

                document.getElementById("editProductPanelTitle").textContent = "Edit Product"
            } else {
                showNotification("Error loading product: " + data.error, "error")
            }
        })
        .catch(() => showNotification("Error loading product", "error"))
}

function showAddProductPanel() {
    document.getElementById("product-form").reset()
    document.getElementById("product-id").value = ""
    document.getElementById("editProductPanelTitle").textContent = "Add New Product"

    const panel = document.getElementById("editProductPanel")
    openPopout("editProductPanel")
}

function saveProduct(event) {
    event.preventDefault()

    const product = {
        id: document.getElementById("product-id").value,
        name: document.getElementById("product-name").value,
        price: Number.parseFloat(document.getElementById("product-price").value),
        description: document.getElementById("product-description").value,
        category: document.getElementById("product-category").value,
        imagePath: document.getElementById("product-imagePath").value,
    }

    if (!product.name || !product.price || !product.category < 0) {
        showNotification("Please fill in all required fields with valid values", "error")
        return
    }

    const url = product.id ? "/Staff/UpdateProduct" : "/Staff/AddProduct"

    fetch(url, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(product),
    })
        .then((res) => res.json())
        .then((data) => {
            if (data.success) {
                showNotification("Product saved successfully", "success")
                closePopout("editProductPanel")
                loadProducts(currentPage) // Reload products instead of full page
            } else {
                showNotification("Error saving product: " + data.error, "error")
            }
        })
        .catch(() => {
            showNotification("Error saving product", "error")
        })
}
//================= Add Product with Image (newly added) ================= (need test)
function addProduct(event) {
    event.preventDefault();

    const formData = new FormData();
    formData.append('name', document.getElementById('add-product-name').value);
    formData.append('price', document.getElementById('add-product-price').value);
    formData.append('description', document.getElementById('add-product-description').value);
    formData.append('category', document.getElementById('add-product-category').value);

    const imageFile = document.getElementById('add-product-image').files[0];
    if (imageFile) {
        formData.append('imageFile', imageFile);
    }

    fetch('/Staff/AddProductWithImage', {
        method: 'POST',
        body: formData
    })
        .then(response => response.json())
        .then(data => {
            if (data.success) {
                alert('Product added successfully!');
                closePopout('addProductPanel');
                document.getElementById('add-product-form').reset();
                removeAddImage(); // Clear image preview
                loadProducts(); // Refresh the products table
            } else {
                alert('Error: ' + data.error);
            }
        })
        .catch(error => {
            console.error('Error:', error);
            alert('An error occurred while adding the product.');
        });
}

// ================= UTILITY FUNCTIONS ================= 
function getCsrfToken() {
    const token = document.querySelector('input[name="__RequestVerificationToken"]')
    return token ? token.value : ""
}

// ================= PRODUCT STATUS TOGGLE =================
function toggleProductStatus(productId, disable, buttonElement) {
    const requestData = {
        ProductId: productId,
        Disable: disable,
    }

    fetch("/Staff/ToggleProductDisable", {
        method: "POST",
        headers: {
            "Content-Type": "application/json",
            RequestVerificationToken: getCsrfToken(),
        },
        body: JSON.stringify(requestData),
    })
        .then((res) => res.json())
        .then((data) => {
            if (data.success) {
                const row = document.getElementById("product-row-" + productId)
                const button = buttonElement

                if (data.isDisabled) {
                    // Product is now disabled
                    row.classList.add("product-disabled")
                    button.classList.remove("enabled")
                    button.classList.add("disabled")
                    button.textContent = "Disabled"
                    button.onclick = () => toggleProductStatus(productId, false, button)
                    showNotification("Product disabled - members cannot order this item", "success")
                } else {
                    // Product is now enabled
                    row.classList.remove("product-disabled")
                    button.classList.remove("disabled")
                    button.classList.add("enabled")
                    button.textContent = "Enabled"
                    button.onclick = () => toggleProductStatus(productId, true, button)
                    showNotification("Product enabled - members can now order this item", "success")
                }
            } else {
                showNotification("Error updating product status: " + data.error, "error")
            }
        })
        .catch(() => {
            showNotification("Error updating product status", "error")
        })
}

// ================= PRODUCT PAGINATION AND SORTING =================
let currentPage = 1
let currentPageSize = 20
let currentSortBy = "name"
let currentSortOrder = "asc"
let currentCategory = "all"

function loadProducts(page = 1) {
    currentPage = page
    currentPageSize = 20
    currentSortBy = document.getElementById("sortBy")?.value || "name"
    currentSortOrder = document.getElementById("sortOrder")?.value || "asc"
    currentCategory = document.getElementById("categoryFilter")?.value || "all"

    // Show loading state
    const tbody = document.getElementById("products-table-body")
    if (tbody) {
        tbody.innerHTML =
            '<tr><td colspan="7" style="text-align: center; padding: 40px; color: #6c757d;"><div style="display: inline-flex; align-items: center; gap: 10px;"><div style="width: 20px; height: 20px; border: 2px solid #007bff; border-top: 2px solid transparent; border-radius: 50%; animation: spin 1s linear infinite;"></div>Loading products...</div></td></tr>'
    }

    const params = new URLSearchParams({
        page: currentPage,
        pageSize: currentPageSize,
        sortBy: currentSortBy,
        sortOrder: currentSortOrder,
        category: currentCategory,
    })

    fetch(`/Staff/GetProductsData?${params}`)
        .then((res) => res.json())
        .then((data) => {
            if (data.success) {
                updateProductsTable(data.products)
                updatePagination(data.currentPage, data.totalPages, data.totalCount)
            } else {
                showNotification("Error loading products: " + data.error, "error")
                if (tbody) {
                    tbody.innerHTML =
                        '<tr><td colspan="7" style="text-align: center; padding: 40px; color: #dc3545;">Error loading products. Please try again.</td></tr>'
                }
            }
        })
        .catch(() => {
            showNotification("Error loading products", "error")
            if (tbody) {
                tbody.innerHTML =
                    '<tr><td colspan="7" style="text-align: center; padding: 40px; color: #dc3545;">Network error. Please check your connection and try again.</td></tr>'
            }
        })
}

function updateProductsTable(products) {
    const tbody = document.getElementById("products-table-body")
    if (!tbody) return

    tbody.innerHTML = ""

    products.forEach((product) => {
        const row = document.createElement("tr")
        row.className = product.isDisabled ? "product-disabled" : ""
        row.id = `product-row-${product.id}`

        row.innerHTML = `
            <td>${product.id.substring(0, 8).toUpperCase()}</td>
            <td>${product.category}</td>
            <td>${product.name}</td>
            <td>$${product.price.toFixed(2)}</td>
            <td>
                <button class="btn-toggle-status ${product.isDisabled ? "disabled" : "enabled"}"
                        onclick="toggleProductStatus('${product.id}', ${!product.isDisabled}, this)">
                    ${product.isDisabled ? "Disabled" : "Enabled"}
                </button>
            </td>
            <td>
                <button class="btn btn-sm btn-warning" onclick="openPopout('editProductPanel','${product.id}')">Edit</button>
                <button class="btn btn-sm btn-danger" onclick="confirmDelete('product', '${product.id}')">Delete</button>
            </td>
        `

        tbody.appendChild(row)
    })
}

function updatePagination(currentPage, totalPages, totalCount) {
    const paginationInfo = document.getElementById("pagination-info")
    const prevButton = document.getElementById("prev-page")
    const nextButton = document.getElementById("next-page")
    const pageNumbers = document.getElementById("page-numbers")

    if (paginationInfo) {
        const startItem = (currentPage - 1) * currentPageSize + 1
        const endItem = Math.min(currentPage * currentPageSize, totalCount)
        paginationInfo.textContent = `Showing ${startItem}-${endItem} of ${totalCount} products`
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
            pageBtn.onclick = () => loadProducts(i)
            pageNumbers.appendChild(pageBtn)
        }
    }
}

function changePage(direction) {
    const newPage = currentPage + direction
    if (newPage >= 1) {
        loadProducts(newPage)
    }
}

// ================= SEARCH FILTERS =================
function searchProducts() {
    const searchTerm = document.getElementById("productSearch").value.toLowerCase()
    const rows = document.querySelectorAll("#products-table-body tr")

    rows.forEach((row) => {
        const productName = row.cells[2].textContent.toLowerCase() // Product name is in the third column
        if (productName.includes(searchTerm)) {
            row.style.display = ""
        } else {
            row.style.display = "none"
        }
    })
}

// ================= DELETE =================
function deleteProduct(productId) {
    const formData = new FormData()
    formData.append("id", productId)

    fetch("/Staff/DeleteProduct", {
        method: "POST",
        body: formData,
        credentials: "same-origin",
    })
        .then((res) => res.json())
        .then((data) => {
            if (data.success) {
                showNotification("Product deleted successfully", "success")
                loadProducts(currentPage) // Reload products instead of full page
            } else {
                showNotification("Error deleting product: " + data.error, "error")
            }
        })
        .catch(() => showNotification("Error deleting product", "error"))
}

//================= Imange (newly added) =================
function setupImageUpload(type) {
    const dropzone = document.getElementById(`${type}-image-dropzone`);
    const fileInput = document.getElementById(`${type}-product-image`);
    const preview = document.getElementById(`${type}-image-preview`);
    const actions = dropzone.parentElement.querySelector('.image-actions');

    // Handle drag and drop
    dropzone.addEventListener('dragover', (e) => {
        e.preventDefault();
        dropzone.classList.add('dragover');
    });

    dropzone.addEventListener('dragleave', (e) => {
        e.preventDefault();
        dropzone.classList.remove('dragover');
    });

    dropzone.addEventListener('drop', (e) => {
        e.preventDefault();
        dropzone.classList.remove('dragover');

        const files = e.dataTransfer.files;
        if (files.length > 0) {
            handleImageFile(files[0], type);
        }
    });

    // Handle file input change
    fileInput.addEventListener('change', (e) => {
        if (e.target.files.length > 0) {
            handleImageFile(e.target.files[0], type);
        }
    });
}

function handleImageFile(file, type) {
    // Validate file type
    const allowedTypes = ['image/jpeg', 'image/jpg', 'image/png', 'image/gif', 'image/webp'];
    if (!allowedTypes.includes(file.type)) {
        alert('Invalid file type. Please select a JPG, PNG, GIF, or WebP image.');
        return;
    }

    // Validate file size (5MB limit)
    if (file.size > 5 * 1024 * 1024) {
        alert('File size too large. Please select an image smaller than 5MB.');
        return;
    }

    // Show preview
    const reader = new FileReader();
    reader.onload = (e) => {
        const dropzone = document.getElementById(`${type}-image-dropzone`);
        const preview = document.getElementById(`${type}-image-preview`);
        const actions = dropzone.parentElement.querySelector('.image-actions');
        const content = dropzone.querySelector('.dropzone-content');

        preview.src = e.target.result;
        preview.style.display = 'block';
        content.style.display = 'none';
        actions.style.display = 'block';
        dropzone.classList.add('has-image');
    };
    reader.readAsDataURL(file);
}

function removeAddImage() {
    const dropzone = document.getElementById('add-image-dropzone');
    const preview = document.getElementById('add-image-preview');
    const fileInput = document.getElementById('add-product-image');
    const actions = dropzone.parentElement.querySelector('.image-actions');
    const content = dropzone.querySelector('.dropzone-content');

    preview.style.display = 'none';
    preview.src = '';
    content.style.display = 'block';
    actions.style.display = 'none';
    fileInput.value = '';
    dropzone.classList.remove('has-image');
}

function removeEditImage() {
    const dropzone = document.getElementById('edit-image-dropzone');
    const preview = document.getElementById('edit-image-preview');
    const fileInput = document.getElementById('edit-product-image');
    const actions = dropzone.parentElement.querySelector('.image-actions');
    const content = dropzone.querySelector('.dropzone-content');

    preview.style.display = 'none';
    preview.src = '';
    content.style.display = 'block';
    actions.style.display = 'none';
    fileInput.value = '';
    dropzone.classList.remove('has-image');
}