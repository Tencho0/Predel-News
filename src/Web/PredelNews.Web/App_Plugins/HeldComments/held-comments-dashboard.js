import { UmbElementMixin } from "@umbraco-cms/backoffice/element-api";
import { LitElement, html, css } from "@umbraco-cms/backoffice/external/lit";

export default class HeldCommentsDashboard extends UmbElementMixin(LitElement) {
  static properties = {
    _comments: { state: true },
    _total: { state: true },
    _page: { state: true },
    _loading: { state: true },
  };

  static styles = css`
    :host {
      display: block;
      padding: 20px;
    }
    table {
      width: 100%;
      border-collapse: collapse;
    }
    th, td {
      padding: 8px 12px;
      text-align: left;
      border-bottom: 1px solid var(--uui-color-border);
    }
    th {
      font-weight: 600;
      background: var(--uui-color-surface-alt);
    }
    .actions {
      display: flex;
      gap: 8px;
    }
    .reason {
      font-size: 0.85em;
      color: var(--uui-color-text-alt);
    }
    .comment-text {
      max-width: 400px;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }
    .pagination {
      display: flex;
      gap: 8px;
      margin-top: 16px;
      align-items: center;
    }
    .empty-state {
      padding: 40px;
      text-align: center;
      color: var(--uui-color-text-alt);
    }
  `;

  constructor() {
    super();
    this._comments = [];
    this._total = 0;
    this._page = 1;
    this._loading = true;
  }

  connectedCallback() {
    super.connectedCallback();
    this._loadComments();
  }

  async _loadComments() {
    this._loading = true;
    try {
      const response = await fetch(
        `/umbraco/management/api/v1/held-comments?page=${this._page}&pageSize=20`,
        {
          headers: { "Content-Type": "application/json" },
          credentials: "include",
        }
      );
      if (response.ok) {
        const data = await response.json();
        this._comments = data.items || [];
        this._total = data.total || 0;
      }
    } catch (e) {
      console.error("Failed to load held comments", e);
    }
    this._loading = false;
  }

  async _approve(id) {
    try {
      await fetch(`/umbraco/management/api/v1/held-comments/${id}/approve`, {
        method: "POST",
        credentials: "include",
      });
      this._loadComments();
    } catch (e) {
      console.error("Failed to approve comment", e);
    }
  }

  async _delete(id) {
    if (!confirm("Сигурни ли сте, че искате да изтриете този коментар?")) return;
    try {
      await fetch(`/umbraco/management/api/v1/held-comments/${id}`, {
        method: "DELETE",
        credentials: "include",
      });
      this._loadComments();
    } catch (e) {
      console.error("Failed to delete comment", e);
    }
  }

  _prevPage() {
    if (this._page > 1) {
      this._page--;
      this._loadComments();
    }
  }

  _nextPage() {
    if (this._page * 20 < this._total) {
      this._page++;
      this._loadComments();
    }
  }

  render() {
    if (this._loading) {
      return html`<uui-loader></uui-loader>`;
    }

    if (this._comments.length === 0) {
      return html`<div class="empty-state">
        <uui-icon name="icon-check"></uui-icon>
        <p>Няма задържани коментари.</p>
      </div>`;
    }

    return html`
      <h2>Задържани коментари (${this._total})</h2>
      <table>
        <thead>
          <tr>
            <th>Име</th>
            <th>Коментар</th>
            <th>Причина</th>
            <th>Дата</th>
            <th>Действия</th>
          </tr>
        </thead>
        <tbody>
          ${this._comments.map(
            (c) => html`
              <tr>
                <td>${c.displayName}</td>
                <td class="comment-text" title="${c.commentText}">${c.commentText}</td>
                <td class="reason">${c.heldReason || "-"}</td>
                <td>${c.createdAt}</td>
                <td class="actions">
                  <uui-button
                    look="primary"
                    label="Одобри"
                    @click=${() => this._approve(c.id)}
                  ></uui-button>
                  <uui-button
                    look="secondary"
                    color="danger"
                    label="Изтрий"
                    @click=${() => this._delete(c.id)}
                  ></uui-button>
                </td>
              </tr>
            `
          )}
        </tbody>
      </table>
      <div class="pagination">
        <uui-button
          ?disabled=${this._page <= 1}
          @click=${this._prevPage}
          label="Предишна"
        ></uui-button>
        <span>Страница ${this._page} от ${Math.ceil(this._total / 20) || 1}</span>
        <uui-button
          ?disabled=${this._page * 20 >= this._total}
          @click=${this._nextPage}
          label="Следваща"
        ></uui-button>
      </div>
    `;
  }
}

customElements.define("held-comments-dashboard", HeldCommentsDashboard);
