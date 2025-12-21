
//------------------------------------------------
// loading efek 
//---------------------------------------
document.addEventListener("DOMContentLoaded", function () {

    const overlay = document.getElementById("globalLoadingOverlay");
    let spinnerTimer = null;
    let spinnerVisible = false;

    const SPINNER_DELAY = 300;
    const MIN_VISIBLE_TIME = 400;

    function showSpinner() {
        if (!overlay || spinnerVisible) return;
        overlay.style.display = "flex";
        spinnerVisible = true;
    }

    function hideSpinner() {
        if (!overlay || !spinnerVisible) return;

        const elapsed = Date.now() - overlay._shownAt;
        const remaining = Math.max(0, MIN_VISIBLE_TIME - elapsed);

        setTimeout(() => {
            overlay.style.display = "none";
            spinnerVisible = false;
        }, remaining);
    }

    function startLoading() {
        spinnerTimer = setTimeout(() => {
            overlay._shownAt = Date.now();
            showSpinner();
        }, SPINNER_DELAY);
    }

    function stopLoading() {
        clearTimeout(spinnerTimer);
        hideSpinner();
    }

    // 🔹 GLOBAL SUBMIT HANDLER (MODAL + PAGE)
    document.addEventListener("submit", function (e) {
        const form = e.target;
        if (!(form instanceof HTMLFormElement)) return;

        // Disable submit button
        const buttons = form.querySelectorAll('[type="submit"]');
        buttons.forEach(btn => btn.disabled = true);

        // Start smart loading
        startLoading();
    });


});


//-----------------------------------------------
// global modal
// -----------------------------------

function initFinishModalValidation() {
    const modal = document.getElementById("globalModalContent");
    const fileInput = modal.querySelector("#fileCOC");
    const form = modal.querySelector("form");

    if (!fileInput || !form) return;

    fileInput.addEventListener("change", function (e) {

        // Pastikan event berasal dari input file #fileCOC
        if (e.target.id !== "fileCOC") return;

        const fileInput = e.target;
        const file = fileInput.files[0];

        if (!file) return;

        // Validasi ekstensi
        const ext = file.name.split('.').pop().toLowerCase();
        if (ext !== "pdf") {
            alert("File harus .pdf");
            fileInput.value = "";
            return;
        }

        // Validasi ukuran (5MB)
        if (file.size > 1 * 1024 * 1024) {
            alert("Ukuran file maksimal 1MB.");
            fileInput.value = "";
            return;
        }

        // Jika valid
        console.log("File valid:", file.name);

    });

    // Reset ketika modal ditampilkan

    modal.addEventListener("shown.bs.modal", () => {
        fileInput.value = "";
        fileInput.classList.remove("is-invalid", "is-valid");
    });
}


// tampilkan modal
document.addEventListener("click", function (e) {
    if (!e.target.classList.contains("open-modal")) return;

    const url = e.target.dataset.url;
    const modal = new bootstrap.Modal(document.getElementById("globalModal"));

    document.getElementById("globalModalContent").innerHTML =
        "<div class='p-5 text-center'>Loading...</div>";

    modal.show();

    fetch(url)
        .then(res => res.text())
        .then(html => {
            document.getElementById("globalModalContent").innerHTML = html;
            initFinishModalValidation();
        })
        .catch(() => {
            document.getElementById("globalModalContent").innerHTML =
                "<div class='p-5 text-danger'>Failed to load content</div>";
        });
});
