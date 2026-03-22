import { LitElement, html, css } from '@umbraco-cms/backoffice/external/lit';
import { UmbElementMixin } from '@umbraco-cms/backoffice/element-api';
import { UMB_AUTH_CONTEXT } from '@umbraco-cms/backoffice/auth';

export class EditorialDashboardElement extends UmbElementMixin(LitElement) {
  static properties = {
    _loading: { state: true },
    _data: { state: true },
    _error: { state: true },
  };

  #authContext;

  static styles = css`
    :host {
      display: block;
      padding: 20px;
    }
    .stats-grid {
      display: grid;
      grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
      gap: 16px;
      margin-bottom: 24px;
    }
    .stat-box {
      padding: 16px;
      border-radius: 8px;
      background: var(--uui-color-surface-alt, #f4f4f4);
      text-align: center;
    }
    .stat-box .number {
      font-size: 2rem;
      font-weight: 700;
      color: var(--uui-color-interactive, #1b264f);
    }
    .stat-box .label {
      font-size: 0.85rem;
      color: var(--uui-color-text-alt, #666);
      margin-top: 4px;
    }
    table {
      width: 100%;
      border-collapse: collapse;
    }
    th, td {
      text-align: left;
      padding: 10px 12px;
      border-bottom: 1px solid var(--uui-color-border, #e0e0e0);
    }
    th {
      font-weight: 600;
      font-size: 0.85rem;
      color: var(--uui-color-text-alt, #666);
    }
    a {
      color: var(--uui-color-interactive, #1b264f);
      text-decoration: none;
    }
    a:hover {
      text-decoration: underline;
    }
    .error {
      color: var(--uui-color-danger, #d32f2f);
      padding: 12px;
    }
    .section-title {
      font-size: 1.1rem;
      font-weight: 600;
      margin-bottom: 12px;
      margin-top: 8px;
    }
  `;

  constructor() {
    super();
    this._loading = true;
    this._data = null;
    this._error = null;

    this.consumeContext(UMB_AUTH_CONTEXT, (authContext) => {
      this.#authContext = authContext;
      if (this.isConnected) this._fetchData();
    });
  }

  connectedCallback() {
    super.connectedCallback();
    if (this.#authContext) this._fetchData();
  }

  async _fetchData() {
    this._loading = true;
    this._error = null;

    try {
      const config = this.#authContext?.getOpenApiConfiguration();
      const token = typeof config?.token === 'function' ? await config.token() : config?.token;

      const response = await fetch('/umbraco/management/api/v1/predelnews/editorial', {
        headers: {
          'Accept': 'application/json',
          ...(token ? { 'Authorization': `Bearer ${token}` } : {}),
        },
      });

      if (!response.ok) {
        throw new Error(`HTTP ${response.status}`);
      }

      this._data = await response.json();
    } catch (err) {
      this._error = err.message || 'Failed to load dashboard data';
    } finally {
      this._loading = false;
    }
  }

  render() {
    if (this._loading) {
      return html`<uui-loader-bar></uui-loader-bar>`;
    }

    if (this._error) {
      return html`<div class="error">Error: ${this._error}</div>`;
    }

    const d = this._data;

    return html`
      <uui-box headline="Editorial Dashboard">
        <div class="stats-grid">
          <div class="stat-box">
            <div class="number">${d.inReviewCount}</div>
            <div class="label">In Review</div>
          </div>
          <div class="stat-box">
            <div class="number">${d.publishedTodayCount}</div>
            <div class="label">Published Today</div>
          </div>
          <div class="stat-box">
            <div class="number">${d.publishedThisWeekCount}</div>
            <div class="label">Published This Week</div>
          </div>
          <div class="stat-box">
            <div class="number">${d.heldCommentsCount}</div>
            <div class="label">Held Comments</div>
          </div>
        </div>

        <div class="section-title">Articles In Review</div>
        ${d.inReviewArticles.length === 0
          ? html`<p style="color:var(--uui-color-text-alt,#666)">No articles awaiting review.</p>`
          : html`
            <table>
              <thead>
                <tr>
                  <th>Headline</th>
                  <th>Author</th>
                  <th>Last Modified</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                ${d.inReviewArticles.map(a => html`
                  <tr>
                    <td>${a.headline}</td>
                    <td>${a.authorName}</td>
                    <td>${new Date(a.modifiedAt).toLocaleString('bg-BG')}</td>
                    <td><a href="/umbraco/section/content/workspace/document/edit/${a.key}">Edit</a></td>
                  </tr>
                `)}
              </tbody>
            </table>
          `}

        <div class="stats-grid" style="margin-top:24px">
          <div class="stat-box">
            <div class="number">${d.emailSignupsCount}</div>
            <div class="label">Email Signups</div>
          </div>
        </div>
      </uui-box>
    `;
  }
}

customElements.define('predelnews-editorial-dashboard', EditorialDashboardElement);

export default EditorialDashboardElement;
