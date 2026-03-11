import { Routes } from '@angular/router';
import { RewritePageComponent } from './features/rewrite/rewrite-page.component';
import { RelationshipsPageComponent } from './features/relationships/relationships-page.component';
import { DecayRadarPageComponent } from './features/decay-radar/decay-radar-page.component';
import { AssistantPageComponent } from './features/assistant/assistant-page.component';
import { DashboardPageComponent } from './features/dashboard/dashboard-page.component';
import { AuthPageComponent } from './features/auth/auth-page.component';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'dashboard' },
  { path: 'auth', component: AuthPageComponent },
  { path: 'dashboard', component: DashboardPageComponent },
  { path: 'rewrite', component: RewritePageComponent },
  { path: 'relationships', component: RelationshipsPageComponent },
  { path: 'decay-radar', component: DecayRadarPageComponent },
  { path: 'assistant', component: AssistantPageComponent },
  { path: '**', redirectTo: 'dashboard' }
];
