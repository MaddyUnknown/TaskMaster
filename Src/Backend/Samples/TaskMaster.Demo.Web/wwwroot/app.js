"use strict";

const MAX_ROWS_DISPLAYED = 500;
const ACTIVE_POLL_MS = 2000;
const TERMINAL = ["completed", "failed", "expired"];

// In-memory only, on purpose: a report is a one-shot artefact. Reloading the page
// starts clean rather than resurrecting yesterday's downloads.
const reports = [];

let pollTimer = null;

// ---- helpers ---------------------------------------------------------------

async function getJson(url) {
  const response = await fetch(url, { credentials: "same-origin" });
  const body = await response.json().catch(() => ({}));
  return { ok: response.ok, status: response.status, body };
}

async function postJson(url, payload) {
  const response = await fetch(url, {
    method: "POST",
    credentials: "same-origin",
    headers: {
      "Content-Type": "application/json"
    },
    body: JSON.stringify(payload)
  });
  const body = await response.json().catch(() => ({}));
  return { ok: response.ok, status: response.status, body };
}

function fmtTime(value) {
  if (!value) return "\u2014";
  const date = new Date(value);
  return Number.isNaN(date.getTime()) ? "\u2014" : date.toLocaleString();
}

function esc(text) {
  const div = document.createElement("div");
  div.textContent = String(text);
  return div.innerHTML;
}

// ---- submit form -----------------------------------------------------------

const recordsInput = document.getElementById("records");
recordsInput.addEventListener("input", () => {
  document.getElementById("records-out").textContent = recordsInput.value;
});

document.getElementById("report-form").addEventListener("submit", async (event) => {
  event.preventDefault();
  const errorEl = document.getElementById("form-error");
  errorEl.classList.add("hidden");

  const button = document.getElementById("submit-btn");
  button.disabled = true;
  try {
    const result = await postJson("/api/demo/reports", {
      title: document.getElementById("title").value,
      recordCount: Number(recordsInput.value)
    });

    if (!result.ok) {
      const detail =
        result.body.error ||
        Object.values(result.body.errors || {}).flat().join(" ") ||
        `HTTP ${result.status}`;
      throw new Error(detail);
    }

    reports.push({
      reportId: result.body.reportId,
      title: document.getElementById("title").value.trim(),
      recordCount: Number(recordsInput.value),
      status: "queued"
    });

    renderReports();
    startPolling();
  } catch (err) {
    errorEl.textContent = err.message;
    errorEl.classList.remove("hidden");
  } finally {
    button.disabled = false;
  }
});

// ---- report list -----------------------------------------------------------

function renderReports() {
  const list = document.getElementById("report-list");
  const emptyState = document.getElementById("empty-state");
  list.textContent = "";
  emptyState.classList.toggle("hidden", reports.length > 0);

  for (const report of [...reports].reverse()) {
    const li = document.createElement("li");
    li.className = "submission";
    li.dataset.reportId = report.reportId;
    list.appendChild(li);
  }
  reports.forEach(updateCard);
}

function updateCard(report) {
  const li = document.querySelector(`[data-report-id="${report.reportId}"]`);
  if (!li) return;

  const stage = report.status || "queued";
  const isCompleted = stage === "completed";
  const isFailed = stage === "failed" || stage === "expired";
  const isActive = !isCompleted && !isFailed;

  const badgeClass = {
    queued: "status-queued",
    running: "status-running",
    completed: "status-completed",
    failed: "status-failed",
    expired: "status-failed"
  }[stage] || "status-queued";

  const progressClass = isFailed ? "failed" : isCompleted ? "completed" : "";
  const percent = isCompleted || isFailed ? 100 : stage === "running" ? 50 : 10;

  const detail = report.failureReason
    ? `<div class="error">${esc(report.failureReason)}</div>`
    : "";

  li.innerHTML = `
    <div class="row1">
      <span class="title">${esc(report.title)}</span>
      <span class="status-badge ${badgeClass}">${esc(stage)}</span>
    </div>
    <div class="progress ${progressClass}"><div style="width:${percent}%"></div></div>
    <div class="meta">
      ${Number(report.recordCount).toLocaleString()} records &middot;
      ${report.rowCount ? `${Number(report.rowCount).toLocaleString()} rows &middot; ` : ""}
      Updated ${fmtTime(report.modifiedAt)}
    </div>
    ${detail}
    ${isCompleted ? `<button class="btn small" data-result="${report.reportId}">View result</button>` : ""}
  `;

  const viewButton = li.querySelector("[data-result]");
  if (viewButton) {
    viewButton.addEventListener("click", () => showResult(report));
  }
}

