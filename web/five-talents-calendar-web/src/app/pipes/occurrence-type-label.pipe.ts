import { Pipe, PipeTransform } from '@angular/core';

const LABELS: Record<string, string> = {
  PrincipalFeast: 'Principal Feast',
  MajorFeast: 'Major Feast',
  Sunday: 'Sunday',
  SeasonDay: 'Season Day',
  AnglicanCommemoration: 'Anglican Commemoration',
  EcumenicalCommemoration: 'Ecumenical Commemoration',
  NationalDay: 'National Day',
  EmberDay: 'Ember Day',
  RogationDay: 'Rogation Day',
  Antiphon: 'Antiphon',
};

@Pipe({ name: 'occurrenceTypeLabel' })
export class OccurrenceTypeLabelPipe implements PipeTransform {
  transform(type: string): string {
    return LABELS[type] ?? type;
  }
}
