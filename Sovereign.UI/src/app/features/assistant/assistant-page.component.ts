import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AssistantFacade } from './assistant.facade';
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
    userId: 'user-001',
    contactId: 'contact-001',
    relationshipRole: 'Friend',
    message: 'Remember that my birthday is Jan 10'
  };

  constructor(public readonly facade: AssistantFacade) {}

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
