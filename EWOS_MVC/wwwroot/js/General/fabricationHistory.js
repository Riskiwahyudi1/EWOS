document.addEventListener('DOMContentLoaded', () => {
    const tableBody = document.querySelector('table.table tbody');
    const searchBox = document.getElementById('searchBox');
    const categoryFilter = document.querySelector('select[name="MachineCategoryId"]');
    const mcFilter = document.querySelector('select[name="MachineId"]');
    const weekFab = document.querySelector('select[name="WeekSettingId"]')
    const yearFab = document.querySelector('select[name="YearSettingId"]')
    const monthSelect = document.querySelector('select[name="MonthSelect"]');

    const currentStatus = [''];
    let debounceTimer = null;
    let loadingTimer = null;

    const initialHTML = tableBody.innerHTML;

    // -------------------------------
    // Helper: filter mesin fabrikasi
    // -------------------------------

    async function loadMachine() {
        const MachineCategoryId = categoryFilter?.value;

        if (!MachineCategoryId) return;

        try {
            mcFilter.innerHTML = '<option value="">-- Pilih Mesin --</option>';
            const response = await fetch(`/MachineList?MachineCategoryId=${MachineCategoryId}`);

            if (!response.ok) throw new Error("Gagal fetch view component");
            const html = await response.text();
            mcFilter.innerHTML = html;
            await performSearch();
        } catch (error) {
            console.error("Gagal memuat minggu:", error);
        }
    }


    // -------------------------------
    // Helper: filter week fabrikasi
    // -------------------------------

    async function loadWeeks() {
        const YearSettingId = yearFab?.value;
        const MonthSelect = monthSelect?.value;

        if (!YearSettingId || !MonthSelect) return;

        try {
            const response = await fetch(`/WeeksSetting?YearSettingId=${YearSettingId}&MonthSelect=${MonthSelect}`);
            if (!response.ok) throw new Error("Gagal fetch view component");
            const html = await response.text();

            weekFab.innerHTML = html;
        } catch (error) {
            console.error("Gagal memuat minggu:", error);
        }
    }


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

        let html = '';
        data.forEach((rq, idx) => {

            let buttons = "";
            const status = rq.status?.trim().toLowerCase();
            let isAdmin = window.isAdminFabrication === true;
            const isEvaluation = rq.repeatOrderId === null;

            // MULAI DROPDOWN
            buttons += `
    <div class="dropstart">
            <button class="btn btn-secondary btn-sm dropdown-toggle"
            type="button"
            data-bs-toggle="dropdown"
            data-bs-display="static">
        Actions
    </button>


        <ul class="dropdown-menu p-2" style="min-width: 150px z-index: 9999;">
    `;

            if (status === "onprogress" && isAdmin) {

                buttons += `
            <li>
                <button class="btn btn-primary btn-sm w-100 text-center open-modal"
                    data-url="/FabricationHistory/LoadData?id=${rq.id}&type=Finish">
                    Done
                </button>
            </li>

            <li class="mt-1">
                <button class="btn btn-danger btn-sm w-100 text-center open-modal"
                    data-url="/FabricationHistory/LoadData?id=${rq.id}&type=Cancel">
                    Cancel
                </button>
            </li>
        `;

                if (!isEvaluation) {
                    buttons += `
                <li class="mt-1">
                    <button class="btn btn-warning btn-sm w-100 text-center open-modal"
                        data-url="/FabricationHistory/LoadData?id=${rq.id}&type=Edit">
                        Edit
                    </button>
                </li>
            `;
                } else {
                    buttons += `
                <li class="mt-1">
                    <button class="btn btn-warning btn-sm w-100 text-center open-modal"
                        data-url="/FabricationHistory/LoadData?id=${rq.id}&type=evaluasi">
                        Evaluasi
                    </button>
                </li>
            `;
                }
            }
            else if (status === "evaluation" && isAdmin) {

                buttons += `
            <li class="mt-1">
                <button class="btn btn-danger btn-sm w-100 text-center open-modal"
                    data-url="/FabricationHistory/LoadData?id=${rq.id}&type=CancelEval">
                    Cancel Evaluation
                </button>
            </li>
        `;
            }
            else if (status === "fabricationdone" && isAdmin && rq.machineCategory === 1) {

                buttons += `
            <li class="mt-1">
                <button class="btn btn-warning btn-sm w-100 text-center open-modal"
                    data-url="/FabricationHistory/LoadData?id=${rq.id}&type=updateCOC">
                    Edit COC
                </button>
            </li>
        `;
            }

            // Tombol detail selalu ada
            buttons += `
            <li class="mt-1">
                <button class="btn btn-info btn-sm w-100 text-center open-modal"
                    data-url="/FabricationHistory/LoadData?id=${rq.id}&type=Detail">
                    Detail
                </button>
            </li>
        </ul>
    </div>
    `;


            html += `
                           <tr>
                               <td class="text-center">${idx + 1}</td>
                               <td>${rq.partName ?? rq.PartName ?? ''}</td>
                               <td>${rq.quantity ?? rq.Quantity ?? 0}</td>
                               <td>${rq.totalSaving ?? rq.TotalSaving ?? ''}</td>
                               <td>${rq.fabricationTime ?? rq.FabricationTime ?? ''}</td>
                               <td>${rq.status ?? rq.Status ?? ''}</td>
                               <td class="text-center">${buttons}</td>
                           </tr>
                       `;
        });

        tableBody.innerHTML = html;
    }
    // -------------------------------
    // MAIN FUNCTION: performSearch
    // -------------------------------

    window.performSearch = async function () {
        const keyword = searchBox.value.trim();
        const categoryId = categoryFilter.value;
        const weekSettingId = weekFab.value;
        const yearSettingId = yearFab.value;
        const MachineId = mcFilter.value;

        // Kalau kosong semua, kembalikan tabel awal
        if (keyword.length === 0 && !categoryId && !weekSettingId && !MachineId && !yearSettingId) {
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




            const url = new URL("/FabricationHistory/Search", window.location.origin);
            if (keyword) url.searchParams.append("keyword", keyword);
            if (categoryId && categoryId !== "0") url.searchParams.append("categoryId", categoryId);
            if (MachineId && MachineId !== "0") url.searchParams.append("MachineId", MachineId);
            if (weekSettingId && weekSettingId !== "0") url.searchParams.append("weekSettingId", weekSettingId);
            if (yearSettingId && yearSettingId !== "0") url.searchParams.append("yearSettingId", yearSettingId);
            if (currentStatus) url.searchParams.append("status", currentStatus);

            const res = await fetch(url);
            if (!res.ok) throw new Error('Network response was not ok');

            const data = await res.json();
            clearTimeout(loadingTimer);

            if (data.utilization !== undefined && !isNaN(data.utilization)) {
                chart.updateSeries([data.utilization]);

                let label = 'All Machines';
                if (data.machineName) {
                    label = `${data.machineName}`;
                } else if (data.categoryName) {
                    label = `${data.categoryName}`;
                }

                chart.updateOptions({
                    labels: [label]
                });
            }

            renderTable(data.data);



        } catch (err) {
            console.error('Search error:', err);
            tableBody.innerHTML = `<tr><td colspan="7" class="text-center text-danger">Terjadi kesalahan saat mencari.</td></tr>`;
        }
    }

    // -------------------------------
    // Event Listeners
    // -------------------------------

    // Event search 
    searchBox.addEventListener('input', () => {
        clearTimeout(debounceTimer);
        debounceTimer = setTimeout(performSearch, 350);
    });

    // Event filter
    categoryFilter.addEventListener('change', performSearch);
    mcFilter.addEventListener('change', performSearch);
    weekFab.addEventListener('change', performSearch);
    yearFab.addEventListener('change', performSearch);

    yearFab?.addEventListener("change", loadWeeks);
    monthSelect?.addEventListener("change", loadWeeks);

    categoryFilter?.addEventListener("change", loadMachine);

});