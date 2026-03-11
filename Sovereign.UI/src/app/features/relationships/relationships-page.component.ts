import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RelationshipsFacade } from './relationships.facade';
import { ROLES } from '../../core/constants/options';
import { CreateRelationshipRequest } from '../../core/models/relationship.models';
import { SessionService } from '../../core/services/session.service';

@Component({
  standalone: true,
  imports: [CommonModule, FormsModule],
  providers: [RelationshipsFacade],
  templateUrl: './relationships-page.component.html'
})
export class RelationshipsPageComponent {
  readonly roles = ROLES;

  readonly form: CreateRelationshipRequest = {
    userId: '',
    contactId: 'contact-001',
    role: 'Investor'
  };

  readonly session = inject(SessionService);

  constructor(public readonly facade: RelationshipsFacade) {
    this.form.userId = this.session.userId() || 'user-001';
  }

  create(): void {
    this.facade.createRelationship({ ...this.form });
  }

  refresh(id: string): void {
    this.facade.refreshTemperature(id);
  }

  refreshAll(): void {
    this.facade.refreshAll();
  }

  async copy(text: string): Promise<void> {
    try {
      await navigator.clipboard.writeText(text);
    } catch {
      this.facade.error.set('Clipboard copy failed.');
    }
  }
}
