import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RewriteFacade } from './rewrite.facade';
import { GOALS, PLATFORMS, ROLES } from '../../core/constants/options';
import { RewriteMessageRequest } from '../../core/models/rewrite.models';

@Component({
  standalone: true,
  imports: [CommonModule, FormsModule],
  providers: [RewriteFacade],
  templateUrl: './rewrite-page.component.html'
})
export class RewritePageComponent {
  readonly roles = ROLES;
  readonly goals = GOALS;
  readonly platforms = PLATFORMS;

  readonly form: RewriteMessageRequest = {
    userId: 'user-001',
    contactId: 'contact-001',
    draft: 'hey can we meet sometime',
    relationshipRole: 'Investor',
    goal: 'ScheduleMeeting',
    platform: 'LinkedIn'
  };

  constructor(public readonly facade: RewriteFacade) {}

  submit(): void {
    this.facade.submit({ ...this.form });
  }

  reset(): void {
    this.form.userId = 'user-001';
    this.form.contactId = 'contact-001';
    this.form.draft = 'hey can we meet sometime';
    this.form.relationshipRole = 'Investor';
    this.form.goal = 'ScheduleMeeting';
    this.form.platform = 'LinkedIn';
    this.facade.clear();
  }

  async copy(text: string): Promise<void> {
    try {
      await navigator.clipboard.writeText(text);
    } catch {
      this.facade.error.set('Clipboard copy failed.');
    }
  }
}
