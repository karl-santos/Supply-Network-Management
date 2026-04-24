const messageBox = document.getElementById("messageBox");
const catalogueBody = document.getElementById("catalogueBody");
const searchInput = document.getElementById("searchInput");

const createSection = document.getElementById("createSection");
const manageSection = document.getElementById("manageSection");
const browseSection = document.getElementById("browseSection");

const navCreateBtn = document.getElementById("navCreateBtn");
const navManageBtn = document.getElementById("navManageBtn");
const navBrowseBtn = document.getElementById("navBrowseBtn");

const sectionLabel = document.getElementById("sectionLabel");
const backToBrowseBtn = document.getElementById("backToBrowseBtn");

const detailEmptyState = document.getElementById("detailEmptyState");
const detailCard = document.getElementById("detailCard");
const detailEditBtn = document.getElementById("detailEditBtn");
const detailDeleteBtn = document.getElementById("detailDeleteBtn");

let allProducts = [];
let selectedProductId = null;

document.addEventListener("DOMContentLoaded", () => {
    navCreateBtn?.addEventListener("click", () => showSection("create"));
    navManageBtn?.addEventListener("click", () => showSection("manage"));
    navBrowseBtn?.addEventListener("click", () => showSection("browse"));
    backToBrowseBtn?.addEventListener("click", () => showSection("browse"));

    document.getElementById("refreshBtn")?.addEventListener("click", handleRefresh);
    document.getElementById("addProductForm")?.addEventListener("submit", addProduct);
    document.getElementById("editProductForm")?.addEventListener("submit", updateProduct);
    searchInput?.addEventListener("input", filterCatalogue);

    detailEditBtn?.addEventListener("click", () => {
        if (!selectedProductId) return;
        fillEditForm(selectedProductId);
        showSection("manage");
    });

    detailDeleteBtn?.addEventListener("click", () => {
        if (!selectedProductId) return;
        deleteProduct(selectedProductId);
    });

    showSection("create");
    loadCatalogue();
});

function showSection(section) {
    createSection?.classList.add("hidden");
    manageSection?.classList.add("hidden");
    browseSection?.classList.add("hidden");

    navCreateBtn?.classList.remove("active");
    navManageBtn?.classList.remove("active");
    navBrowseBtn?.classList.remove("active");

    if (section === "create") {
        createSection?.classList.remove("hidden");
        navCreateBtn?.classList.add("active");
        if (sectionLabel) sectionLabel.textContent = "Create";
        backToBrowseBtn?.classList.add("hidden");
    } else if (section === "manage") {
        manageSection?.classList.remove("hidden");
        navManageBtn?.classList.add("active");
        if (sectionLabel) sectionLabel.textContent = "Manage";
        backToBrowseBtn?.classList.remove("hidden");
    } else {
        browseSection?.classList.remove("hidden");
        navBrowseBtn?.classList.add("active");
        if (sectionLabel) sectionLabel.textContent = "Browse";
        backToBrowseBtn?.classList.add("hidden");
    }
}

async function apiFetch(url, options = {}) {
    const response = await fetch(url, {
        credentials: "include",
        headers: {
            "Content-Type": "application/json",
            ...(options.headers || {})
        },
        ...options
    });

    if (response.status === 401) {
        window.location.href = "/login.html";
        return null;
    }

    return response;
}

async function loadCatalogue() {
    try {
        const response = await apiFetch("/api/catalogue", { method: "GET" });
        if (!response) return;

        if (!response.ok) {
            const err = await safeParseJson(response);
            throw new Error(err?.message || "Failed to load catalogue.");
        }

        const raw = await response.json();
        const data = typeof raw === "string" ? JSON.parse(raw) : raw;

        allProducts = Array.isArray(data.products) ? data.products : [];
        renderCatalogue(getDisplayedProducts());

        if (allProducts.length > 0) {
            selectedProductId = allProducts[0].productId;
            renderCatalogue(getDisplayedProducts());
            showDetail(allProducts[0]);
        } else {
            selectedProductId = null;
            clearDetailPane();
        }
    } catch (error) {
        showMessage(error.message, true);
    }
}

