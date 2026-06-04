import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { toSignal, toObservable } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { switchMap, of } from 'rxjs';

import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatTooltipModule } from '@angular/material/tooltip';

import { CalendarService } from '../services/calendar.service';
import { LiturgicalDay } from '../models/liturgical-day.model';
import { SeasonLabelPipe } from '../pipes/season-label.pipe';
import { liturgicalColorClass } from '../day-view/day-view';

@Component({
  selector: 'app-week-view',
  templateUrl: './week-view.html',
  styleUrl: './week-view.scss',
  imports: [
    DatePipe,
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatIconModule,
    MatTooltipModule,
    SeasonLabelPipe,
  ],
})
export class WeekView implements OnInit {
  private readonly calendarService = inject(CalendarService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  selectedDate = signal<string>(todayIso());
  selectedTradition = signal<string>('AcnaBcp2019');

  private weekRange = computed(() => {
    const d = new Date(this.selectedDate() + 'T00:00:00');
    const day = d.getDay();
    const monday = new Date(d);
    monday.setDate(d.getDate() - ((day + 6) % 7));
    const sunday = new Date(monday);
    sunday.setDate(monday.getDate() + 6);
    return { from: isoDate(monday), to: isoDate(sunday) };
  });

  weekLabel = computed(() => {
    const { from, to } = this.weekRange();
    const f = new Date(from + 'T00:00:00');
    const t = new Date(to + 'T00:00:00');
    return `${f.toLocaleDateString('en-US', { month: 'short', day: 'numeric' })} – ${t.toLocaleDateString('en-US', { month: 'short', day: 'numeric', year: 'numeric' })}`;
  });

  days = toSignal(
    toObservable(computed(() => ({ ...this.weekRange(), tradition: this.selectedTradition() }))).pipe(
      switchMap(({ from, to, tradition }) =>
        from && tradition ? this.calendarService.getRange(tradition, from, to) : of([]),
      ),
    ),
    { initialValue: [] as LiturgicalDay[] },
  );

  colorClassFor(day: LiturgicalDay): string {
    return liturgicalColorClass(day.feast?.color ?? null, day.season);
  }

  ngOnInit(): void {
    const params = this.route.snapshot.queryParamMap;
    const date = params.get('date');
    const tradition = params.get('tradition');
    if (date) this.selectedDate.set(date);
    if (tradition) this.selectedTradition.set(tradition);
  }

  navigateWeek(offset: number): void {
    const d = new Date(this.selectedDate() + 'T00:00:00');
    d.setDate(d.getDate() + offset * 7);
    this.selectedDate.set(isoDate(d));
    this.syncUrl();
  }

  dayUrl(date: string): string[] {
    return ['/day'];
  }

  dayQueryParams(date: string): Record<string, string> {
    return { date, tradition: this.selectedTradition() };
  }

  private syncUrl(): void {
    this.router.navigate([], {
      queryParams: { date: this.selectedDate(), tradition: this.selectedTradition() },
      replaceUrl: true,
    });
  }
}

function todayIso(): string {
  return isoDate(new Date());
}

function isoDate(d: Date): string {
  return d.toISOString().split('T')[0];
}
