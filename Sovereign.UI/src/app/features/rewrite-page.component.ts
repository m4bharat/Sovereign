import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { GOALS, PLATFORMS, ROLES } from '../core/options';

type Variant = { stance: string; message: string };
type RewriteResponse = { variants: Variant[] };

@Component({
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <section class="hero">
      <h1>Rewrite important messages</h1>
      <p>Generate three calibrated variants through <code>POST /api/ai/rewrite</code>.</p>
    </section>

    <section class="grid two">
      <div class="card stack">
        <div class="field"><label>User ID</label><input [(ngModel)]="form.userId"></div>
        <div class="field"><label>Contact ID</label><input [(ngModel)]="form.contactId"></div>
        <div class="row">
          <div class="field"><label>Relationship role</label><select [(ngModel)]="form.relationshipRole"><option *ngFor="let x of roles" [value]="x">{{x}}</option></select></div>
          <div class="field"><label>Goal</label><select [(ngModel)]="form.goal"><option *ngFor="let x of goals" [value]="x">{{x}}</option></select></div>
          <div class="field"><label>Platform</label><select [(ngModel)]="form.platform"><option *ngFor="let x of platforms" [value]="x">{{x}}</option></select></div>
        </div>
        <div class="field"><label>Draft</label><textarea [(ngModel)]="form.draft"></textarea></div>
        <div class="row">
          <button class="btn" (click)="submit()" [disabled]="loading()">Generate 3 variants</button>
          <button class="btn secondary" (click)="reset()">Reset</button>
        </div>
        <div class="muted" *ngIf="error()">{{ error() }}</div>
      </div>

      <div class="card stack">
        <h3>Variants</h3>
        <div class="empty" *ngIf="!loading() && !variants().length">No rewrites yet.</div>
        <div class="muted" *ngIf="loading()">Generating rewrites...</div>
        <div class="stack" *ngIf="variants().length">
          <div class="card" *ngFor="let v of variants()">
            <div class="row" style="justify-content:space-between">
              <strong>{{ v.stance }}</strong>
              <button class="btn ghost" (click)="copy(v.message)">Copy</button>
            </div>
            <div class="result">{{ v.message }}</div>
          </div>
        </div>
      </div>
    </section>
  `
})
export class RewritePageComponent {
  private http = inject(HttpClient);
  roles = ROLES;
  goals = GOALS;
  platforms = PLATFORMS;

  loading = signal(false);
  error = signal('');
  variants = signal<Variant[]>([]);

  form = {
    userId: 'user-001',
    contactId: 'contact-001',
    draft: 'hey can we meet sometime',
    relationshipRole: 'Investor',
    goal: 'ScheduleMeeting',
    platform: 'LinkedIn'
  };

  submit() {
    this.loading.set(true);
    this.error.set('');
    this.http.post<RewriteResponse>('/api/ai/rewrite', this.form).subscribe({
      next: r => { this.variants.set(r.variants ?? []); this.loading.set(false); },
      error: e => { this.error.set(e?.error?.detail || 'Failed to generate rewrites.'); this.loading.set(false); }
    });
  }

  reset() {
    this.form = {
      userId: 'user-001',
      contactId: 'contact-001',
      draft: 'hey can we meet sometime',
      relationshipRole: 'Investor',
      goal: 'ScheduleMeeting',
      platform: 'LinkedIn'
    };
    this.variants.set([]);
    this.error.set('');
  }

  async copy(text: string) {
    try { await navigator.clipboard.writeText(text); }
    catch { this.error.set('Clipboard copy failed.'); }
  }
}
