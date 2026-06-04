import { Pipe, PipeTransform } from '@angular/core';

const LABELS: Record<string, string> = {
  Advent: 'Advent',
  Christmas: 'Christmas',
  Epiphany: 'Epiphany',
  Lent: 'Lent',
  HolyWeek: 'Holy Week',
  Easter: 'Easter',
  Pentecost: 'Pentecost',
  OrdinaryTime: 'Ordinary Time',
};

@Pipe({ name: 'seasonLabel' })
export class SeasonLabelPipe implements PipeTransform {
  transform(season: string): string {
    return LABELS[season] ?? season;
  }
}
