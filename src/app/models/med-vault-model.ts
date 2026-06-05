export interface Appointment {
  category: string;
  date: string;
  doctor: string;
  followUpInterval?: string;
  hospital: string;
  id: string;
  isFollowUp?: boolean;
  location?: string;
  previousAppointmentId?: string;
  time: string;
  title: string;
  userId: string;
  visitedDate?: string;
  visited: boolean;
}

export interface HealthProfile {
  age: number;
  allergies: string[];
  bloodType: string;
  fullName: string;
  height: number;
  id: string;
  lastCheckup: string;
  sex: 'Male' | 'Female' | 'Other';
  userId: string;
  weight: number;
}

export interface Medicine {
  condition: string;
  dosage: string;
  endDate: string;
  frequency: string;
  id: string;
  lastTakenDates: (string | null)[];
  mealPreference: 'before' | 'after' | 'any';
  name: string;
  startDate: string;
  times: string[];
  timesPerDay: number;
  userId: string;
}

export interface MedVaultData {
  appointments: Appointment[];
  healthProfiles: HealthProfile[];
  medicines: Medicine[];
  users: User[];
}

export interface Notification {
  body: string;
  createdAt: string;
  doseIndex?: number;
  id: string;
  read: boolean;
  referenceId: string;
  title: string;
  type: 'appointment' | 'medicine';
  userId: string;
}

export interface User {
  email: string;
  googleAccessToken?: string;
  googleCalendarConnected?: boolean;
  googleTokenExpiry?: number;
  id: string;
  password?: string;
  username: string;
}

export interface Folder {
  id: string;
  name: string;
  parentId: string | null;
  userId: string;
  createdAt: string;
}

export interface MedDocument {
  id: string;
  name: string;
  folderId: string;
  fileName: string;
  url: string;
  userId: string;
  uploadedAt: string;
}

export interface WeeklyConsistency {
  appointmentConsistency: number;
  medicineConsistency: number;
  week: string;
}

export interface NotificationExtra {
  doseIndex?: number;
  medicineId?: string;
  referenceId: string;
  type: 'appointment' | 'medicine';
}
