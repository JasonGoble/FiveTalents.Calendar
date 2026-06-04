import { inject, Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

import { LiturgicalDay, Tradition } from '../models/liturgical-day.model';

const API_BASE = 'http://localhost:5299';

@Injectable({ providedIn: 'root' })
export class CalendarService {
  private readonly http = inject(HttpClient);

  getTraditions(): Observable<Tradition[]> {
    return this.http.get<Tradition[]>(`${API_BASE}/calendar/traditions`);
  }

  getDay(tradition: string, date: string): Observable<LiturgicalDay> {
    return this.http.get<LiturgicalDay>(`${API_BASE}/calendar/${tradition}/day/${date}`);
  }

  getRange(tradition: string, from: string, to: string): Observable<LiturgicalDay[]> {
    return this.http.get<LiturgicalDay[]>(
      `${API_BASE}/calendar/${tradition}/range?from=${from}&to=${to}`,
    );
  }
}
