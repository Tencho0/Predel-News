import { LitElement, html, css } from '@umbraco-cms/backoffice/external/lit';
import { UmbElementMixin } from '@umbraco-cms/backoffice/element-api';
import { UMB_AUTH_CONTEXT } from '@umbraco-cms/backoffice/auth';

export class EngagementDashboardElement extends UmbElementMixin(LitElement) {
  static properties = {
    _loading: { state: true },
    _activeTab: { state: true },
    _polls: { state: true },
    _subscriberCount: { state: true },
    _recentSubscribers: { state: true },
    _error: { state: true },
    _showCreateForm: { state: true },
    _newQuestion: { state: true },
    _newOptions: { state: true },
    _newOpensAt: { state: true },
    _newClosesAt: { state: true },
    _selectedPoll: { state: true },
    _selectedPollResults: { state: true },
  };

  #authContext;
  #baseUrl = '/umbraco/management/api/v1/engagement';

  static styles = css`
    :host { display: block; padding: 20px; }
    .tabs { display: flex; gap: 8px; margin-bottom: 24px; }
    .tab { padding: 8px 16px; border: 1px solid var(--uui-color-border, #ccc); border-radius: 4px; cursor: pointer; background: none; font-size: 0.9rem; }
    .tab.active { background: var(--uui-color-interactive, #1b264f); color: #fff; border-color: var(--uui-color-interactive, #1b264f); }
    .stats-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(160px, 1fr)); gap: 16px; margin-bottom: 24px; }
    .stat-box { padding: 16px; border-radius: 8px; background: var(--uui-color-surface-alt, #f4f4f4); text-align: center; }
    .stat-box .number { font-size: 2rem; font-weight: 700; color: var(--uui-color-interactive, #1b264f); }
    .stat-box .label { font-size: 0.85rem; color: var(--uui-color-text-alt, #666); margin-top: 4px; }
    table { width: 100%; border-collapse: collapse; }
    th, td { text-align: left; padding: 10px 12px; border-bottom: 1px solid var(--uui-color-border, #e0e0e0); }
    th { font-weight: 600; font-size: 0.85rem; color: var(--uui-color-text-alt, #666); }
    .error { color: var(--uui-color-danger, #d32f2f); padding: 12px; }
    .btn { padding: 6px 14px; border-radius: 4px; border: 1px solid var(--uui-color-border, #ccc); cursor: pointer; font-size: 0.85rem; background: none; }
    .btn-primary { background: var(--uui-color-interactive, #1b264f); color: #fff; border-color: var(--uui-color-interactive, #1b264f); }
    .btn-danger { color: var(--uui-color-danger, #d32f2f); border-color: var(--uui-color-danger, #d32f2f); }
    .btn-success { color: #198754; border-color: #198754; }
    .form-group { margin-bottom: 12px; }
    .form-group label { display: block; font-size: 0.85rem; margin-bottom: 4px; font-weight: 600; }
    .form-group input { width: 100%; padding: 6px 10px; border: 1px solid var(--uui-color-border, #ccc); border-radius: 4px; font-size: 0.9rem; box-sizing: border-box; }
    .progress-bar-container { background: #e9ecef; border-radius: 4px; height: 20px; overflow: hidden; margin-bottom: 4px; }
    .progress-fill { background: var(--uui-color-interactive, #1b264f); height: 100%; transition: width 0.3s ease; }
    .badge { display: inline-block; padding: 2px 8px; border-radius: 10px; font-size: 0.75rem; font-weight: 600; }
    .badge-active { background: #d4edda; color: #155724; }
    .badge-inactive { background: #f8d7da; color: #721c24; }
    .section-title { font-size: 1.1rem; font-weight: 600; margin-bottom: 12px; margin-top: 8px; }
  `;

  constructor() {
    super();
    this._loading = true;
    this._activeTab = 'polls';
    this._polls = [];
    this._subscriberCount = 0;
    this._recentSubscribers = [];
    this._error = null;
    this._showCreateForm = false;
    this._newQuestion = '';
    this._newOptions = ['', ''];
    this._newOpensAt = '';
    this._newClosesAt = '';
    this._selectedPoll = null;
    this._selectedPollResults = null;

    this.consumeContext(UMB_AUTH_CONTEXT, (authContext) => {
      this.#authContext = authContext;
      if (this.isConnected) this._fetchData();
    });
  }

