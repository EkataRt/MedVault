using System;
using System.Collections.Generic;
using System.Text.Json;
using MedVaultAPI.Models;

namespace MedVaultAPI.Services
{
    public class SchedulingConflictService
    {
        private static readonly TimeSpan DefaultAppointmentDuration = TimeSpan.FromMinutes(30);

        // --- Appointment Conflict Check ---
        public bool HasAppointmentConflict(Appointment newAppt, List<Appointment> existingAppointments)
        {
            if (!TryParseDateTime(newAppt.Date, newAppt.Time, out var newStart))
                return false;
            
            var newEnd = newStart.Add(DefaultAppointmentDuration);

            foreach (var existing in existingAppointments)
            {
                if (existing.Id == newAppt.Id) continue;

                if (TryParseDateTime(existing.Date, existing.Time, out var existingStart))
                {
                    var existingEnd = existingStart.Add(DefaultAppointmentDuration);

                    if (newStart < existingEnd && existingStart < newEnd)
                        return true;
                }
            }
            return false;
        }

        public bool HasMedicineConflict(    Medicine newMed,    List<Medicine> existingMedicines,    int bufferMinutes = 30)
        {
            // Parse the new medicine's date range
            if (!DateTime.TryParse(newMed.StartDate, out var newStartDate) ||
                !DateTime.TryParse(newMed.EndDate, out var newEndDate))
            {
                return false;
            }

            if (newMed.Times == null || !newMed.Times.Any())
                return false;

            foreach (var existing in existingMedicines)
            {
                // Don't compare the medicine with itself
                if (existing.Id == newMed.Id)
                    continue;

                // Parse existing medicine's date range
                if (!DateTime.TryParse(existing.StartDate, out var existingStartDate) ||
                    !DateTime.TryParse(existing.EndDate, out var existingEndDate))
                {
                    continue;
                }

                // --------------------------------------------------
                // STEP 1: Check whether the date ranges overlap
                // --------------------------------------------------

                bool dateRangesOverlap =
                    newStartDate <= existingEndDate &&
                    existingStartDate <= newEndDate;

                // If the date ranges don't overlap, there is
                // definitely no medicine scheduling conflict.
                if (!dateRangesOverlap)
                    continue;

                // --------------------------------------------------
                // STEP 2: Get existing medicine times
                // --------------------------------------------------

                var existingTimes =
                    JsonSerializer.Deserialize<List<string>>(
                        existing.TimesJson ?? "[]"
                    ) ?? new List<string>();

                // --------------------------------------------------
                // STEP 3: Compare medication times
                // --------------------------------------------------

                foreach (var newTimeStr in newMed.Times)
                {
                    if (!TimeSpan.TryParse(newTimeStr, out var newTime))
                        continue;

                    foreach (var existTimeStr in existingTimes)
                    {
                        if (!TimeSpan.TryParse(existTimeStr, out var existTime))
                            continue;

                        double diffMinutes =
                            Math.Abs((newTime - existTime).TotalMinutes);

                        // Doses are too close together
                        if (diffMinutes < bufferMinutes)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        private bool TryParseDateTime(string? dateStr, string? timeStr, out DateTime parsedDateTime)
        {
            parsedDateTime = default;
            if (string.IsNullOrWhiteSpace(dateStr) || string.IsNullOrWhiteSpace(timeStr))
                return false;

            return DateTime.TryParse($"{dateStr} {timeStr}", out parsedDateTime);
        }
    }
}