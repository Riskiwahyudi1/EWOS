document.addEventListener("DOMContentLoaded", function () {

    //================================
    //Validasi file
    //================================

    const maxSize = 1 * 1024 * 1024; // 1MB
    const fileInputs = [
        { id: 'fileDrawing', type: 'pdf' },
        { id: 'fileQuotation', type: 'pdf' },
        { id: 'fileDesign', type: 'zip' }
    ];

    fileInputs.forEach(item => {
        const input = document.getElementById(item.id);
        if (!input) return;

        input.addEventListener('change', function () {
            const file = this.files[0];
            if (!file) return;

            const fileName = file.name.toLowerCase();
            const fileSize = file.size;
            const isPDF = file.type === "application/pdf" || fileName.endsWith(".pdf");
            const isZIP = file.type === "application/zip" || fileName.endsWith(".zip");

            if (item.type === 'pdf' && !isPDF) {
                alert("Hanya file berformat PDF yang diizinkan.");
                this.value = "";
                return;
            }

            if (item.type === 'zip' && !isZIP) {
                alert("Hanya file berformat ZIP yang diizinkan.");
                this.value = "";
                return;
            }

            if (fileSize > maxSize) {
                alert("Ukuran file maksimal 1MB. Silakan pilih file yang lebih kecil.");
                this.value = "";
                return;
            }
        });
    });

    //================================
    // Update visibility field berdasarkan kategori
    //================================

    const kategori = document.getElementById("MachineCategoryId");
    const jumlah = document.getElementById("qty");
    const price = document.getElementById("price");
    const sapid = document.getElementById("sapid");
    const quatation = document.getElementById("quatationcontainer");

    function updateVisibility() {
        if (kategori.value === "3") {
            price.classList.add("d-none");
            sapid.classList.add("d-none");
            jumlah.classList.add("d-none");
            quatation.classList.add("d-none");
        } else {
            price.classList.remove("d-none");
            sapid.classList.remove("d-none");
            jumlah.classList.remove("d-none");
            quatation.classList.remove("d-none");
        }
    }

    kategori.addEventListener("change", updateVisibility);
    updateVisibility();

    //================================
    // Validasi form sebelum submit
    // ===============================

    const form = document.getElementById("form");
    form.querySelector("button[type='submit']").addEventListener("click", function (e) {
        const sapid = document.getElementById("SAPID");
        const price = document.getElementById("ExternalFabCost");
        const quotation = document.getElementById("fileQuotation");

        // hapus error lama
        document.querySelectorAll(".text-danger").forEach(el => el.remove());

        let valid = true;

        if (kategori.value !== "3") {
            if (!sapid.value) {
                const error = document.createElement("div");
                error.className = "text-danger mt-1";
                error.innerText = "SAPID wajib diisi!";
                sapid.parentNode.appendChild(error);
                valid = false;
            }
            if (!price.value) {
                const error = document.createElement("div");
                error.className = "text-danger mt-1";
                error.innerText = "Price wajib diisi!";
                price.parentNode.appendChild(error);
                valid = false;
            }
            if (!quotation.files || quotation.files.length === 0) {
                const error = document.createElement("div");
                error.className = "text-danger mt-1";
                error.innerText = "Quotation wajib diisi!";
                quotation.parentNode.appendChild(error);
                valid = false;
            }
        }

        if (!valid) {
            e.preventDefault();
            return false;
        }
    });

});
