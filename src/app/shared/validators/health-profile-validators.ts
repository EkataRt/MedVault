import { AbstractControl, ValidationErrors, ValidatorFn } from '@angular/forms';

const MAX_AGE_YEARS = 100;
const MIN_AGE_MONTHS = 1;
const MAX_CHECKUP_YEARS = 3;

export function nameValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value: string = control.value ?? '';
    if (!value) return null;
    const pattern = /^[a-zA-Z\s]+$/;
    return pattern.test(value) ? null : { invalidName: true };
  };
}

export function alphaOnlyValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value: string = control.value ?? '';
    if (!value) return null;
    const pattern = /^[a-zA-Z]+$/;
    return pattern.test(value) ? null : { invalidAlpha: true };
  };
}

export function dobAgeRangeValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value: string = control.value;
    if (!value) return null;
    const dob = new Date(value);
    const today = new Date();
    if (dob.getTime() > today.getTime()) return { dateInFuture: true };
    const totalMonths = calculateMonthsDifference(dob, today);
    if (totalMonths < MIN_AGE_MONTHS) return { tooYoung: true };
    if (totalMonths > MAX_AGE_YEARS * 12) return { tooOld: true };
    return null;
  };
}

export function lastCheckupRangeValidator(): ValidatorFn {
  return (control: AbstractControl): ValidationErrors | null => {
    const value: string = control.value;
    if (!value) return null;
    const checkupDate = new Date(value);
    const today = new Date();
    today.setHours(23, 59, 59, 999);
    if (checkupDate.getTime() > today.getTime()) return { dateInFuture: true };
    const earliestAllowed = new Date();
    earliestAllowed.setFullYear(
      earliestAllowed.getFullYear() - MAX_CHECKUP_YEARS,
    );
    return checkupDate.getTime() < earliestAllowed.getTime()
      ? { tooOld: true }
      : null;
  };
}

export function calculateAgeLabel(dateOfBirth: string): string {
  if (!dateOfBirth) return '';
  const dob = new Date(dateOfBirth);
  const today = new Date();
  if (dob.getTime() > today.getTime()) return '';
  const totalMonths = calculateMonthsDifference(dob, today);
  const years = Math.floor(totalMonths / 12);
  const months = totalMonths % 12;
  if (years < 1) return `${months} month${months === 1 ? '' : 's'}`;
  if (months === 0) return `${years} year${years === 1 ? '' : 's'}`;
  return `${years} year${years === 1 ? '' : 's'}, ${months} month${months === 1 ? '' : 's'}`;
}

function calculateMonthsDifference(from: Date, to: Date): number {
  let months = (to.getFullYear() - from.getFullYear()) * 12;
  months -= from.getMonth();
  months += to.getMonth();
  if (to.getDate() < from.getDate()) months -= 1;
  return months <= 0 ? 0 : months;
}
