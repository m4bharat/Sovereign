import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DecayRadarFacade } from './decay-radar.facade';
import { SessionService } from '../../core/services/session.service';

@Component({
  standalone: true,
  imports: [CommonModule, FormsModule],
  providers: [DecayRadarFacade],
  templateUrl: './decay-radar-page.component.html'
})
export class DecayRadarPageComponent {
  readonly session = inject(SessionService);
  userId = this.session.userId() || 'user-001';

  constructor(public readonly facade: DecayRadarFacade) {}

  load(): void {
    this.facade.load(this.userId);
  }

  clear(): void {
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
