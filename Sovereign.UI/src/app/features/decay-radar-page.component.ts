import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';

type Alert = {
  relationshipId: string;
  contactId: string;
  daysSilent: number;
  decayScore: number;
  temperature: string;
  suggestedAction: string;
  suggestedMessage: string;
};

type AlertsResponse = { alerts: Alert[] };

@Component({
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <section class="hero">
      <h1>Decay radar</h1>
      <p>Fetch follow-up opportunities through <code>GET /api/relationships/decay-alerts</code>.</p>
    </section>

    <section class="grid two">
      <div class="card stack">
        <div class="field"><label>User ID</label><input [(ngModel)]="userId"></div>
        <div class="row">
          <button class="btn" (click)="load()" [disabled]="loading()">Load decay alerts</button>
          <button class="btn secondary" (click)="clear()">Clear</button>
        </div>
        <div class="muted" *ngIf="error()">{{ error() }}</div>
      </div>

      <div class="card stack">
        <h3>Summary</h3>
        <div class="grid three">
          <div class="card"><div class="muted">Alerts</div><div class="kpi">{{ alerts().length }}</div></div>
          <div class="card"><div class="muted">Reconnect</div><div class="kpi">{{ reconnectCount() }}</div></div>
          <div class="card"><div class="muted">Maintain</div><div class="kpi">{{ maintainCount() }}</div></div>
        </div>
      </div>
    </section>

    <section class="card stack" style="margin-top:20px">
      <h3>Follow-up opportunities</h3>
      <div class="empty" *ngIf="!alerts().length && !loading()">No alerts loaded yet.</div>
      <div class="muted" *ngIf="loading()">Loading...</div>
      <div class="stack" *ngIf="alerts().length">
        <div class="card stack" *ngFor="let a of alerts()">
          <div class="row" style="justify-content:space-between">
            <div><strong>{{ a.contactId }}</strong><div class="muted">Relationship ID: {{ a.relationshipId }}</div></div>
            <span class="badge" [class.hot]="a.temperature==='Hot'" [class.warm]="a.temperature==='Warm'" [class.cold]="a.temperature==='Cold'">{{ a.temperature }}</span>
          </div>
          <div class="grid two">
            <div><div class="muted">Days silent</div><div>{{ a.daysSilent }}</div></div>
            <div><div class="muted">Decay score</div><div>{{ a.decayScore }}</div></div>
            <div><div class="muted">Suggested action</div><div>{{ a.suggestedAction }}</div></div>
          </div>
          <div class="card result">{{ a.suggestedMessage }}</div>
          <div class="row"><button class="btn ghost" (click)="copy(a.suggestedMessage)">Copy message</button></div>
        </div>
      </div>
    </section>
  `
})
export class DecayRadarPageComponent {
  private http = inject(HttpClient);
  userId = 'user-001';
  loading = signal(false);
  error = signal('');
  alerts = signal<Alert[]>([]);

  reconnectCount = computed(() => this.alerts().filter(x => x.suggestedAction === 'Reconnect').length);
  maintainCount = computed(() => this.alerts().filter(x => x.suggestedAction !== 'Reconnect').length);

  load() {
    this.loading.set(true);
    this.error.set('');
    this.http.get<AlertsResponse>(`/api/relationships/decay-alerts?userId=${encodeURIComponent(this.userId)}`).subscribe({
      next: r => { this.alerts.set(r.alerts ?? []); this.loading.set(false); },
      error: e => { this.error.set(e?.error?.detail || 'Failed to fetch decay alerts.'); this.loading.set(false); }
    });
  }

  clear() { this.alerts.set([]); this.error.set(''); }

  async copy(text: string) {
    try { await navigator.clipboard.writeText(text); }
    catch { this.error.set('Clipboard copy failed.'); }
  }
}
