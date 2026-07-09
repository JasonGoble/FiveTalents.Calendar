import { Pipe, PipeTransform } from '@angular/core';

const LABELS: Record<string, string> = {
  FirstLesson: 'First Lesson',
  Psalm: 'Psalm',
  SecondLesson: 'Second Lesson',
  Gospel: 'Gospel',
  // Daily Office regular calendar-date grid: each office gets exactly one lesson,
  // deliberately not labeled "First"/"Second" since it doesn't reliably mean OT/NT
  // here the way it does for the Eucharist (see ADR 0005).
  MorningPrayer: 'Lesson',
  EveningPrayer: 'Lesson',
};

@Pipe({ name: 'readingTypeLabel' })
export class ReadingTypeLabelPipe implements PipeTransform {
  transform(type: string): string {
    return LABELS[type] ?? type;
  }
}
