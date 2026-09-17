export interface LiturgicalWeek {
  season: string;
  weekNumber: number;
  lectionaryYear: string;
}

export interface FeastDay {
  name: string;
  rank: string;
  color: string | null;
}

export interface LectionaryReading {
  type: string;
  citation: string;
  alternateCitations: string[];
  translationCode: string | null;
}

export interface LiturgicalService {
  name: string | null;
  readings: LectionaryReading[];
}

export interface DailyOfficeReadings {
  morningPrayer: LiturgicalService;
  eveningPrayer: LiturgicalService;
}

export type OccurrenceType =
  | 'PrincipalFeast'
  | 'MajorFeast'
  | 'Sunday'
  | 'SeasonDay'
  | 'AnglicanCommemoration'
  | 'EcumenicalCommemoration'
  | 'NationalDay'
  | 'EmberDay'
  | 'RogationDay'
  | 'Antiphon';

export type ObservancePrecedence = 'Prescribed' | 'CommonPractice' | 'Supplementary';

export interface Collect {
  text: string;
  alternateTexts: string[];
}

/**
 * One occurrence in a day's ordered occurrence stack — a Feast, the Sunday's own slot, a
 * commemoration, an Ember/Rogation Day, or season-day naming. Supersedes the old
 * `feast`/`commemorations`/`sundayTitle` split; see ADR 0016.
 */
export interface Occurrence {
  type: OccurrenceType;
  name: string | null;
  feast: FeastDay | null;
  precedence: ObservancePrecedence;
  services: LiturgicalService[];
  rubricNote: string | null;
  yieldedFeast: FeastDay | null;
  collect: Collect | null;
  prefaceNames: string[];
}

export interface LiturgicalDay {
  date: string;
  season: string;
  week: LiturgicalWeek;
  occurrences: Occurrence[];
  readings: LiturgicalService[];
  dailyOffice: DailyOfficeReadings;
  properNumber: number | null;
  isEmberDay: boolean;
  isRogationDay: boolean;
  isFastDay: boolean;
}

export interface Tradition {
  tradition: string;
  name: string;
}
