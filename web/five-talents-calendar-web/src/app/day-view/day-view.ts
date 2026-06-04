import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { DatePipe } from '@angular/common';
import { toSignal, toObservable } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { switchMap, of } from 'rxjs';

import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatExpansionModule } from '@angular/material/expansion';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';
import { FormsModule } from '@angular/forms';

import { CalendarService } from '../services/calendar.service';
import { LiturgicalDay, Tradition } from '../models/liturgical-day.model';
import { SeasonLabelPipe } from '../pipes/season-label.pipe';
import { ReadingTypeLabelPipe } from '../pipes/reading-type-label.pipe';

@Component({
  selector: 'app-day-view',
  templateUrl: './day-view.html',
  styleUrl: './day-view.scss',
  imports: [
    FormsModule,
    RouterLink,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatExpansionModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatSelectModule,
    MatTooltipModule,
    DatePipe,
    SeasonLabelPipe,
    ReadingTypeLabelPipe,
  ],
})
export class DayView implements OnInit {
  private readonly calendarService = inject(CalendarService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  selectedDate = signal<string>(todayIso());
  selectedTradition = signal<string>('AcnaBcp2019');

  traditions = toSignal(this.calendarService.getTraditions(), {
    initialValue: [] as Tradition[],
  });

  day = toSignal(
    toObservable(computed(() => ({ date: this.selectedDate(), tradition: this.selectedTradition() }))).pipe(
      switchMap(({ date, tradition }) =>
        date && tradition ? this.calendarService.getDay(tradition, date) : of(null),
      ),
    ),
  );

  colorClass = computed(() => liturgicalColorClass(this.day()?.feast?.color ?? null, this.day()?.season ?? ''));

  ngOnInit(): void {
    const params = this.route.snapshot.queryParamMap;
    const date = params.get('date');
    const tradition = params.get('tradition');
    if (date) this.selectedDate.set(date);
    if (tradition) this.selectedTradition.set(tradition);
  }

  onDateChange(date: string): void {
    this.selectedDate.set(date);
    this.syncUrl();
  }

  onTraditionChange(tradition: string): void {
    this.selectedTradition.set(tradition);
    this.syncUrl();
  }

  navigateDay(offset: number): void {
    const d = new Date(this.selectedDate() + 'T00:00:00');
    d.setDate(d.getDate() + offset);
    this.selectedDate.set(isoDate(d));
    this.syncUrl();
  }

  weekUrl = computed(() => `/week?date=${this.selectedDate()}&tradition=${this.selectedTradition()}`);

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

export function liturgicalColorClass(color: string | null, season: string): string {
  const c = color ?? seasonFallbackColor(season);
  return `liturgical-color--${c.toLowerCase()}`;
}

function seasonFallbackColor(season: string): string {
  switch (season) {
    case 'Advent': return 'Blue';
    case 'Lent':
    case 'HolyWeek': return 'Purple';
    case 'Easter': return 'White';
    case 'OrdinaryTime':
    case 'Pentecost': return 'Green';
    default: return 'White';
  }
}
