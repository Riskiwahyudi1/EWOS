
document.addEventListener('DOMContentLoaded', () => {

    const tableBody = document.querySelector('table.table tbody');
    const searchBox = document.getElementById('searchBox');
    const categoryFilter = document.querySelector('select[name="MachineCategoryId"]');
    const tabs = document.querySelectorAll('#statusTabs .nav-link');
    const paginationClient = document.getElementById("pagination-client");
    const paginationNumber = document.getElementById("pagination-js");
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
            let buttons = `
                <div class="dropstart d-inline-block">
                    <button class="btn btn-secondary btn-sm dropdown-toggle"
                            type="button"
                            data-bs-toggle="dropdown">
                        Actions
                    </button>

                    <ul class="dropdown-menu p-2" style="min-width:160px">
                `;

                            // === Request (hanya status Maspro)
                            if (rq.status === "Maspro") {
                                buttons += `
                        <li>
                            <button class="btn btn-success btn-sm w-100 text-center open-modal"
                                    data-url="/Requestor/ListProduct/LoadData?Id=${id}&type=Request">
                                Request
                            </button>
                        </li>
                    `;
                            }

                            // === Edit (role)
                            if (canEdit) {
                                buttons += `
                        <li class="mt-1">
                            <button class="btn btn-warning btn-sm w-100 text-center open-modal"
                                    data-url="/Requestor/ListProduct/LoadData?Id=${id}&type=Edit">
                                Edit
                            </button>
                        </li>
                    `;
                            }

                            // === Detail (selalu ada)
                            buttons += `
                        <li class="mt-1">
                            <button class="btn btn-secondary btn-sm w-100 text-center open-modal"
                                    data-url="/Requestor/ListProduct/LoadData?Id=${id}&type=Detail">
                                Detail
                            </button>
                        </li>

                        <li class="mt-1">
                            <button class="btn btn-info btn-sm w-100 text-center open-modal"
                                    data-url="/Requestor/ListProduct/LoadData?Id=${id}&type=Status">
                                Status
                            </button>
                        </li>
                    </ul>
                </div>
                `;


            html += `
                <tr>
                    <td class="text-center">${idx + 1}</td>
                    <td>${rq.id ?? '-'}</td>
                    <td>${rq.requestor ?? '-'}</td>
                    <td style=" max-width: 300px; white-space: nowrap; overflow: hidden; text-overflow: ellipsis; ">${rq.partName ?? '-'}</td>
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
        let maxPagesToShow = 5;
        let startPage = currentPage - 2;
        let endPage = currentPage + 2;

        // Koreksi start
        if (startPage < 1) {
            endPage += (1 - startPage);
            startPage = 1;
        }

        // Koreksi end
        if (endPage > totalPages) {
            startPage -= (endPage - totalPages);
            endPage = totalPages;
        }

        // Pastikan max tombol tetap 5
        if (endPage - startPage + 1 > maxPagesToShow) {
            endPage = startPage + maxPagesToShow - 1;
        }
        if (startPage < 1) startPage = 1;

        let html = `
        <ul class="pagination justify-content-end">
            <li class="page-item ${currentPage == 1 ? "disabled" : ""}">
                <a class="page-link" data-page="prev">Previous</a>
            </li>
    `;

        // Jika tidak mulai dari 1 → tampilkan "1" dan "..."
        if (startPage > 1) {
            html += `
            <li class="page-item">
                <a class="page-link" data-page="1">1</a>
            </li>
            <li class="page-item disabled">
                <span class="page-link">...</span>
            </li>
        `;
        }

        // Nomor halaman utama
        for (let i = startPage; i <= endPage; i++) {
            html += `
            <li class="page-item ${i === currentPage ? "active" : ""}">
                <a class="page-link" data-page="${i}">${i}</a>
            </li>
        `;
        }

        // Jika tidak berakhir pada lastPage → tampilkan "..." dan lastPage
        if (endPage < totalPages) {
            html += `
                <li class="page-item disabled">
                    <span class="page-link">...</span>
                </li>
                <li class="page-item ${currentPage === totalPages ? "active" : ""}">
                    <a class="page-link" data-page="${totalPages}">${totalPages}</a>
                </li>
            `;
        }

        html += `
            <li class="page-item ${currentPage == totalPages ? "disabled" : ""}">
                <a class="page-link" data-page="next">Next</a>
            </li>
        </ul>
    `;

        paginationNumber.innerHTML = html;

        // Event listener
        paginationNumber.querySelectorAll(".page-link").forEach(btn => {
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