async function pollOnce() {
  let anyActive = false;

  for (const report of reports) {
    if (TERMINAL.includes(report.status)) continue;

    const { ok, status, body } = await getJson(`/api/demo/reports/${report.reportId}`);

    if (ok) {
      report.status = body.status;
      report.modifiedAt = body.modifiedAt;
      report.rowCount = body.rowCount;
      report.failureReason = body.failureReason;
    } else if (status === 404) {
      // Swept by the retention service, or never existed.
      report.status = "expired";
    }

    updateCard(report);
    if (!TERMINAL.includes(report.status)) anyActive = true;
  }

  if (!anyActive && pollTimer) {
    clearInterval(pollTimer);
    pollTimer = null;
  }
}

function startPolling() {
  if (pollTimer) return;
  pollTimer = setInterval(pollOnce, ACTIVE_POLL_MS);
  pollOnce();
}

// ---- result viewer ---------------------------------------------------------

async function showResult(report) {
  const { ok, body } = await getJson(`/api/demo/reports/${report.reportId}/result`);
  if (!ok) {
    alert(body.error || "Result not available.");
    return;
  }

  document.getElementById("result-card").classList.remove("hidden");
  document.getElementById("result-title").textContent =
    `${body.fileName} (${Number(body.rowCount).toLocaleString()} rows)`;
  document.getElementById("result-meta").textContent =
    `Generated at ${fmtTime(body.generatedAt)} \u00b7 ${Number(body.sizeBytes).toLocaleString()} bytes`;
  document.getElementById("result-truncated").classList.toggle("hidden", !body.truncated);

  const table = document.getElementById("result-table");
  table.textContent = "";

  const lines = body.content.split(/\r?\n/);
  let shownHeader = false;
  let rowCount = 0;

  for (const line of lines) {
    if (!line) continue;
    if (line.startsWith("#")) {
      const tr = document.createElement("tr");
      const td = document.createElement("td");
      td.colSpan = 10;
      td.style.color = "var(--muted)";
      td.textContent = line;
      tr.appendChild(td);
      table.appendChild(tr);
      continue;
    }

    const cells = line.split(",");
    if (!shownHeader) {
      const theadRow = document.createElement("tr");
      cells.forEach((cell) => {
        const th = document.createElement("th");
        th.textContent = cell;
        theadRow.appendChild(th);
      });
      table.appendChild(theadRow);
      shownHeader = true;
      continue;
    }

    if (++rowCount > MAX_ROWS_DISPLAYED) break;
    const row = document.createElement("tr");
    cells.forEach((cell) => {
      const td = document.createElement("td");
      td.textContent = cell;
      row.appendChild(td);
    });
    table.appendChild(row);
  }

  if (rowCount > MAX_ROWS_DISPLAYED) {
    const tr = document.createElement("tr");
    const td = document.createElement("td");
    td.colSpan = 10;
    td.style.color = "var(--muted)";
    td.textContent = `\u2026 showing first ${MAX_ROWS_DISPLAYED} of ${Number(body.rowCount).toLocaleString()} rows. Use Download CSV for the full file.`;
    tr.appendChild(td);
    table.appendChild(tr);
  }

  // Streamed by the server from the file store, so this is the real file.
  const downloadButton = document.getElementById("download-btn");
  downloadButton.onclick = () => {
    window.location.href = `/api/demo/reports/${report.reportId}/download`;
  };

  document.getElementById("result-card").scrollIntoView({ behavior: "smooth" });
}

document.getElementById("close-result-btn").addEventListener("click", () => {
  document.getElementById("result-card").classList.add("hidden");
});

// ---- boot -------------------------------------------------------------------

renderReports();
