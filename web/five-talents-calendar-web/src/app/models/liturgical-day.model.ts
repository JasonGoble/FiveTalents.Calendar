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

export interface LiturgicalDay {
  date: string;
  season: string;
  week: LiturgicalWeek;
  feast: FeastDay | null;
  commemorations: FeastDay[];
  readings: LiturgicalService[];
  properNumber: number | null;
  isEmberDay: boolean;
  isRogationDay: boolean;
  isFastDay: boolean;
}

export interface Tradition {
  tradition: string;
  name: string;
}