  connectedCallback() {
    super.connectedCallback();
    if (this.#authContext) this._fetchData();
  }

  async _getHeaders() {
    const config = this.#authContext?.getOpenApiConfiguration();
    const token = typeof config?.token === 'function' ? await config.token() : config?.token;
    return {
      'Accept': 'application/json',
      'Content-Type': 'application/json',
      ...(token ? { 'Authorization': `Bearer ${token}` } : {}),
    };
  }

  async _fetchData() {
    this._loading = true;
    this._error = null;
    try {
      const headers = await this._getHeaders();
      const [pollsRes, countRes, recentRes] = await Promise.all([
        fetch(`${this.#baseUrl}/polls`, { headers }),
        fetch(`${this.#baseUrl}/subscribers/count`, { headers }),
        fetch(`${this.#baseUrl}/subscribers/recent`, { headers }),
      ]);

      if (!pollsRes.ok || !countRes.ok || !recentRes.ok) throw new Error('Failed to load data');

      this._polls = await pollsRes.json();
      const countData = await countRes.json();
      this._subscriberCount = countData.count;
      this._recentSubscribers = await recentRes.json();
    } catch (err) {
      this._error = err.message || 'Failed to load dashboard data';
    } finally {
      this._loading = false;
    }
  }

  async _createPoll() {
    const options = this._newOptions.filter(o => o.trim());
    if (!this._newQuestion.trim() || options.length < 2) return;

    const body = { question: this._newQuestion, options };
    if (this._newOpensAt) body.opensAt = new Date(this._newOpensAt).toISOString();
    if (this._newClosesAt) body.closesAt = new Date(this._newClosesAt).toISOString();

    try {
      const headers = await this._getHeaders();
      const res = await fetch(`${this.#baseUrl}/polls`, {
        method: 'POST',
        headers,
        body: JSON.stringify(body),
      });
      if (!res.ok) { const d = await res.json(); alert(d.message || 'Error'); return; }
      this._showCreateForm = false;
      this._newQuestion = '';
      this._newOptions = ['', ''];
      this._newOpensAt = '';
      this._newClosesAt = '';
      await this._fetchData();
    } catch (err) { alert('Error creating poll'); }
  }

  async _viewPollResults(id) {
    try {
      const headers = await this._getHeaders();
      const res = await fetch(`${this.#baseUrl}/polls/${id}`, { headers });
      if (!res.ok) { alert('Error loading poll'); return; }
      const data = await res.json();
      this._selectedPoll = data.poll;
      this._selectedPollResults = data.results;
    } catch (err) { alert('Error loading poll results'); }
  }

  async _togglePoll(id, isActive) {
    const action = isActive ? 'deactivate' : 'activate';
    try {
      const headers = await this._getHeaders();
      await fetch(`${this.#baseUrl}/polls/${id}/${action}`, { method: 'POST', headers });
      await this._fetchData();
    } catch (err) { alert('Error toggling poll'); }
  }

  async _deletePoll(id) {
    if (!confirm('Сигурни ли сте, че искате да изтриете тази анкета?')) return;
    try {
      const headers = await this._getHeaders();
      const res = await fetch(`${this.#baseUrl}/polls/${id}`, { method: 'DELETE', headers });
      if (!res.ok) { const d = await res.json(); alert(d.message || 'Error'); return; }
      await this._fetchData();
    } catch (err) { alert('Error deleting poll'); }
  }

  _addOption() {
    if (this._newOptions.length >= 4) return;
    this._newOptions = [...this._newOptions, ''];
  }

  _updateOption(index, value) {
    const opts = [...this._newOptions];
    opts[index] = value;
    this._newOptions = opts;
  }

  render() {
    if (this._loading) return html`<uui-loader-bar></uui-loader-bar>`;
    if (this._error) return html`<div class="error">Error: ${this._error}</div>`;

    return html`
      <uui-box headline="\u0410\u043d\u0433\u0430\u0436\u0438\u0440\u0430\u043d\u043e\u0441\u0442">
        <div class="tabs">
          <button class="tab ${this._activeTab === 'polls' ? 'active' : ''}" @click=${() => this._activeTab = 'polls'}>\u0410\u043d\u043a\u0435\u0442\u0438</button>
          <button class="tab ${this._activeTab === 'subscribers' ? 'active' : ''}" @click=${() => this._activeTab = 'subscribers'}>\u0410\u0431\u043e\u043d\u0430\u0442\u0438</button>
        </div>
        ${this._activeTab === 'polls' ? this._renderPolls() : this._renderSubscribers()}
      </uui-box>
    `;
  }

  _formatDate(dateStr) {
    if (!dateStr) return '\u2014';
    return new Date(dateStr).toLocaleDateString('bg-BG', { day: '2-digit', month: '2-digit', year: 'numeric' });
  }

  _getPollStatus(p) {
    if (p.isActive) return { label: '\u0410\u043a\u0442\u0438\u0432\u043d\u0430', cls: 'badge-active' };
    if (p.closesAt && new Date(p.closesAt) < new Date()) return { label: '\u041f\u0440\u0438\u043a\u043b\u044e\u0447\u0438\u043b\u0430', cls: 'badge-inactive' };
    if (p.opensAt && new Date(p.opensAt) > new Date()) return { label: '\u041f\u043b\u0430\u043d\u0438\u0440\u0430\u043d\u0430', cls: 'badge-inactive' };
    return { label: '\u041d\u0435\u0430\u043a\u0442\u0438\u0432\u043d\u0430', cls: 'badge-inactive' };
  }

  _renderPolls() {
    if (this._selectedPoll) return this._renderPollResults();

    return html`
      <div style="display:flex;justify-content:space-between;align-items:center;margin-bottom:16px">
        <div class="section-title">\u0410\u043d\u043a\u0435\u0442\u0438 (${this._polls.length})</div>
        <button class="btn btn-primary" @click=${() => this._showCreateForm = !this._showCreateForm}>
          ${this._showCreateForm ? '\u041e\u0442\u043a\u0430\u0436\u0438' : '\u041d\u043e\u0432\u0430 \u0430\u043d\u043a\u0435\u0442\u0430'}
        </button>
      </div>

      ${this._showCreateForm ? this._renderCreateForm() : ''}

      ${this._polls.length === 0
        ? html`<p style="color:var(--uui-color-text-alt,#666)">\u041d\u044f\u043c\u0430 \u0441\u044a\u0437\u0434\u0430\u0434\u0435\u043d\u0438 \u0430\u043d\u043a\u0435\u0442\u0438.</p>`
        : html`
          <table>
            <thead><tr><th>\u0412\u044a\u043f\u0440\u043e\u0441</th><th>\u0421\u0442\u0430\u0442\u0443\u0441</th><th>\u041e\u0442/\u0414\u043e</th><th>\u0413\u043b\u0430\u0441\u043e\u0432\u0435</th><th></th></tr></thead>
            <tbody>
              ${this._polls.map(p => {
                const status = this._getPollStatus(p);
                return html`
                <tr>
                  <td><a href="#" @click=${(e) => { e.preventDefault(); this._viewPollResults(p.id); }} style="color:inherit;text-decoration:underline">${p.question}</a></td>
                  <td><span class="badge ${status.cls}">${status.label}</span></td>
                  <td style="font-size:0.85rem">${this._formatDate(p.opensAt)} \u2013 ${this._formatDate(p.closesAt)}</td>
                  <td>${p.totalVotes}</td>
                  <td>
                    <button class="btn btn-success" @click=${() => this._togglePoll(p.id, p.isActive)}>
                      ${p.isActive ? '\u0414\u0435\u0430\u043a\u0442\u0438\u0432\u0438\u0440\u0430\u0439' : '\u0410\u043a\u0442\u0438\u0432\u0438\u0440\u0430\u0439'}
                    </button>
                    <button class="btn btn-danger" @click=${() => this._deletePoll(p.id)}>\u0418\u0437\u0442\u0440\u0438\u0439</button>
                  </td>
                </tr>
              `;})}
            </tbody>
          </table>
        `}
    `;
  }

  _renderPollResults() {
    const poll = this._selectedPoll;
    const results = this._selectedPollResults || [];
    const totalVotes = results.reduce((sum, r) => sum + r.voteCount, 0);

    return html`
      <div style="margin-bottom:16px">
        <button class="btn" @click=${() => { this._selectedPoll = null; this._selectedPollResults = null; }}>&larr; \u041d\u0430\u0437\u0430\u0434</button>
      </div>
      <div class="section-title">${poll.question}</div>
      <p style="font-size:0.85rem;color:var(--uui-color-text-alt,#666)">
        \u041e\u0431\u0449\u043e \u0433\u043b\u0430\u0441\u043e\u0432\u0435: ${totalVotes}
        ${poll.opensAt ? html` | \u041e\u0442: ${this._formatDate(poll.opensAt)}` : ''}
        ${poll.closesAt ? html` | \u0414\u043e: ${this._formatDate(poll.closesAt)}` : ''}
      </p>
      <div style="margin-top:16px">
        ${results.map(r => html`
          <div style="margin-bottom:12px">
            <div style="display:flex;justify-content:space-between;margin-bottom:4px;font-size:0.9rem">
              <span>${r.optionText}</span>
              <span>${r.voteCount} (${r.percentage}%)</span>
            </div>
            <div class="progress-bar-container">
              <div class="progress-fill" style="width:${r.percentage}%"></div>
            </div>
          </div>
        `)}
      </div>
    `;
  }

  _renderCreateForm() {
    return html`
      <div style="border:1px solid var(--uui-color-border,#ccc);border-radius:8px;padding:16px;margin-bottom:16px">
        <div class="form-group">
          <label>\u0412\u044a\u043f\u0440\u043e\u0441</label>
          <input type="text" .value=${this._newQuestion} @input=${(e) => this._newQuestion = e.target.value} placeholder="\u0412\u044a\u0432\u0435\u0434\u0435\u0442\u0435 \u0432\u044a\u043f\u0440\u043e\u0441\u0430..." />
        </div>
        ${this._newOptions.map((opt, i) => html`
          <div class="form-group">
            <label>\u041e\u043f\u0446\u0438\u044f ${i + 1}</label>
            <input type="text" .value=${opt} @input=${(e) => this._updateOption(i, e.target.value)} placeholder="\u041e\u043f\u0446\u0438\u044f ${i + 1}..." />
          </div>
        `)}
        ${this._newOptions.length < 4 ? html`<button class="btn" @click=${() => this._addOption()}>+ \u0414\u043e\u0431\u0430\u0432\u0438 \u043e\u043f\u0446\u0438\u044f</button>` : ''}
        <div style="display:flex;gap:16px;margin-top:12px">
          <div class="form-group" style="flex:1">
            <label>\u041e\u0442\u0432\u0430\u0440\u044f\u043d\u0435 (\u043d\u0435\u0437\u0430\u0434\u044a\u043b\u0436\u0438\u0442\u0435\u043b\u043d\u043e)</label>
            <input type="datetime-local" .value=${this._newOpensAt} @input=${(e) => this._newOpensAt = e.target.value} />
          </div>
          <div class="form-group" style="flex:1">
            <label>\u0417\u0430\u0442\u0432\u0430\u0440\u044f\u043d\u0435 (\u043d\u0435\u0437\u0430\u0434\u044a\u043b\u0436\u0438\u0442\u0435\u043b\u043d\u043e)</label>
            <input type="datetime-local" .value=${this._newClosesAt} @input=${(e) => this._newClosesAt = e.target.value} />
          </div>
        </div>
        <div style="margin-top:12px">
          <button class="btn btn-primary" @click=${() => this._createPoll()}>\u0421\u044a\u0437\u0434\u0430\u0439</button>
        </div>
      </div>
    `;
  }

  _renderSubscribers() {
    return html`
      <div class="stats-grid">
        <div class="stat-box">
          <div class="number">${this._subscriberCount}</div>
          <div class="label">\u0410\u0431\u043e\u043d\u0430\u0442\u0438</div>
        </div>
      </div>

      <div style="margin-bottom:16px">
        <a href="${this.#baseUrl}/subscribers/export" class="btn btn-primary" target="_blank">\u0418\u0437\u0442\u0435\u0433\u043b\u0438 CSV</a>
      </div>

      <div class="section-title">\u041f\u043e\u0441\u043b\u0435\u0434\u043d\u0438 20 \u0430\u0431\u043e\u043d\u0430\u0442\u0430</div>
      ${this._recentSubscribers.length === 0
        ? html`<p style="color:var(--uui-color-text-alt,#666)">\u041d\u044f\u043c\u0430 \u0430\u0431\u043e\u043d\u0430\u0442\u0438.</p>`
        : html`
          <table>
            <thead><tr><th>\u0418\u043c\u0435\u0439\u043b</th><th>\u0414\u0430\u0442\u0430</th></tr></thead>
            <tbody>
              ${this._recentSubscribers.map(s => html`
                <tr><td>${s.email}</td><td>${s.signedUpAt}</td></tr>
              `)}
            </tbody>
          </table>
        `}
    `;
  }
}

customElements.define('predelnews-engagement-dashboard', EngagementDashboardElement);

export default EngagementDashboardElement;
