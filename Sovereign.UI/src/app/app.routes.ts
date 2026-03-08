import { Routes } from '@angular/router';
import { RewritePageComponent } from './features/rewrite/rewrite-page.component';
import { RelationshipsPageComponent } from './features/relationships/relationships-page.component';
import { DecayRadarPageComponent } from './features/decay-radar/decay-radar-page.component';
import { AssistantPageComponent } from './features/assistant/assistant-page.component';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'rewrite' },
  { path: 'rewrite', component: RewritePageComponent },
  { path: 'relationships', component: RelationshipsPageComponent },
  { path: 'decay-radar', component: DecayRadarPageComponent },
  { path: 'assistant', component: AssistantPageComponent },
  { path: '**', redirectTo: 'rewrite' }
];
