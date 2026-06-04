import { Routes } from '@angular/router';
import { DayView } from './day-view/day-view';
import { WeekView } from './week-view/week-view';

export const routes: Routes = [
  { path: '', redirectTo: 'day', pathMatch: 'full' },
  { path: 'day', component: DayView },
  { path: 'week', component: WeekView },
];