async function addProduct(event) {
    event.preventDefault();

    const payload = {
        productName: document.getElementById("addProductName").value.trim(),
        categoryL1: document.getElementById("addCategoryL1").value.trim(),
        categoryL2: document.getElementById("addCategoryL2").value.trim(),
        categoryL3: document.getElementById("addCategoryL3").value.trim(),
        unit: document.getElementById("addUnit").value,
        quantity: parseInt(document.getElementById("addQuantity").value, 10),
        price: parseFloat(document.getElementById("addPrice").value)
    };

    const validationError = validateProductPayload(payload);
    if (validationError) {
        showMessage(validationError, true);
        return;
    }

    try {
        const response = await apiFetch("/api/catalogue/add", {
            method: "POST",
            body: JSON.stringify(payload)
        });
        if (!response) return;

        const data = await safeParseJson(response);

        if (!response.ok) {
            throw new Error(data?.message || "Failed to add product.");
        }

        document.getElementById("addProductForm").reset();
        document.getElementById("addPrice").value = "0.00";
        showMessage(data?.message || "Product added successfully.", false);

        await loadCatalogue();
        showSection("browse");
    } catch (error) {
        showMessage(error.message, true);
    }
}

async function updateProduct(event) {
    event.preventDefault();

    const productId = document.getElementById("editProductId").value.trim();
    if (!productId) {
        showMessage("Please select a product first.", true);
        return;
    }

    const payload = {
        productName: document.getElementById("editProductName").value.trim(),
        categoryL1: document.getElementById("editCategoryL1").value.trim(),
        categoryL2: document.getElementById("editCategoryL2").value.trim(),
        categoryL3: document.getElementById("editCategoryL3").value.trim(),
        unit: document.getElementById("editUnit").value,
        quantity: parseInt(document.getElementById("editQuantity").value, 10),
        price: parseFloat(document.getElementById("editPrice").value)
    };

    const validationError = validateProductPayload(payload);
    if (validationError) {
        showMessage(validationError, true);
        return;
    }

    try {
        const response = await apiFetch(`/api/catalogue/update/${encodeURIComponent(productId)}`, {
            method: "PATCH",
            body: JSON.stringify(payload)
        });
        if (!response) return;

        const data = await safeParseJson(response);

        if (!response.ok) {
            throw new Error(data?.message || "Failed to update product.");
        }

        showMessage(data?.message || "Product updated successfully.", false);
        await loadCatalogue();

        selectedProductId = productId;
        const updated = allProducts.find(p => p.productId === productId);
        if (updated) showDetail(updated);

        showSection("browse");
    } catch (error) {
        showMessage(error.message, true);
    }
}

async function deleteProduct(productId) {
    const confirmed = window.confirm(`Are you sure you want to remove product ${productId}?`);
    if (!confirmed) return;

    try {
        const response = await apiFetch(`/api/catalogue/remove/${encodeURIComponent(productId)}`, {
            method: "DELETE"
        });
        if (!response) return;

        const data = await safeParseJson(response);

        if (!response.ok) {
            throw new Error(data?.message || "Failed to remove product.");
        }

        clearEditFormIfMatches(productId);
        selectedProductId = null;
        clearDetailPane();
        showMessage(data?.message || "Product removed successfully.", false);

        await loadCatalogue();
    } catch (error) {
        showMessage(error.message, true);
    }
}

function renderCatalogue(products) {
    if (!catalogueBody) return;

    if (!products.length) {
        catalogueBody.innerHTML = `<div class="detail-empty">No products found.</div>`;
        return;
    }

    catalogueBody.innerHTML = products.map((product, index) => `
        <div class="catalogue-item ${selectedProductId === product.productId ? "active" : ""}"
             onclick="selectProduct('${jsEscape(product.productId)}')">
            <h4>Catalogue #${products.length - index}</h4>
            <p><strong>Product Name:</strong> ${escapeHtml(product.productName)}</p>
            <p><strong>Quantity:</strong> ${escapeHtml(product.quantity)}</p>
            <p><strong>Price:</strong> $${Number(product.price).toFixed(2)}</p>
        </div>
    `).join("");
}

