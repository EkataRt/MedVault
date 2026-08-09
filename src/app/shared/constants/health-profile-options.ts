export interface SelectOption {
  label: string;
  value: string;
}

export const SEX_OPTIONS: SelectOption[] = [
  { label: 'Male', value: 'Male' },
  { label: 'Female', value: 'Female' },
  { label: 'Other', value: 'Other' },
];

export const BLOOD_TYPE_OPTIONS: SelectOption[] = [
  { label: 'A+', value: 'A+' },
  { label: 'A-', value: 'A-' },
  { label: 'B+', value: 'B+' },
  { label: 'B-', value: 'B-' },
  { label: 'AB+', value: 'AB+' },
  { label: 'AB-', value: 'AB-' },
  { label: 'O+', value: 'O+' },
  { label: 'O-', value: 'O-' },
];

export const COMMON_ALLERGIES: string[] = [
  'Peanuts',
  'Shellfish',
  'Dairy',
  'Eggs',
  'Penicillin',
  'Pollen',
  'Dust',
  'Latex',
];
