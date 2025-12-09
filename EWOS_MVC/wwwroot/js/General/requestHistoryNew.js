
document.addEventListener('DOMContentLoaded', () => {
    const tableBody = document.querySelector('table.table tbody');
    const searchBox = document.getElementById('searchBox');
    const categoryFilter = document.querySelector('select[name="MachineCategoryId"]');
    let currentStatusList = ['Maspro', 'Fail'];
    const loadingText = document.getElementById('loadingText');
    const tabs = document.querySelectorAll('#statusTabs .nav-link');

    if (!tableBody || !searchBox || !categoryFilter) return;

    const initialHTML = tableBody.innerHTML;
    let debounceTimer = null;
    let loadingTimer = null;

    async function performSearch() {
        const keyword = searchBox.value.trim();
        const categoryId = categoryFilter.value;

        if (keyword.length === 0 && !categoryId && (!currentStatusList || currentStatusList.length === 0)) {
            tableBody.innerHTML = initialHTML;
            return;
        }

        // Hapus timer sebelumnya
        clearTimeout(debounceTimer);

        // Set delay
        debounceTimer = setTimeout(async () => {
            try {
                // tampilkan indikator loading
                clearTimeout(loadingTimer);
                loadingTimer = setTimeout(() => {
                    if (loadingText) loadingText.style.display = 'block';
                }, 350);

                // Buat URL dan query
                const url = new URL("/RequestHistory/SearchNew", window.location.origin);
                if (keyword) url.searchParams.append("keyword", keyword);
                if (categoryId && categoryId !== "0") url.searchParams.append("categoryId", categoryId);

                // kirim semua status (bisa 1 atau banyak)
                currentStatusList.forEach(s => url.searchParams.append("status", s.trim()));
                const res = await fetch(url);
                if (!res.ok) throw new Error('Network response was not ok');

                const data = await res.json();
                clearTimeout(loadingTimer);
                if (loadingText) loadingText.style.display = 'none';

                if (!Array.isArray(data) || data.length === 0) {
                    tableBody.innerHTML = `<tr><td colspan="7" class="text-center text-muted">Tidak ada data Request.</td></tr>`;
                    return;
                }

                // Generate HTML
                let html = '';
                data.forEach((rq, idx) => {
                    let buttons = "";
                    buttons += `
    <button class="btn btn-warning btn-sm open-modal"
        data-url="/RequestHistory/LoadData?id=${rq.id}&type=Detail">
        Detail
    </button>


    <button class="btn btn-info btn-sm open-modal"
        data-url="/RequestHistory/LoadDataFab?id=${rq.id}&type=Status">
        Progress
    </button>
    `;

                    html += `
    <tr>
        <td class="text-center">${idx + 1}</td>
        <td>${rq.id ?? ''}</td>
        <td>${rq.users ?? ''}</td>
        <td>${rq.partName ?? rq.PartName ?? ''}</td>
        <td>${rq.categoryName ?? '-'}</td>
        <td>${rq.createdAt ? new Date(rq.createdAt).toLocaleDateString('id-ID') : ''}</td>
        <td class="text-center">${buttons}</td>
    </tr>
    `;
                });

                tableBody.innerHTML = html;

            } catch (err) {
                console.error('Search error:', err);
                if (loadingText) loadingText.style.display = 'none';
                tableBody.innerHTML = `<tr><td colspan="7" class="text-center text-danger">Terjadi kesalahan saat mencari.</td></tr>`;
            }
        }, 200);
    }


    // Event search dengan debounce
    searchBox.addEventListener('input', () => {
        clearTimeout(debounceTimer);
        debounceTimer = setTimeout(performSearch, 350);
    });

    // Tab change (Bootstrap event)
    tabs.forEach(tab => {
        tab.addEventListener('shown.bs.tab', event => {
            const statusAttr = event.target.getAttribute('data-status');
            currentStatusList = statusAttr ? statusAttr.split(',').map(s => s.trim()) : [];
            performSearch();
        });
    });

    // Event filter kategori
    categoryFilter.addEventListener('change', performSearch);


});
