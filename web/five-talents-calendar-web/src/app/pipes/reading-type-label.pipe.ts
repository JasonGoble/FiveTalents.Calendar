import { Pipe, PipeTransform } from '@angular/core';

const LABELS: Record<string, string> = {
  FirstLesson: 'First Lesson',
  Psalm: 'Psalm',
  SecondLesson: 'Second Lesson',
  Gospel: 'Gospel',
};

@Pipe({ name: 'readingTypeLabel' })
export class ReadingTypeLabelPipe implements PipeTransform {
  transform(type: string): string {
    return LABELS[type] ?? type;
  }
}
