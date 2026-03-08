import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { ROLES } from '../core/options';

type CreateRelationshipResponse = { relationshipId: string; userId: string; contactId: string; role: string };
type TemperatureResponse = { relationshipId: string; score: number; silenceDays: number; temperature: string; recommendedAction: string };
type Vm = CreateRelationshipResponse & Partial<TemperatureResponse>;

@Component({
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <section class="hero">
      <h1>Relationships dashboard</h1>
      <p>Create relationship nodes and fetch live temperature from the local API.</p>
    </section>

    <section class="grid two">
      <div class="card stack">
        <div class="field"><label>User ID</label><input [(ngModel)]="form.userId"></div>
        <div class="field"><label>Contact ID</label><input [(ngModel)]="form.contactId"></div>
        <div class="field"><label>Role</label><select [(ngModel)]="form.role"><option *ngFor="let x of roles" [value]="x">{{x}}</option></select></div>
        <div class="row">
          <button class="btn" (click)="create()" [disabled]="busy()">Create</button>
          <button class="btn secondary" (click)="refreshAll()">Refresh temperatures</button>
        </div>
        <div class="muted" *ngIf="message()">{{ message() }}</div>
        <div class="muted" *ngIf="error()">{{ error() }}</div>
      </div>

      <div class="card stack">
        <h3>Quick stats</h3>
        <div class="grid three">
          <div class="card"><div class="muted">Hot</div><div class="kpi">{{ hotCount() }}</div></div>
          <div class="card"><div class="muted">Warm</div><div class="kpi">{{ warmCount() }}</div></div>
          <div class="card"><div class="muted">Cold</div><div class="kpi">{{ coldCount() }}</div></div>
        </div>
      </div>
    </section>

    <section class="card stack" style="margin-top:20px">
      <h3>Tracked relationships</h3>
      <div class="empty" *ngIf="!items().length">No relationships yet.</div>
      <div class="grid two" *ngIf="items().length">
        <div class="card stack" *ngFor="let item of items()">
          <div class="row" style="justify-content:space-between">
            <div><strong>{{ item.contactId }}</strong><div class="muted">Role: {{ item.role }}</div></div>
            <span class="badge" [class.hot]="item.temperature==='Hot'" [class.warm]="item.temperature==='Warm'" [class.cold]="item.temperature==='Cold'">
              {{ item.temperature || 'Unknown' }}
            </span>
          </div>
          <div class="grid two">
            <div><div class="muted">Relationship ID</div><div>{{ item.relationshipId }}</div></div>
            <div><div class="muted">Recommended action</div><div>{{ item.recommendedAction || '—' }}</div></div>
            <div><div class="muted">Score</div><div>{{ item.score ?? '—' }}</div></div>
            <div><div class="muted">Silence days</div><div>{{ item.silenceDays ?? '—' }}</div></div>
          </div>
          <div class="row">
            <button class="btn ghost" (click)="refresh(item.relationshipId)">Refresh temperature</button>
          </div>
        </div>
      </div>
    </section>
  `
})
export class RelationshipsPageComponent {
  private http = inject(HttpClient);
  roles = ROLES;
  busy = signal(false);
  error = signal('');
  message = signal('');
  items = signal<Vm[]>([]);

  hotCount = computed(() => this.items().filter(x => x.temperature === 'Hot').length);
  warmCount = computed(() => this.items().filter(x => x.temperature === 'Warm').length);
  coldCount = computed(() => this.items().filter(x => x.temperature === 'Cold').length);

  form = { userId: 'user-001', contactId: 'contact-001', role: 'Investor' };

  create() {
        this.busy.set(true);
    this.error.set('');
    this.message.set('');
    this.http.post<CreateRelationshipResponse>('/api/relationships', this.form).subscribe({
      next: r => {
        if (!this.items().some(x => x.relationshipId === r.relationshipId)) {
          this.items.set([r, ...this.items()]);
        }
        this.message.set('Relationship created successfully.');
        this.busy.set(false);
        this.refresh(r.relationshipId);
      },
      error: e => {
        this.error.set(e?.error?.detail || 'Failed to create relationship.');
        this.busy.set(false);
      }
    });
  }

  refresh(id: string) {
    this.http.get<TemperatureResponse>(`/api/relationships/${id}/temperature`).subscribe({
      next: t => {
        this.items.set(this.items().map(x => x.relationshipId === id ? { ...x, ...t } : x));
      },
      error: e => { this.error.set(e?.error?.detail || 'Failed to fetch temperature.'); }
    });
  }

  refreshAll() { this.items().forEach(x => this.refresh(x.relationshipId)); }
}
