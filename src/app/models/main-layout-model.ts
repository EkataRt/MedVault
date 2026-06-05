import { Appointment, Medicine } from './med-vault-model';

export interface CalendarDay {
  appointments: Appointment[];
  date: Date;
  isCurrentMonth: boolean;
  isToday: boolean;
  medicines: Medicine[];
}
