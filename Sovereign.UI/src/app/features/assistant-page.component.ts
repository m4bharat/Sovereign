import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { ROLES } from '../core/options';

type Decision = {
  action: string;
  reply: string;
  memoryKey: string;
  memoryValue: string;
  summary: string;
  confidence: number;
};

@Component({
  standalone: true,
  imports: [CommonModule, FormsModule],
  template: `
    <section class="hero">
      <h1>AI assistant panel</h1>
      <p>Use the conversation-aware decision endpoint to decide whether to reply, summarize, or save memory.</p>
    </section>

    <section class="grid two">
      <div class="card stack">
        <div class="field"><label>User ID</label><input [(ngModel)]="form.userId"></div>
        <div class="field"><label>Contact ID</label><input [(ngModel)]="form.contactId"></div>
        <div class="field"><label>Relationship role</label><select [(ngModel)]="form.relationshipRole"><option *ngFor="let x of roles" [value]="x">{{x}}</option></select></div>
        <div class="field"><label>Message</label><textarea [(ngModel)]="form.message"></textarea></div>
        <div class="row">
          <button class="btn" (click)="run()" [disabled]="loading()">Run AI decision</button>
          <button class="btn secondary" (click)="reset()">Reset</button>
        </div>
        <div class="muted" *ngIf="error()">{{ error() }}</div>
      </div>

      <div class="card stack">
        <h3>Decision output</h3>
        <div class="empty" *ngIf="!decision() && !loading()">No AI decision yet.</div>
        <div class="muted" *ngIf="loading()">Evaluating message context...</div>
        <ng-container *ngIf="decision() as d">
          <div class="grid two">
            <div><div class="muted">Action</div><div>{{ d.action || '—' }}</div></div>
            <div><div class="muted">Confidence</div><div>{{ d.confidence || 0 }}</div></div>
          </div>
          <div class="card" *ngIf="d.reply"><strong>Reply</strong><div class="result">{{ d.reply }}</div></div>
          <div class="card" *ngIf="d.summary"><strong>Summary</strong><div class="result">{{ d.summary }}</div></div>
          <div class="grid two" *ngIf="d.memoryKey || d.memoryValue">
            <div class="card"><strong>Memory key</strong><div>{{ d.memoryKey || '—' }}</div></div>
            <div class="card"><strong>Memory value</strong><div>{{ d.memoryValue || '—' }}</div></div>
          </div>
        </ng-container>
      </div>
    </section>
  `
})
export class AssistantPageComponent {
  private http = inject(HttpClient);
  roles = ROLES;
  loading = signal(false);
  error = signal('');
  decision = signal<Decision | null>(null);

  form = {
    userId: 'user-001',
    contactId: 'contact-001',
    relationshipRole: 'Friend',
    message: 'Remember that my birthday is Jan 10'
  };

  run() {
    this.loading.set(true);
    this.error.set('');
    this.decision.set(null);
    this.http.post<Decision>('/api/ai/conversations/decide', this.form).subscribe({
      next: r => { this.decision.set(r); this.loading.set(false); },
      error: e => { this.error.set(e?.error?.detail || 'Failed to run AI decision.'); this.loading.set(false); }
    });
  }

  reset() {
    this.form = {
      userId: 'user-001',
      contactId: 'contact-001',
      relationshipRole: 'Friend',
      message: 'Remember that my birthday is Jan 10'
    };
    this.decision.set(null);
    this.error.set('');
  }
}
