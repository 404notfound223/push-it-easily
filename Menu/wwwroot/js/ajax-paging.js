class AjaxPaging {
    constructor(options) {
        this.containerId = options.containerId
        this.apiUrl = options.apiUrl
        this.templateFunction = options.templateFunction
        this.currentPage = 1
        this.pageSize = options.pageSize || 12
        this.searchTerm = ""
        this.category = ""
        this.sortBy = options.defaultSort || ""
        this.sortDirection = "asc"
        this.isLoading = false

        this.init()
    }

    init() {
        this.createPagingControls()
        this.loadPage(1)
    }

    createPagingControls() {
        const container = document.getElementById(this.containerId)
        if (!container) return

        // Create search and filter controls
        const controlsHtml = `
            <div class="paging-controls">
                <div class="search-filters">
                    <input type="text" id="search-input" placeholder="Search..." class="form-control" />
                    <select id="category-filter" class="form-control">
                        <option value="">All Categories</option>
                        <option value="Pizza">Pizza</option>
                        <option value="Pasta">Pasta</option>
                        <option value="Seafood">Seafood</option>
                        <option value="Burgers">Burgers</option>
                        <option value="Beverages">Beverages</option>
                        <option value="Desserts">Desserts</option>
                        <option value="Specials">Specials</option>
                    </select>
                    <select id="sort-filter" class="form-control">
                        <option value="">Default</option>
                        <option value="name">Name</option>
                        <option value="price">Price</option>
                        <option value="category">Category</option>
                    </select>
                </div>
                <div class="results-info">
                    <span id="results-count"></span>
                </div>
            </div>
            <div id="content-container"></div>
            <div id="pagination-container"></div>
            <div id="loading-spinner" class="loading-spinner" style="display: none;">
                <div class="spinner"></div>
                <span>Loading...</span>
            </div>
        `

        container.innerHTML = controlsHtml

        // Bind events
        this.bindEvents()
    }

    bindEvents() {
        const searchInput = document.getElementById("search-input")
        const categoryFilter = document.getElementById("category-filter")
        const sortFilter = document.getElementById("sort-filter")

        // Debounced search
        let searchTimeout
        searchInput?.addEventListener("input", (e) => {
            clearTimeout(searchTimeout)
            searchTimeout = setTimeout(() => {
                this.searchTerm = e.target.value
                this.loadPage(1)
            }, 500)
        })

        categoryFilter?.addEventListener("change", (e) => {
            this.category = e.target.value
            this.loadPage(1)
        })

        sortFilter?.addEventListener("change", (e) => {
            this.sortBy = e.target.value
            this.loadPage(1)
        })
    }

    async loadPage(page) {
        if (this.isLoading) return

        this.isLoading = true
        this.currentPage = page
        this.showLoading(true)

        try {
            const params = new URLSearchParams({
                page: page,
                pageSize: this.pageSize,
                searchTerm: this.searchTerm || "",
                category: this.category || "",
                sortBy: this.sortBy || "",
                sortDirection: this.sortDirection,
            })

            const response = await fetch(`${this.apiUrl}?${params}`)
            const data = await response.json()

            if (data.success) {
                this.renderContent(data.result)
                this.renderPagination(data.result)
                this.updateResultsInfo(data.result)
            } else {
                this.showError(data.error || "Failed to load data")
            }
        } catch (error) {
            console.error("Paging error:", error)
            this.showError("Failed to load data")
        } finally {
            this.isLoading = false
            this.showLoading(false)
        }
    }

    renderContent(pagedResult) {
        const container = document.getElementById("content-container")
        if (!container) return

        if (pagedResult.items && pagedResult.items.length > 0) {
            container.innerHTML = pagedResult.items.map((item) => this.templateFunction(item)).join("")
        } else {
            container.innerHTML = '<div class="no-results">No items found</div>'
        }
    }

    renderPagination(pagedResult) {
        const container = document.getElementById("pagination-container")
        if (!container) return

        if (pagedResult.totalPages <= 1) {
            container.innerHTML = ""
            return
        }

        let paginationHtml = '<nav class="pagination-nav"><ul class="pagination">'

        // Previous button
        if (pagedResult.hasPreviousPage) {
            paginationHtml += `<li><button onclick="ajaxPaging.loadPage(${pagedResult.pageNumber - 1})" class="page-btn">Previous</button></li>`
        }

        // Page numbers
        const startPage = Math.max(1, pagedResult.pageNumber - 2)
        const endPage = Math.min(pagedResult.totalPages, pagedResult.pageNumber + 2)

        if (startPage > 1) {
            paginationHtml += `<li><button onclick="ajaxPaging.loadPage(1)" class="page-btn">1</button></li>`
            if (startPage > 2) {
                paginationHtml += '<li><span class="page-ellipsis">...</span></li>'
            }
        }

        for (let i = startPage; i <= endPage; i++) {
            const activeClass = i === pagedResult.pageNumber ? "active" : ""
            paginationHtml += `<li><button onclick="ajaxPaging.loadPage(${i})" class="page-btn ${activeClass}">${i}</button></li>`
        }

        if (endPage < pagedResult.totalPages) {
            if (endPage < pagedResult.totalPages - 1) {
                paginationHtml += '<li><span class="page-ellipsis">...</span></li>'
            }
            paginationHtml += `<li><button onclick="ajaxPaging.loadPage(${pagedResult.totalPages})" class="page-btn">${pagedResult.totalPages}</button></li>`
        }

        // Next button
        if (pagedResult.hasNextPage) {
            paginationHtml += `<li><button onclick="ajaxPaging.loadPage(${pagedResult.pageNumber + 1})" class="page-btn">Next</button></li>`
        }

        paginationHtml += "</ul></nav>"
        container.innerHTML = paginationHtml
    }

    updateResultsInfo(pagedResult) {
        const container = document.getElementById("results-count")
        if (!container) return

        if (pagedResult.totalCount > 0) {
            container.textContent = `Showing ${pagedResult.startIndex}-${pagedResult.endIndex} of ${pagedResult.totalCount} items`
        } else {
            container.textContent = "No items found"
        }
    }

    showLoading(show) {
        const spinner = document.getElementById("loading-spinner")
        if (spinner) {
            spinner.style.display = show ? "flex" : "none"
        }
    }

    showError(message) {
        const container = document.getElementById("content-container")
        if (container) {
            container.innerHTML = `<div class="error-message">Error: ${message}</div>`
        }
    }
}

// Global variable for easy access
let ajaxPaging
