document.addEventListener('DOMContentLoaded', () => {
    const tableBody = document.querySelector('table.table tbody');
    const searchBox = document.getElementById('searchBox');

    if (!tableBody || !searchBox) return;

    let debounceTimer = null;
    let loadingTimer = null;

    const initialHTML = tableBody.innerHTML;

    // ---------------------------------
    // Render table
    // ---------------------------------
    function renderTable(data) {

        if (!Array.isArray(data) || data.length === 0) {
            tableBody.innerHTML = `
                <tr>
                    <td colspan="9" class="text-center text-muted">
                        Tidak ada data.
                    </td>
                </tr>`;
            return;
        }

        let html = '';

        data.forEach((y, idx) => {
            const id = y.id ?? '';
            const year = y.year ?? '';
            const electricalCost = Number(y.electricalCost ?? 0).toFixed(5);
            const startDate = y.startDate ?? '';
            const createdAt = y.createdAt ?? '';
            const updatedAt = y.updatedAt ?? '';

            html += `
                <tr>
                    <td class="text-center">${idx + 1}</td>
                    <td>${year}</td>
                    <td>${electricalCost}</td>
                    <td>${startDate}</td>
                    <td>${createdAt}</td>
                    <td>${updatedAt}</td>
                    <td class="text-center">
                        <button class="btn btn-warning btn-sm open-modal"
                            data-url="/AdminSystem/YearsSetting/LoadData?Id=${id}&type=Edit">
                            Edit
                        </button>
                    </td>
                </tr>`;
        });

        tableBody.innerHTML = html;
    }

    // ---------------------------------
    // Perform search
    // ---------------------------------
    async function performSearch() {
        const keyword = searchBox.value.trim();

        if (!keyword) {
            tableBody.innerHTML = initialHTML;
            return;
        }

        clearTimeout(loadingTimer);
        loadingTimer = setTimeout(() => {
            tableBody.innerHTML = `
                <tr><td colspan="9" class="text-center text-primary">Memuat data...</td></tr>
            `;
        }, 300);

        try {
            const url = new URL("/AdminSystem/YearsSetting/Search", window.location.origin);
            url.searchParams.set("keyword", keyword);

            const res = await fetch(url);
            if (!res.ok) throw new Error('Network response was not ok');

            const data = await res.json();

            console.log(data)
            clearTimeout(loadingTimer);

            renderTable(data);

        } catch (err) {
            console.error('Search error:', err);
            tableBody.innerHTML = `
                <tr><td colspan="9" class="text-center text-danger">
                    Terjadi kesalahan saat mencari.
                </td></tr>`;
        }
    }

    // ---------------------------------
    // Event listener (debounce)
    // ---------------------------------
    searchBox.addEventListener('input', () => {
        clearTimeout(debounceTimer);
        debounceTimer = setTimeout(performSearch, 350);
    });

    // ---------------------------------
    // List Year
    // ---------------------------------

    // Tahun awal
    const startYear = 2024;
    // Tahun akhir = tahun sekarang
    const endYear = new Date().getFullYear();
    const yearSelect = document.getElementById('yearSelect');

    for (let year = endYear; year >= startYear; year--) {
        let option = document.createElement('option');
        option.value = year;
        option.textContent = year;
        yearSelect.appendChild(option);
    }

    yearSelect.value = endYear;
});
