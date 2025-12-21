document.addEventListener('DOMContentLoaded', () => {
    const tableBody = document.querySelector('table.table tbody');
    const searchBox = document.getElementById('searchBox');
    const categoryFilter = document.querySelector('select[name="MachineCategoryId"]');
    const tabs = document.querySelectorAll(".nav-link");
    let currentStatus = ['WaitingApproval'];
    const loadingText = document.getElementById('loadingText');

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
        data.forEach((rq, idx) => {
            let buttons = `
                    <div class="dropstart d-inline-block">
                        <button class="btn btn-info btn-sm dropdown-toggle"
                                type="button"
                                data-bs-toggle="dropdown">
                            Actions
                        </button>
                        <ul class="dropdown-menu p-2" style="min-width:170px">
                `;

                            // ==========================
                            // FABRICATION APPROVAL
                            // ==========================
                            if (rq.status === "FabricationApproval") {

                                buttons += `
                        <li>
                            <button type="button"
                                    class="btn btn-success btn-sm w-100 text-center open-modal"
                                    data-url="/AdminFabrication/Request/LoadData?id=${rq.id}&type=Approve">
                                Approve
                            </button>
                        </li>

                        <li class="mt-1">
                            <button type="button"
                                    class="btn btn-danger btn-sm w-100 text-center open-modal"
                                    data-url="/AdminFabrication/Request/LoadData?id=${rq.id}&type=Reject">
                                Reject
                            </button>
                        </li>
                    `;
                            }

                            // ==========================
                            // WAITING FABRICATION
                            // ==========================
                            else if (rq.status === "WaitingFabrication") {

                                const externalCostIsNull = rq.externalFabCost === null;
                                const fabTimeIsNull = rq.fabricationTime === null;
                                const rawMaterialIsNull = rq.rawMaterialId === null;
                                const weightIsNull = rq.weight === null;

                                if (rq.machineCategoryId === 1 || rq.machineCategoryId === 2) {

                                    const isDisabled =
                                        rq.machineCategoryId === 1
                                            ? (externalCostIsNull || fabTimeIsNull || rawMaterialIsNull)
                                            : (externalCostIsNull || fabTimeIsNull || rawMaterialIsNull || weightIsNull);

                                    buttons += `
                            <li>
                                <button type="button"
                                        class="btn btn-primary btn-sm w-100 text-center open-modal ${isDisabled ? "disabled" : ""}"
                                        data-url="/AdminFabrication/Request/LoadData?id=${rq.id}&type=Fabrikasi">
                                    Fabrikasi
                                </button>
                            </li>

                            <li class="mt-1">
                                <button type="button"
                                        class="btn btn-warning btn-sm w-100 text-center open-modal"
                                        data-url="/AdminFabrication/Request/LoadData?id=${rq.id}&type=Edit">
                                    Edit
                                </button>
                            </li>
                        `;
                                }

                                else if (rq.machineCategoryId === 3) {

                                    buttons += `
                            <li>
                                <button type="button"
                                        class="btn btn-primary btn-sm w-100 text-center open-modal"
                                        data-url="/AdminFabrication/Request/LoadData?id=${rq.id}&type=Fabrikasi">
                                    Drawing
                                </button>
                            </li>
                        `;
                                }
                            }

                            // ==========================
                            // DETAIL (ALWAYS AVAILABLE)
                            // ==========================
                            buttons += `
                        <li class="mt-1">
                            <button type="button"
                                    class="btn btn-info btn-sm w-100 text-center open-modal"
                                    data-url="/AdminFabrication/Request/LoadData?id=${rq.id}&type=Detail">
                                Detail
                            </button>
                        </li>
                    </ul>
                </div>
                `;


            html += `
                        <tr>
                            <td class="text-center">${idx + 1}</td>
                            <td>${rq.users ?? ''}</td>
                            <td>${rq.partName ?? rq.PartName ?? ''}</td>
                            <td>${rq.categoryName ?? ''}</td>
                            <td>${rq.crd ? new Date(rq.crd).toLocaleDateString('id-ID') : ''}</td>
                            <td>${rq.createdAt ? new Date(rq.createdAt).toLocaleDateString('id-ID') : ''}</td>
                            <td class="text-center">${buttons}</td>
                        </tr>
                    `;
        });

        tableBody.innerHTML = html;
    }
    // -------------------------------
    // MAIN FUNCTION: performSearch
    // -------------------------------
    async function performSearch() {

        const keyword = searchBox.value.trim();
        const categoryId = categoryFilter.value;

        if (keyword.length === 0 && !categoryId && !currentStatus) {
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

            // Buat URL dan query
            const url = new URL("/AdminFabrication/Request/SearchNew", window.location.origin);
            if (keyword) url.searchParams.append("keyword", keyword);
            if (categoryId && categoryId !== "0") url.searchParams.append("categoryId", categoryId);
            if (currentStatus) url.searchParams.append("status", currentStatus.replace('#', ''));

            const res = await fetch(url);
            if (!res.ok) throw new Error('Network response was not ok');

            const data = await res.json();

            clearTimeout(loadingTimer);

            renderTable(data);

        } catch (err) {
            console.error('Search error:', err);
            if (loadingText) loadingText.style.display = 'none';
            tableBody.innerHTML = `<tr><td colspan="7" class="text-center text-danger">Terjadi kesalahan saat mencari.</td></tr>`;
        }

    }


    // -------------------------------
    // Event Listeners
    // -------------------------------

    //search
    searchBox.addEventListener('input', () => {
        clearTimeout(debounceTimer);
        debounceTimer = setTimeout(performSearch, 350);
    });

    // Event filter kategori
    categoryFilter.addEventListener('change', performSearch);

    // event tab
    tabs.forEach(tab => {
        tab.addEventListener("click", function () {
            currentStatus = this.getAttribute("data-bs-target");
            performSearch();
        });
    });
});