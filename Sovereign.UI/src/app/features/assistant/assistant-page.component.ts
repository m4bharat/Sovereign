import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AssistantFacade } from './assistant.facade';
import { SessionService } from '../../core/services/session.service';
import { ROLES } from '../../core/constants/options';
import { AiConversationDecisionRequest } from '../../core/models/assistant.models';

@Component({
  standalone: true,
  imports: [CommonModule, FormsModule],
  providers: [AssistantFacade],
  templateUrl: './assistant-page.component.html'
})
export class AssistantPageComponent {
  readonly roles = ROLES;

  readonly form: AiConversationDecisionRequest = {
    userId: '',
    contactId: 'contact-001',
    relationshipRole: 'Friend',
    message: 'Remember that my birthday is Jan 10'
  };

  readonly session = inject(SessionService);

  constructor(public readonly facade: AssistantFacade) {
    this.form.userId = this.session.userId() || 'user-001';
  }

  run(): void {
    this.facade.run({ ...this.form });
  }

  reset(): void {
    this.form.userId = 'user-001';
    this.form.contactId = 'contact-001';
    this.form.relationshipRole = 'Friend';
    this.form.message = 'Remember that my birthday is Jan 10';
    this.facade.clear();
  }
}
