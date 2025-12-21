document.addEventListener('DOMContentLoaded', () => {
    const tableBody = document.querySelector('table.table tbody');
    const searchBox = document.getElementById('searchBox');
    const YearsFilter = document.querySelector('select[name="YearSettingId"]');
    const generateBtn = document.getElementById('btn-generate-week');
    const idYear = document.getElementById('YearSettingId');
    const paginationClient = document.getElementById("pagination-client");
    const paginationNumber = document.getElementById("pagination-js");
    const paginationServer = document.getElementById("pagination-server");

    let currentPage = 1;
    let totalPages = 1;
    const pageSize = 10;

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
                        <td colspan="7" class="text-center text-muted">Tidak ada data.</td>
                    </tr>`;
            return;
        }

        let html = "";

        data.forEach((w, idx) => {

            const rowNumber = (currentPage - 1) * pageSize + idx + 1;

            html += `
              <tr>
                <td class="text-center">${rowNumber}</td>
                <td>Week ${w.week}</td>
                <td>${w.dayCount}</td>
                <td>${w.startDate}</td>
                <td>${w.endDate}</td>
                <td class="text-center">
                  <button class="btn btn-warning btn-sm open-modal"
                            data-url="/AdminSystem/WeekFabrication/LoadData?Id=${w.id}&type=Edit">
                        Edit
                    </button>
                </td>
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
        const YearSettingId = YearsFilter ? YearsFilter.value : "";

        if (!tableBody || !searchBox) return;

        clearTimeout(loadingTimer);
        loadingTimer = setTimeout(() => {
            tableBody.innerHTML = `
                    <tr><td colspan="7" class="text-center text-primary">Memuat data...</td></tr>
                `;
        }, 350);

        try {

            const url = new URL("/AdminSystem/WeekFabrication/Search", window.location.origin);
            if (keyword) url.searchParams.append("keyword", keyword);
            if (YearSettingId && YearSettingId !== "0") url.searchParams.append("YearSettingId", YearSettingId);
            const res = await fetch(url);
            if (!res.ok) throw new Error('Network response was not ok');

            const data = await res.json();

            if (Array.isArray(data) && data.length > 0) {
                generateBtn.disabled = true;
            } else {
                generateBtn.disabled = false;
            }

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
            console.error('Search error:', err);
            tableBody.innerHTML = `<tr><td colspan="6" class="text-center text-muted">Terjadi kesalahan saat mencari.</td></tr>`;
        }
    }

    // -------------------------------
    // Event Listeners
    // -------------------------------

    //  Event ketika mengetik di search box
    searchBox.addEventListener('input', function () {
        const keyword = this.value.trim();
        if (keyword.length === 0 && (!YearsFilter || YearsFilter.value === "0")) {
            tableBody.innerHTML = initialHTML;
            return;
        }

        clearTimeout(debounceTimer);
        debounceTimer = setTimeout(performSearch, 350);
    });

    // Event ketika mengubah dropdown 
    if (YearsFilter) {
        YearsFilter.addEventListener('change', function () {
            idYear.value = YearsFilter.value
            performSearch();
        });
    }
});