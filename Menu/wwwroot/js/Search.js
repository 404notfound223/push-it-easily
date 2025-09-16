document.addEventListener("DOMContentLoaded", () => {
    const searchForm = document.querySelector(".search-form")
    const searchInput = searchForm.querySelector('input[type="text"]')
    const searchButton = searchForm.querySelector('button[type="submit"]')

    let query = ""

    searchInput.addEventListener("input", (e) => {
        // Prevent any dropdown or live search behavior
        e.stopPropagation()
    })

    searchInput.addEventListener("focus", (e) => {
        e.target.setAttribute("autocomplete", "off")
    })

    searchForm.addEventListener("submit", (e) => {
        e.preventDefault()
        query = searchInput.value.trim()
        performSearch()
    })

    function performSearch() {
        if (query) {
            window.location.href = `/Home/Search?query=${encodeURIComponent(query)}`
        }
    }
})
