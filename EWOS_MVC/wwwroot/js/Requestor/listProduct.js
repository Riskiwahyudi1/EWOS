
document.addEventListener('DOMContentLoaded', () => {

    const tableBody = document.querySelector('table.table tbody');
    const searchBox = document.getElementById('searchBox');
    const categoryFilter = document.querySelector('select[name="MachineCategoryId"]');
    const tabs = document.querySelectorAll('#statusTabs .nav-link');
    const paginationClient = document.getElementById("pagination-client");
    const paginationServer = document.getElementById("pagination-server");

    let currentPage = 1;
    let totalPages = 1;
    const pageSize = 20;

    let currentStatusList = ['Maspro'];
    let debounceTimer = null;
    let loadingTimer = null;

    const initialHTML = tableBody.innerHTML;

    // -------------------------------
    // Helper: Render Table
    // -------------------------------
    function renderTable(data) {
        if (!data || data.length === 0) {
            tableBody.innerHTML = `
                    <tr>
                        <td colspan="7" class="text-center text-muted">Tidak ada data Request.</td>
                    </tr>`;
            return;
        }

        let html = "";
        const canEdit = window.canEdit === true;

        data.forEach((rq, idx) => {
            const id = rq.id ?? '';
            let buttons = "";

            if (rq.status == "Maspro") {
                buttons += `<button class="btn btn-success btn-sm open-modal"
                                    data-url="/Requestor/ListProduct/LoadData?Id=${id}&type=Request">Request</button>`;
            }

            if (canEdit) {
                buttons += `<button class="btn btn-warning btn-sm open-modal"
                                    data-url="/Requestor/ListProduct/LoadData?Id=${id}&type=Edit">Edit</button>`;
            }

            buttons += `
                    <button class="btn btn-secondary btn-sm open-modal"
                        data-url="/Requestor/ListProduct/LoadData?Id=${id}&type=Detail">Detail</button>
                    <button class="btn btn-info btn-sm open-modal"
                        data-url="/Requestor/ListProduct/LoadData?Id=${id}&type=Status">Status</button>
                `;

            html += `
                <tr>
                    <td class="text-center">${idx + 1}</td>
                    <td>${rq.id ?? '-'}</td>
                    <td>${rq.requestor ?? '-'}</td>
                    <td>${rq.partName ?? '-'}</td>
                    <td>${rq.categoryName ?? '-'}</td>
                    <td>${rq.createdAt ? new Date(rq.createdAt).toLocaleDateString('en-GB').replace(/\//g, '-') : '-'}</td>
                    <td class="text-center">${buttons}</td>
                </tr>`;
        });

        tableBody.innerHTML = html;
    }

    // -------------------------------
    // Helper: Render Pagination
    // -------------------------------
    function renderPagination() {
        paginationClient.innerHTML = `
                <ul class="pagination justify-content-end">
                    <li class="page-item ${currentPage == 1 ? "disabled" : ""}">
                        <a class="page-link" data-page="prev">Previous</a>
                    </li>

                    ${Array.from({ length: totalPages }, (_, i) => `
                        <li class="page-item ${currentPage == i + 1 ? "active" : ""}">
                            <a class="page-link" data-page="${i + 1}">${i + 1}</a>
                        </li>`).join("")}

                    <li class="page-item ${currentPage == totalPages ? "disabled" : ""}">
                        <a class="page-link" data-page="next">Next</a>
                    </li>
                </ul>
            `;

        paginationClient.querySelectorAll(".page-link").forEach(btn => {
            btn.addEventListener("click", () => {
                const p = btn.dataset.page;

                if (p === "prev" && currentPage > 1) currentPage--;
                else if (p === "next" && currentPage < totalPages) currentPage++;
                else if (!isNaN(parseInt(p))) currentPage = parseInt(p);

                performSearch();
            });
        });
    }

    // -------------------------------
    // MAIN FUNCTION: performSearch
    // -------------------------------
    async function performSearch() {

        const keyword = searchBox.value.trim();
        const categoryId = categoryFilter ? categoryFilter.value : "";

        // No filters → kembali initial
        if (!keyword && !categoryId && currentStatusList.length === 0) {
            tableBody.innerHTML = initialHTML;
            return;
        }

        clearTimeout(loadingTimer);
        loadingTimer = setTimeout(() => {
            tableBody.innerHTML = `
                    <tr><td colspan="7" class="text-center text-primary">Memuat data...</td></tr>
                `;
        }, 350);

        try {
            // Create URL
            const url = new URL("/Requestor/ListProduct/Search", window.location.origin);
            if (keyword) url.searchParams.append("keyword", keyword);
            if (categoryId && categoryId !== "0") url.searchParams.append("categoryId", categoryId);
            currentStatusList.forEach(s => url.searchParams.append("status", s.trim()));

            const res = await fetch(url);
            const data = await res.json();

            clearTimeout(loadingTimer);

            paginationServer.style.display = "none";
            paginationClient.style.display = "block";

            // Paging Calculation
            totalPages = Math.max(1, Math.ceil(data.length / pageSize));
            if (currentPage > totalPages) currentPage = 1;

            // HIDE pagination if data < pageSize
            if (data.length <= pageSize) {
                paginationClient.style.display = "none";
            } else {
                paginationClient.style.display = "block";
                renderPagination();
            }

            const start = (currentPage - 1) * pageSize;
            const pageData = data.slice(start, start + pageSize);

            renderTable(pageData);

        } catch (err) {
            console.error("Search error:", err);
            tableBody.innerHTML = `<tr>
                    <td colspan="7" class="text-center text-danger">Terjadi kesalahan.</td>
                </tr>`;
        }
    }

    // -------------------------------
    // Event Listeners
    // -------------------------------
    searchBox.addEventListener('input', () => {
        clearTimeout(debounceTimer);
        debounceTimer = setTimeout(performSearch, 350);
    });

    if (categoryFilter)
        categoryFilter.addEventListener("change", performSearch);

    tabs.forEach(tab => {
        tab.addEventListener("shown.bs.tab", e => {
            const s = e.target.getAttribute("data-status");
            currentStatusList = s ? s.split(',').map(x => x.trim()) : [];
            currentPage = 1;
            performSearch();
        });
    });

});