function selectProduct(productId) {
    selectedProductId = productId;
    const product = allProducts.find(p => p.productId === productId);
    renderCatalogue(getDisplayedProducts());
    if (product) showDetail(product);
}

function showDetail(product) {
    detailEmptyState?.classList.add("hidden");
    detailCard?.classList.remove("hidden");

    document.getElementById("detailTitle").textContent = product.productName || "Product Details";
    document.getElementById("detailUnit").textContent = product.unit || "-";
    document.getElementById("detailProductId").textContent = product.productId || "-";
    document.getElementById("detailProductName").textContent = product.productName || "-";
    document.getElementById("detailCategoryL1").textContent = product.categoryL1 || "-";
    document.getElementById("detailCategoryL2").textContent = product.categoryL2 || "-";
    document.getElementById("detailCategoryL3").textContent = product.categoryL3 || "-";
    document.getElementById("detailQuantity").textContent = product.quantity ?? "-";
    document.getElementById("detailPrice").textContent = `$${Number(product.price || 0).toFixed(2)}`;
}

function clearDetailPane() {
    detailEmptyState?.classList.remove("hidden");
    detailCard?.classList.add("hidden");
}

function fillEditForm(productId) {
    const product = allProducts.find(p => p.productId === productId);
    if (!product) return;

    document.getElementById("editProductId").value = product.productId || "";
    document.getElementById("editProductName").value = product.productName || "";
    document.getElementById("editCategoryL1").value = product.categoryL1 || "";
    document.getElementById("editCategoryL2").value = product.categoryL2 || "";
    document.getElementById("editCategoryL3").value = product.categoryL3 || "";
    document.getElementById("editUnit").value = product.unit || "";
    document.getElementById("editQuantity").value = product.quantity || "";
    document.getElementById("editPrice").value = Number(product.price || 0).toFixed(2);
}

function clearEditFormIfMatches(productId) {
    const currentId = document.getElementById("editProductId").value.trim();
    if (currentId === productId) {
        document.getElementById("editProductForm").reset();
        document.getElementById("editPrice").value = "0.00";
    }
}

function handleRefresh() {
    if (searchInput) searchInput.value = "";
    loadCatalogue();
}

function filterCatalogue() {
    const filtered = getDisplayedProducts();
    renderCatalogue(filtered);

    if (selectedProductId && !filtered.some(p => p.productId === selectedProductId)) {
        selectedProductId = null;
        clearDetailPane();
        return;
    }

    if (selectedProductId) {
        const selected = filtered.find(p => p.productId === selectedProductId);
        if (selected) showDetail(selected);
    }
}

function getDisplayedProducts() {
    const term = searchInput?.value.trim().toLowerCase() || "";
    if (!term) return allProducts;

    return allProducts.filter(product =>
        String(product.productName || "").toLowerCase().includes(term)
    );
}

function validateProductPayload(payload) {
    if (!payload.productName) return "Product name is required.";
    if (!payload.categoryL1) return "Category Level 1 is required.";
    if (!payload.categoryL2) return "Category Level 2 is required.";
    if (!payload.categoryL3) return "Category Level 3 is required.";
    if (!payload.unit) return "Unit is required.";
    if (payload.unit !== "kg" && payload.unit !== "l") return "Unit must be either kg or l.";
    if (!Number.isFinite(payload.quantity) || payload.quantity <= 0) return "Quantity must be greater than 0.";
    if (!Number.isFinite(payload.price) || payload.price < 0) return "Price cannot be negative.";
    return null;
}

function showMessage(message, isError) {
    if (!messageBox) return;
    messageBox.textContent = message;
    messageBox.classList.remove("hidden", "success", "error");
    messageBox.classList.add(isError ? "error" : "success");
}

async function safeParseJson(response) {
    try {
        return await response.json();
    } catch {
        return null;
    }
}

function escapeHtml(value) {
    return String(value ?? "")
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#039;");
}

function jsEscape(value) {
    return String(value ?? "")
        .replaceAll("\\", "\\\\")
        .replaceAll("'", "\\'");
}

window.selectProduct = selectProduct;