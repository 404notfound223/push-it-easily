document.addEventListener("DOMContentLoaded", () => {
    const searchForm = document.querySelector(".search-form")
    const searchInput = searchForm.querySelector('input[name="query"]') // Updated selector to use name attribute
    const searchButton = searchForm.querySelector('button[type="submit"]')

    let searchTimeout
    let searchResults = null
    let query = ""

    // Handle form submission
    searchForm.addEventListener("submit", (e) => {
        query = searchInput.value.trim()
        if (!query) {
            e.preventDefault()
            return false
        }
        // Form will submit naturally to /Home/Search with query parameter
    })

    // Handle live search as user types
    searchInput.addEventListener("input", function () {
        clearTimeout(searchTimeout)
        query = this.value.trim()

        if (query.length >= 2) {
            searchTimeout = setTimeout(() => {
                showLiveSearchResults(query)
            }, 300)
        } else {
            hideLiveSearchResults()
        }
    })

    // Hide results when clicking outside
    document.addEventListener("click", (e) => {
        if (!searchForm.contains(e.target)) {
            hideLiveSearchResults()
        }
    })

    function performSearch() {
        if (query) {
            searchInput.value = query
            searchForm.submit()
        }
    }

    function showLiveSearchResults(query) {
        fetch(`/Home/LiveSearch?term=${encodeURIComponent(query)}`)
            .then((response) => response.json())
            .then((data) => {
                if (data.success && data.results.length > 0) {
                    displayLiveResults(data.results)
                } else {
                    hideLiveSearchResults()
                }
            })
            .catch((error) => {
                console.error("Search error:", error)
                hideLiveSearchResults()
            })
    }

    function displayLiveResults(results) {
        hideLiveSearchResults() // Remove existing results first

        searchResults = document.createElement("div")
        searchResults.className = "live-search-results"

        let html = '<div class="search-results-header">Quick Results</div>'

        results.forEach((item) => {
            html += `
                <div class="search-result-item" onclick="selectSearchResult('${item.id}', '${item.name}', ${item.price})">
                    <div class="result-info">
                        <div class="result-name">${item.name}</div>
                        <div class="result-category">${item.category}</div>
                    </div>
                    <div class="result-price">$${item.price.toFixed(2)}</div>
                </div>
            `
        })

        html += `<div class="search-results-footer" onclick="performSearch()">
                    View all results for "${query}" →
                 </div>`

        searchResults.innerHTML = html
        searchForm.appendChild(searchResults)
    }

    function hideLiveSearchResults() {
        if (searchResults) {
            searchResults.remove()
            searchResults = null
        }
    }

    // Make selectSearchResult available globally
    window.selectSearchResult = (id, name, price) => {
        if (typeof window.addToOrder === "function") {
            // Declare addToOrder variable
            window.addToOrder(id, name, price)
        }
        hideLiveSearchResults()
        searchInput.value = ""
    }
})
