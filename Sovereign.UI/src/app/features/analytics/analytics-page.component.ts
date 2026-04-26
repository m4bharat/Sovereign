import { CommonModule } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { forkJoin } from 'rxjs';
import { SuggestionAnalyticsResponse, SuggestionFailureResponse, SuggestionMetricBucket } from '../../core/models/telemetry.models';
import { TelemetryApiService } from '../../core/services/telemetry-api.service';

@Component({
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="analytics-page">
      <header class="hero">
        <div>
          <p class="eyebrow">Product Quality Loop</p>
          <h1>Sovereign analytics</h1>
          <p class="subtext">Track whether generated suggestions become useful actions instead of vanity completions.</p>
        </div>
      </header>

      <section class="card-grid" *ngIf="summary() as data">
        <article class="metric-card">
          <span>Total Suggestions</span>
          <strong>{{ data.totalSuggestions }}</strong>
        </article>
        <article class="metric-card">
          <span>Acceptance Rate</span>
          <strong>{{ asPercent(data.acceptanceRate) }}</strong>
        </article>
        <article class="metric-card">
          <span>Post Rate</span>
          <strong>{{ asPercent(data.postRate) }}</strong>
        </article>
        <article class="metric-card">
          <span>Discard Rate</span>
          <strong>{{ asPercent(data.discardRate) }}</strong>
        </article>
        <article class="metric-card">
          <span>Average Edit Ratio</span>
          <strong>{{ asPercent(data.averageEditRatio) }}</strong>
        </article>
        <article class="metric-card">
          <span>Regeneration Rate</span>
          <strong>{{ asPercent(data.regenerationRate) }}</strong>
        </article>
        <article class="metric-card">
          <span>Average Latency</span>
          <strong>{{ data.averageLatencyMs | number:'1.0-0' }} ms</strong>
        </article>
        <article class="metric-card danger">
          <span>Hallucination Rate</span>
          <strong>{{ asPercent(data.hallucinationRate) }}</strong>
        </article>
      </section>

      <section class="summary-grid" *ngIf="summary() as data">
        <article class="panel">
          <h2>Feedback rates</h2>
          <div class="targets compact">
            <p>Generic: {{ asPercent(data.genericComplaintRate) }}</p>
            <p>Wrong Context: {{ asPercent(data.wrongContextRate) }}</p>
            <p>Wrong Tone: {{ asPercent(data.wrongToneRate) }}</p>
            <p>Hallucination: {{ asPercent(data.hallucinationRate) }}</p>
          </div>
        </article>

        <article class="panel">
          <h2>Lifecycle totals</h2>
          <div class="targets compact">
            <p>Generated: {{ data.totalGenerated }}</p>
            <p>Inserted: {{ data.totalInserted }}</p>
            <p>Posted: {{ data.totalPosted }}</p>
            <p>Discarded: {{ data.totalDiscarded }}</p>
            <p>Regenerated: {{ data.totalRegenerated }}</p>
            <p>Avg Latency: {{ data.averageLatencyMs | number:'1.0-0' }} ms</p>
          </div>
        </article>

        <article class="panel">
          <h2>Beta targets</h2>
          <div class="targets">
            <p [class.good]="data.acceptanceRate >= 0.55">Acceptance Rate: {{ asPercent(data.acceptanceRate) }} / 55%+</p>
            <p [class.good]="data.postRate >= 0.35">Post Rate: {{ asPercent(data.postRate) }} / 35%+</p>
            <p [class.good]="data.averageEditRatio < 0.35">Average Edit Ratio: {{ asPercent(data.averageEditRatio) }} / &lt; 35%</p>
            <p [class.good]="data.regenerationRate < 0.2">Regeneration Rate: {{ asPercent(data.regenerationRate) }} / &lt; 20%</p>
            <p [class.good]="data.genericComplaintRate < 0.08">Generic Complaint Rate: {{ asPercent(data.genericComplaintRate) }} / &lt; 8%</p>
            <p [class.good]="data.wrongContextRate < 0.05">Wrong Context Rate: {{ asPercent(data.wrongContextRate) }} / &lt; 5%</p>
            <p [class.good]="data.hallucinationRate < 0.02">Hallucination Rate: {{ asPercent(data.hallucinationRate) }} / &lt; 2%</p>
          </div>
        </article>
      </section>

      <section class="tables">
        <article class="panel">
          <h2>By Surface</h2>
          <table>
            <thead>
              <tr>
                <th>Surface</th>
                <th>Count</th>
                <th>Acceptance</th>
                <th>Edit Ratio</th>
                <th>Regeneration</th>
                <th>Avg Latency</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let bucket of bySurface()">
                <td>{{ bucket.name }}</td>
                <td>{{ bucket.count }}</td>
                <td>{{ asPercent(bucket.acceptanceRate) }}</td>
                <td>{{ asPercent(bucket.averageEditRatio) }}</td>
                <td>{{ asPercent(bucket.regenerationRate) }}</td>
                <td>{{ bucket.averageLatencyMs | number:'1.0-0' }} ms</td>
              </tr>
            </tbody>
          </table>
        </article>

        <article class="panel">
          <h2>By Situation</h2>
          <table>
            <thead>
              <tr>
                <th>Situation</th>
                <th>Count</th>
                <th>Acceptance</th>
                <th>Edit Ratio</th>
                <th>Regeneration</th>
                <th>Avg Latency</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let bucket of bySituation()">
                <td>{{ bucket.name }}</td>
                <td>{{ bucket.count }}</td>
                <td>{{ asPercent(bucket.acceptanceRate) }}</td>
                <td>{{ asPercent(bucket.averageEditRatio) }}</td>
                <td>{{ asPercent(bucket.regenerationRate) }}</td>
                <td>{{ bucket.averageLatencyMs | number:'1.0-0' }} ms</td>
              </tr>
            </tbody>
          </table>
        </article>

        <article class="panel">
          <h2>By Move</h2>
          <table>
            <thead>
              <tr>
                <th>Move</th>
                <th>Count</th>
                <th>Acceptance</th>
                <th>Edit Ratio</th>
                <th>Regeneration</th>
                <th>Avg Latency</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let bucket of byMove()">
                <td>{{ bucket.name }}</td>
                <td>{{ bucket.count }}</td>
                <td>{{ asPercent(bucket.acceptanceRate) }}</td>
                <td>{{ asPercent(bucket.averageEditRatio) }}</td>
                <td>{{ asPercent(bucket.regenerationRate) }}</td>
                <td>{{ bucket.averageLatencyMs | number:'1.0-0' }} ms</td>
              </tr>
            </tbody>
          </table>
        </article>
      </section>

      <section class="tables">
        <article class="panel">
          <h2>Worst Performing Buckets</h2>
          <table>
            <thead>
              <tr>
                <th>Bucket</th>
                <th>Count</th>
                <th>Acceptance</th>
                <th>Edit Ratio</th>
                <th>Regeneration</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let bucket of worstBuckets()">
                <td>{{ bucket.name }}</td>
                <td>{{ bucket.count }}</td>
                <td>{{ asPercent(bucket.acceptanceRate) }}</td>
                <td>{{ asPercent(bucket.averageEditRatio) }}</td>
                <td>{{ asPercent(bucket.regenerationRate) }}</td>
              </tr>
            </tbody>
          </table>
        </article>

        <article class="panel">
          <h2>Recent Failures</h2>
          <table>
            <thead>
              <tr>
                <th>Surface</th>
                <th>Situation</th>
                <th>Move</th>
                <th>Reason</th>
                <th>Edit Ratio</th>
              </tr>
            </thead>
            <tbody>
              <tr *ngFor="let failure of recentFailures()">
                <td>{{ failure.surface }}</td>
                <td>{{ failure.situationType }}</td>
                <td>{{ failure.move }}</td>
                <td>{{ failure.failureReason }}</td>
                <td>{{ asPercent(failure.editRatio) }}</td>
              </tr>
            </tbody>
          </table>
        </article>
      </section>

      <p class="empty" *ngIf="error()">{{ error() }}</p>
    </div>
  `,
  styles: [
    `
      :host { display: block; }
      .analytics-page { min-height: 100vh; padding: 32px; background: linear-gradient(180deg, #f8f4ee 0%, #eef3f8 100%); color: #122033; }
      .hero { display: flex; justify-content: space-between; align-items: end; margin-bottom: 24px; }
      .eyebrow { text-transform: uppercase; letter-spacing: 0.12em; font-size: 12px; color: #8a4b2a; margin: 0 0 8px; font-weight: 700; }
      h1 { margin: 0; font-size: clamp(32px, 6vw, 56px); line-height: 0.95; }
      .subtext { max-width: 720px; color: #506074; margin-top: 10px; font-size: 16px; }
      .card-grid { display: grid; grid-template-columns: repeat(auto-fit, minmax(180px, 1fr)); gap: 16px; margin-bottom: 24px; }
      .metric-card, .panel { background: rgba(255, 255, 255, 0.84); backdrop-filter: blur(10px); border: 1px solid rgba(18, 32, 51, 0.08); border-radius: 22px; box-shadow: 0 18px 60px rgba(18, 32, 51, 0.08); }
      .metric-card { padding: 20px; display: grid; gap: 10px; }
      .metric-card span { color: #5b697a; font-size: 13px; text-transform: uppercase; letter-spacing: 0.08em; }
      .metric-card strong { font-size: 32px; }
      .metric-card.danger strong { color: #b23b2e; }
      .summary-grid, .tables { display: grid; grid-template-columns: repeat(auto-fit, minmax(320px, 1fr)); gap: 18px; margin-bottom: 20px; }
      .panel { padding: 22px; overflow: auto; }
      h2 { margin: 0 0 16px; font-size: 20px; }
      .targets { display: grid; gap: 10px; }
      .targets p { margin: 0; padding: 12px 14px; border-radius: 14px; background: #f4f7fa; }
      .targets.compact { grid-template-columns: repeat(auto-fit, minmax(140px, 1fr)); }
      .good { background: #e6f5ec !important; color: #17653c; font-weight: 600; }
      table { width: 100%; border-collapse: collapse; min-width: 520px; }
      th, td { text-align: left; padding: 12px 10px; border-bottom: 1px solid rgba(18, 32, 51, 0.08); }
      th { color: #5b697a; font-size: 12px; text-transform: uppercase; letter-spacing: 0.08em; }
      .empty { margin: 24px 0 0; color: #b23b2e; font-weight: 600; }
      @media (max-width: 720px) {
        .analytics-page { padding: 18px; }
        .panel { padding: 16px; }
      }
    `
  ]
})
export class AnalyticsPageComponent {
  private readonly telemetryApi = inject(TelemetryApiService);

  readonly summary = signal<SuggestionAnalyticsResponse | null>(null);
  readonly bySurface = signal<SuggestionMetricBucket[]>([]);
  readonly bySituation = signal<SuggestionMetricBucket[]>([]);
  readonly byMove = signal<SuggestionMetricBucket[]>([]);
  readonly recentFailures = signal<SuggestionFailureResponse[]>([]);
  readonly error = signal('');

  readonly worstBuckets = computed(() =>
    [...this.byMove()]
      .sort((left, right) => this.bucketScore(right) - this.bucketScore(left))
      .slice(0, 6)
  );

  constructor() {
    forkJoin({
      summary: this.telemetryApi.getSummary(),
      bySurface: this.telemetryApi.getBySurface(),
      bySituation: this.telemetryApi.getBySituation(),
      byMove: this.telemetryApi.getByMove(),
      recentFailures: this.telemetryApi.getRecentFailures()
    }).subscribe({
      next: ({ summary, bySurface, bySituation, byMove, recentFailures }) => {
        this.summary.set(summary);
        this.bySurface.set(bySurface);
        this.bySituation.set(bySituation);
        this.byMove.set(byMove);
        this.recentFailures.set(recentFailures);
      },
      error: () => {
        this.error.set('Unable to load telemetry analytics right now.');
      }
    });
  }

  asPercent(value: number): string {
    return `${(value * 100).toFixed(1)}%`;
  }

  private bucketScore(bucket: SuggestionMetricBucket): number {
    return (1 - bucket.acceptanceRate) + bucket.averageEditRatio + bucket.regenerationRate;
  }
}
