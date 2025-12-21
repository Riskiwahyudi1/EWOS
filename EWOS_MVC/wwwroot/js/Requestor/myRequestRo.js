document.addEventListener('DOMContentLoaded', () => {
    const tableBody = document.querySelector('table.table tbody');
    const searchBox = document.getElementById('searchBox');
    const loadingText = document.getElementById('loadingText');
    const categoryFilter = document.querySelector('select[name="MachineCategoryId"]');
    const tabs = document.querySelectorAll('#statusTabs .nav-link');
    const paginationNumber = document.getElementById("pagination-js");
    const paginationServer = document.getElementById("pagination-server");
    const paginationClient = document.getElementById("pagination-client");

    let currentStatusList = ['WaitingApproval', 'FabricationApproval'];
    let debounceTimer = null;
    let loadingTimer = null;
    let currentPage = 1;
    let totalPages = 1;
    const pageSize = 10;

    const initialHTML = tableBody.innerHTML;

    // -------------------------------
    // Helper: Render Table
    // -------------------------------
    function renderTable(data) {
        if (!data || data.length === 0) {
            tableBody.innerHTML = `
                    <tr>
                        <td colspan="7" class="text-center text-muted">Tidak ada data.</td>
                    </tr>`;
            return;
        }

        let html = "";

        data.forEach((rq, idx) => {
            const id = rq.id ?? '';
            const rowNumber = (currentPage - 1) * pageSize + idx + 1;
            let buttons = `
                <div class="dropstart d-inline-block">
                    <button class="btn btn-info btn-sm dropdown-toggle"
                            type="button"
                            data-bs-toggle="dropdown">
                        Actions
                    </button>

                    <ul class="dropdown-menu p-2" style="min-width:180px">
                `;

            // === Receive (hanya Done)
            if (rq.status === "Done") {
                buttons += `
                        <li class="mt-1">
                            <button class="btn btn-success btn-sm w-100 text-center open-modal"
                                    data-url="/Requestor/MyRequest/LoadDataRo?id=${id}&type=Recived">
                                <i class="bi bi-circular-check"></i> Receive
                            </button>
                        </li>
                    `;
            }

            // === Progress (Close, Done, WaitingFabrication)
            if (
                rq.status === "Close" ||
                rq.status === "Done" ||
                rq.status === "WaitingFabrication"
            ) {
                buttons += `
                                    <li class="mt-1">
                                        <button class="btn btn-secondary btn-sm w-100 text-center open-modal"
                                                data-url="/RequestHistory/LoadDataRoFab?id=${id}&type=Status">
                                            <i class="bi bi-ui-checks-grid"></i> Progress
                                        </button>
                                    </li>
                                `;
            }
            // === Detail (selalu)
            buttons += `
                        <li class="mt-1">
                            <button class="btn btn-warning btn-sm w-100 text-center open-modal"
                                    data-url="/Requestor/MyRequest/LoadDataRo?id=${id}&type=Detail">
                                <i class="bi bi-info-circle"></i> Detail
                            </button>
                        </li>

                        <li class="mt-1">
                            <button class="btn btn-info btn-sm w-100 text-center open-modal"
                                    data-url="/Requestor/MyRequest/LoadDataRo?id=${id}&type=Status">
                                <i class="bi bi-ticket-detailed"></i> Status
                            </button>
                        </li>
                `;



            buttons += `
                    </ul>
                </div>
                `;

            html += `
              <tr>
                <td class="text-center">${rowNumber}</td>
                <td>${rq.id ?? '-'}</td>
                <td>${rq.partName ?? '-'}</td>
                <td>${rq.categoryName ?? '-'}</td>
                <td>${rq.quantityReq ?? '0'}</td>
                <td>${rq.quantityDone ?? '0'}</td>
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
            console.log(totalPages, currentPage);

        });
    }


    async function performSearch() {
        const keyword = searchBox.value.trim();
        const categoryId = categoryFilter ? categoryFilter.value : "";

        // Jika tidak ada filter apapun, kembalikan tampilan awal
        if (keyword.length === 0 && !categoryId && (!currentStatusList || currentStatusList.length === 0)) {
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
            const url = new URL("/Requestor/MyRequest/SearchRo", window.location.origin);
            if (keyword) url.searchParams.append("keyword", keyword);
            if (categoryId && categoryId !== "0") url.searchParams.append("categoryId", categoryId);

            // kirim semua status (bisa 1 atau banyak)
            currentStatusList.forEach(s => url.searchParams.append("status", s.trim()));

            const res = await fetch(url);
            if (!res.ok) throw new Error('Network response was not ok');
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
            clearTimeout(loadingTimer);
            if (loadingText) loadingText.style.display = 'none';
            console.error('Search error:', err);
            tableBody.innerHTML = `<tr><td colspan="7" class="text-center text-danger">Terjadi kesalahan saat mencari.</td></tr>`;
        }
    }

    // Debounce input
    searchBox.addEventListener('input', () => {
        clearTimeout(debounceTimer);
        debounceTimer = setTimeout(performSearch, 350);
    });

    // Filter kategori
    if (categoryFilter) categoryFilter.addEventListener('change', performSearch);

    // Tab change (Bootstrap event)
    tabs.forEach(tab => {
        tab.addEventListener('shown.bs.tab', event => {
            const statusAttr = event.target.getAttribute('data-status');
            currentStatusList = statusAttr ? statusAttr.split(',').map(s => s.trim()) : [];
            performSearch();
        });
    });
});