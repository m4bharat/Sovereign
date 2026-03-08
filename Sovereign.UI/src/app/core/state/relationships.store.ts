import { Injectable, signal } from '@angular/core';
import { RelationshipCardVm } from '../models/relationship.models';

@Injectable({ providedIn: 'root' })
export class RelationshipsStore {
  private readonly _items = signal<RelationshipCardVm[]>([]);
  readonly items = this._items.asReadonly();

  add(item: RelationshipCardVm): void {
    if (this._items().some(x => x.relationshipId === item.relationshipId)) return;
    this._items.update(items => [item, ...items]);
  }

  updateTemperature(
    relationshipId: string,
    patch: Pick<RelationshipCardVm, 'score' | 'silenceDays' | 'temperature' | 'recommendedAction'>
  ): void {
    this._items.update(items =>
      items.map(item => item.relationshipId === relationshipId ? { ...item, ...patch } : item)
    );
  }
}
