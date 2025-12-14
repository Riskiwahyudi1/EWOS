document.addEventListener('DOMContentLoaded', () => {
    const tableBody = document.querySelector('table.table tbody');
    const searchBox = document.getElementById('searchBox');

    if (!tableBody || !searchBox) return;

    const initialHTML = tableBody.innerHTML;

    let debounceTimer = null;
    let loadingTimer = null;

    // ---------------------------------
    // Render Table
    // ---------------------------------
    function renderTable(data) {

        if (!Array.isArray(data) || data.length === 0) {
            tableBody.innerHTML = `
                <tr>
                    <td colspan="9" class="text-center text-muted">
                        Tidak ada data Machine.
                    </td>
                </tr>`;
            return;
        }

        let html = '';

        data.forEach((m, idx) => {
            const id = m.id ?? '';
            const machineName = m.machineName ?? '';
            const machineCategory = m.categoryName ?? '';
            const machinePower = m.machinePower ?? '';
            const machineStatus = m.isActive ? 'Active' : 'Nonactive';
            const createdAt = m.createdAt ?? '';
            const updatedAt = m.updatedAt ?? '';

            html += `
                <tr>
                    <td class="text-center">${idx + 1}</td>
                    <td>${machineName}</td>
                    <td>${machineCategory}</td>
                    <td>${machinePower}</td>
                    <td>${machineStatus}</td>
                    <td>${createdAt}</td>
                    <td>${updatedAt}</td>
                    <td class="text-center">
                        <button class="btn btn-warning btn-sm open-modal"
                                data-url="/AdminSystem/Machine/LoadData?Id=${id}&type=Edit">
                            Edit
                        </button>
                    </td>
                </tr>`;
        });

        tableBody.innerHTML = html;
    }

    // ---------------------------------
    // Perform Search
    // ---------------------------------
    async function performSearch() {
        const keyword = searchBox.value.trim();

        // Jika kosong → kembalikan data awal
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
            const url = new URL("/AdminSystem/Machine/Search", window.location.origin);
            url.searchParams.set("keyword", keyword);

            const res = await fetch(url);
            if (!res.ok) throw new Error('Network response was not ok');

            const data = await res.json();

            clearTimeout(loadingTimer);

            renderTable(data);

        } catch (err) {
            console.error('Search error:', err);

            clearTimeout(loadingTimer);

            tableBody.innerHTML = `
                <tr>
                    <td colspan="9" class="text-center text-muted">
                        Terjadi kesalahan saat mencari.
                    </td>
                </tr>`;
        }
    }

    // ---------------------------------
    // Event Listener (Debounce)
    // ---------------------------------
    searchBox.addEventListener('input', () => {
        clearTimeout(debounceTimer);
        debounceTimer = setTimeout(performSearch, 350);
    });
});
