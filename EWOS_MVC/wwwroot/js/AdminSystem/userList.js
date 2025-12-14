document.addEventListener('DOMContentLoaded', () => {

    const tableBody = document.querySelector('table.table tbody');
    const searchBox = document.getElementById('searchBox');

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
                        Tidak ada data user.
                    </td>
                </tr>`;
            return;
        }

        let html = '';

        data.forEach((u, idx) => {

            const id = u.id ?? '';
            const userName = u.userName ?? '';
            const name = u.name ?? '';
            const badge = u.badge ?? '';
            const email = u.email ?? '';
            const status = u.isActive ? 'Aktif' : 'Nonaktif';
            const createdAt = u.createdAt ?? '';
            const updatedAt = u.updatedAt ?? '';

            html += `
                <tr>
                    <td class="text-center">${idx + 1}</td>
                    <td>${userName}</td>
                    <td>${name}</td>
                    <td>${badge}</td>
                    <td>${email}</td>
                    <td>${status}</td>
                    <td>${createdAt}</td>
                    <td>${updatedAt}</td>
                    <td class="text-center">
                        <button class="btn btn-warning btn-sm open-modal"
                                data-url="/AdminSystem/UserList/LoadData?Id=${id}&type=Edit">
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

        if (keyword.length === 0) {
            clearTimeout(loadingTimer);
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

            const url = new URL("/AdminSystem/UserList/Search", window.location.origin);
            url.searchParams.set("keyword", keyword);

            if (keyword) url.searchParams.append("keyword", keyword);

            const res = await fetch(url);
            if (!res.ok) throw new Error('Network response was not ok');

            const data = await res.json();
            clearTimeout(loadingTimer);

            renderTable(data);

        } catch (err) {
            console.error('Search error:', err);

            tableBody.innerHTML = `
                <tr>
                    <td colspan="9" class="text-center text-danger">
                        Terjadi kesalahan saat mencari data.
                    </td>
                </tr>`;
        }
    }

    // ---------------------------------
    // Event listener (debounce)
    // ---------------------------------
    searchBox.addEventListener('input', () => {
        clearTimeout(debounceTimer);
        debounceTimer = setTimeout(performSearch, 350);
    });
});
