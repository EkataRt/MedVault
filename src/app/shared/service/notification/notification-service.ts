import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Subject } from 'rxjs';
import {
  ActionPerformed,
  LocalNotifications,
  LocalNotificationSchema,
  ScheduleOptions,
} from '@capacitor/local-notifications';
import dayjs from 'dayjs';
import { firstValueFrom } from 'rxjs';
import {
  Appointment,
  Medicine,
  Notification,
  NotificationExtra,
} from '../../../models/med-vault-model';
import { environment } from '../../../../environments/environment';

@Injectable({
  providedIn: 'root',
})
export class NotificationService {
  private readonly apiUrl = environment.apiUrl;
  private readonly maxInApp = 10;
  private readonly _newNotification$ = new Subject<void>();

  public readonly newNotification$ = this._newNotification$.asObservable();

  constructor(private readonly http: HttpClient) { }
  public async checkDueNotifications(userId: string): Promise<void> {
    const [medicines, appointments, existing] = await Promise.all([
      firstValueFrom(this.http.get<Medicine[]>(`${this.apiUrl}/medicines`)),
      firstValueFrom(this.http.get<Appointment[]>(`${this.apiUrl}/appointments`)),
      firstValueFrom(this.http.get<Notification[]>(`${this.apiUrl}/notifications`)),
    ]);

    const userMedicines = medicines.filter((m) => String(m.userId) === String(userId));
    const userAppointments = appointments.filter((a) => String(a.userId) === String(userId));
    const userExisting = existing.filter((n) => String(n.userId) === String(userId));

    const now = new Date();
    const todayStr = dayjs().format('YYYY-MM-DD');
    let created = false;

    // --- Medicine doses due today ---
    for (const medicine of userMedicines) {
      if (medicine.startDate > todayStr || medicine.endDate < todayStr) continue;

      for (const [doseIndex, time] of medicine.times.entries()) {
        const [hours, minutes] = time.split(':').map(Number);
        const doseTime = dayjs().hour(hours).minute(minutes).second(0).toDate();

        if (doseTime > now) continue; // not due yet

        const alreadyNotified = userExisting.some(
          (n) =>
            n.type === 'medicine' &&
            n.referenceId === medicine.id &&
            n.doseIndex === doseIndex &&
            dayjs(n.createdAt).format('YYYY-MM-DD') === todayStr,
        );
        if (alreadyNotified) continue;

        const doseLabel = `Dose ${doseIndex + 1} of ${medicine.times.length}`;
        const mealText =
          medicine.mealPreference === 'before'
            ? 'Before meal'
            : medicine.mealPreference === 'after'
              ? 'After meal'
              : 'Any time';

        await this.saveInAppNotification({
          body: `${doseLabel} · ${mealText}`,
          createdAt: new Date().toISOString(),
          doseIndex,
          read: false,
          referenceId: medicine.id,
          title: `Time to take your ${medicine.name} (${medicine.dosage})`,
          type: 'medicine',
          userId,
        });
        created = true;
      }
    }

    // --- Appointment reminders ---
    for (const appointment of userAppointments) {
      if (appointment.visited) continue;

      const appointmentDateTime = this.parseAppointmentDateTime(appointment.date, appointment.time);
      if (!appointmentDateTime || appointmentDateTime <= now) continue;

      const label = appointment.isFollowUp ? `${appointment.title} follow-up` : appointment.title;
      const twoDaysBefore = new Date(appointmentDateTime.getTime() - 48 * 60 * 60 * 1000);
      const threeHoursBefore = new Date(appointmentDateTime.getTime() - 3 * 60 * 60 * 1000);

      if (twoDaysBefore <= now) {
        const already = userExisting.some(
          (n) => n.type === 'appointment' && n.referenceId === appointment.id && n.title.includes('Upcoming'),
        );
        if (!already) {
          await this.saveInAppNotification({
            body: `Your ${label} is in 2 days.`,
            createdAt: new Date().toISOString(),
            read: false,
            referenceId: appointment.id,
            title: appointment.isFollowUp ? 'Upcoming Follow-up' : 'Upcoming Appointment',
            type: 'appointment',
            userId,
          });
          created = true;
        }
      }

      if (threeHoursBefore <= now) {
        const already = userExisting.some(
          (n) => n.type === 'appointment' && n.referenceId === appointment.id && n.title.includes('Today'),
        );
        if (!already) {
          const timeStr = dayjs(appointmentDateTime).format('h:mm A');
          await this.saveInAppNotification({
            body: `Your ${label} is today at ${timeStr}.`,
            createdAt: new Date().toISOString(),
            read: false,
            referenceId: appointment.id,
            title: appointment.isFollowUp ? 'Follow-up Today' : 'Appointment Today',
            type: 'appointment',
            userId,
          });
          created = true;
        }
      }
    }

    if (created) this._newNotification$.next();
  }

  public async requestPermission(): Promise<void> {
    const { display } = await LocalNotifications.requestPermissions();
    if (display !== 'granted') return;

    await LocalNotifications.registerActionTypes({
      types: [
        {
          id: 'MEDICINE_ACTIONS',
          actions: [
            { id: 'TAKE', title: 'Take' },
            { id: 'SNOOZE', title: 'Snooze 15 min' },
          ],
        },
      ],
    });
  }

  public async scheduleAll(userId: string): Promise<void> {
    const pending = await LocalNotifications.getPending();
    if (pending.notifications.length) {
      await LocalNotifications.cancel({ notifications: pending.notifications });
    }

    const [medicines, appointments] = await Promise.all([
      firstValueFrom(this.http.get<Medicine[]>(`${this.apiUrl}/medicines`)),
      firstValueFrom(
        this.http.get<Appointment[]>(`${this.apiUrl}/appointments`),
      ),
    ]);

    const userMedicines = medicines.filter(
      (m) => String(m.userId) === String(userId),
    );
    const userAppointments = appointments.filter(
      (a) => String(a.userId) === String(userId),
    );

    const notifications: LocalNotificationSchema[] = [
      ...this.buildMedicineNotifications(userMedicines),
      ...this.buildAppointmentNotifications(userAppointments),
    ];

    if (!notifications.length) return;

    const options: ScheduleOptions = { notifications };
    await LocalNotifications.schedule(options);
  }

  public listenForReceived(userId: string): void {
    LocalNotifications.addListener(
      'localNotificationReceived',
      async (notification) => {
        const extra = notification.extra as NotificationExtra | undefined;
        if (!extra) return;

        await this.saveInAppNotification({
          body: notification.body ?? '',
          createdAt: new Date().toISOString(),
          doseIndex: extra.doseIndex,
          read: false,
          referenceId: extra.referenceId,
          title: notification.title ?? '',
          type: extra.type,
          userId,
        });

        this._newNotification$.next();
      },
    );
  }

  public listenForActions(userId: string): void {
    LocalNotifications.addListener(
      'localNotificationActionPerformed',
      async (action: ActionPerformed) => {
        const extra = action.notification.extra as
          | NotificationExtra
          | undefined;
        if (!extra) return;

        if (extra.type === 'appointment' && action.actionId === 'tap') {
          await this.saveInAppNotification({
            body: action.notification.body ?? '',
            createdAt: new Date().toISOString(),
            read: false,
            referenceId: extra.referenceId,
            title: action.notification.title ?? '',
            type: 'appointment',
            userId,
          });

          this._newNotification$.next();
          return;
        }

        if (extra.type === 'medicine') {
          if (action.actionId === 'TAKE') {
            await this.handleTakeAction(extra, userId);
          } else if (action.actionId === 'SNOOZE') {
            await this.handleSnoozeAction(action.notification);
          }
        }
      },
    );
  }

  public async getInAppNotifications(userId: string): Promise<Notification[]> {
    return await firstValueFrom(
      this.http.get<Notification[]>(
        `${this.apiUrl}/notifications/in-app/${userId}`
      )
    );
  }

  public async saveInAppNotification(
    notification: Omit<Notification, 'id'>,
  ): Promise<void> {
    const existing = await this.getInAppNotifications(notification.userId);

    await firstValueFrom(
      this.http.post<Notification>(`${this.apiUrl}/notifications`, {
        ...notification,
        id: `notif-${Date.now()}`,
      }),
    );

    if (existing.length >= this.maxInApp) {
      const toDelete = existing.slice(this.maxInApp - 1);
      await Promise.all(
        toDelete.map((n) =>
          firstValueFrom(
            this.http.delete(`${this.apiUrl}/notifications/${n.id}`),
          ),
        ),
      );
    }
  }

  public async markAllAsRead(userId: string): Promise<void> {
    await firstValueFrom(
      this.http.patch(
        `${this.apiUrl}/notifications/read-all/${userId}`,
        {}
      )
    );
  }

  private buildMedicineNotifications(
    medicines: Medicine[],
  ): LocalNotificationSchema[] {
    const notifications: LocalNotificationSchema[] = [];
    const today = dayjs().startOf('day');
    const rangeEnd = today.add(30, 'day');
    let idCounter = 1000;

    for (const medicine of medicines) {
      const start = dayjs(medicine.startDate);
      const end = dayjs(medicine.endDate);

      const scheduleStart = start.isBefore(today) ? today : start;
      const scheduleEnd = end.isBefore(rangeEnd) ? end : rangeEnd;

      let current = scheduleStart;

      while (
        current.isBefore(scheduleEnd) ||
        current.isSame(scheduleEnd, 'day')
      ) {
        medicine.times.forEach((time, doseIndex) => {
          const [hours, minutes] = time.split(':').map(Number);
          const scheduledAt = current
            .hour(hours)
            .minute(minutes)
            .second(0)
            .toDate();

          if (scheduledAt <= new Date()) return;

          const doseLabel = `Dose ${doseIndex + 1} of ${medicine.times.length}`;
          const mealText =
            medicine.mealPreference === 'before'
              ? 'Before meal'
              : medicine.mealPreference === 'after'
                ? 'After meal'
                : 'Any time';

          notifications.push({
            actionTypeId: 'MEDICINE_ACTIONS',
            body: `${doseLabel} · ${mealText}`,
            extra: {
              doseIndex,
              medicineId: medicine.id,
              referenceId: medicine.id,
              type: 'medicine',
            } satisfies NotificationExtra,
            id: idCounter++,
            schedule: { at: scheduledAt },
            title: `Time to take your ${medicine.name} (${medicine.dosage})`,
          });
        });

        current = current.add(1, 'day');
      }
    }

    return notifications;
  }

  private buildAppointmentNotifications(
    appointments: Appointment[],
  ): LocalNotificationSchema[] {
    const notifications: LocalNotificationSchema[] = [];
    const now = new Date();
    let idCounter = 5000;

    for (const appointment of appointments) {
      if (appointment.visited) continue;

      const appointmentDateTime = this.parseAppointmentDateTime(
        appointment.date,
        appointment.time,
      );
      if (!appointmentDateTime) continue;

      const label = appointment.isFollowUp
        ? `${appointment.title} follow-up`
        : appointment.title;

      const twoDaysBefore = new Date(appointmentDateTime);
      twoDaysBefore.setTime(
        appointmentDateTime.getTime() - 48 * 60 * 60 * 1000,
      );

      if (twoDaysBefore > now) {
        notifications.push({
          body: `Your ${label} is in 2 days.`,
          extra: {
            referenceId: appointment.id,
            type: 'appointment',
          } satisfies NotificationExtra,
          id: idCounter++,
          schedule: { at: twoDaysBefore },
          title: appointment.isFollowUp
            ? 'Upcoming Follow-up'
            : 'Upcoming Appointment',
        });
      }

      const threeHoursBefore = new Date(appointmentDateTime);
      threeHoursBefore.setTime(
        appointmentDateTime.getTime() - 3 * 60 * 60 * 1000,
      );

      if (threeHoursBefore > now) {
        const timeStr = dayjs(appointmentDateTime).format('h:mm A');
        notifications.push({
          body: `Your ${label} is today at ${timeStr}.`,
          extra: {
            referenceId: appointment.id,
            type: 'appointment',
          } satisfies NotificationExtra,
          id: idCounter++,
          schedule: { at: threeHoursBefore },
          title: appointment.isFollowUp
            ? 'Follow-up Today'
            : 'Appointment Today',
        });
      }
    }

    return notifications;
  }

  private parseAppointmentDateTime(date: string, time: string): Date | null {
    try {
      const combined = `${date} ${time}`;
      const parsed = dayjs(combined, 'YYYY-MM-DD hh:mm A');
      if (!parsed.isValid()) return null;
      return parsed.toDate();
    } catch {
      return null;
    }
  }

  private async handleTakeAction(
    extra: NotificationExtra,
    userId: string,
  ): Promise<void> {
    if (!extra.medicineId) return;

    const medicine = await firstValueFrom(
      this.http.get<Medicine>(`${this.apiUrl}/medicines/${extra.medicineId}`),
    );

    const today = new Date().toISOString().split('T')[0];
    const doseIndex = extra.doseIndex ?? 0;
    const updated = [...(medicine.lastTakenDates ?? [])];
    updated[doseIndex] = today;

    await firstValueFrom(
      this.http.patch(`${this.apiUrl}/medicines/${extra.medicineId}`, {
        lastTakenDates: updated,
      }),
    );

    const doseLabel = `Dose ${doseIndex + 1} of ${medicine.times.length}`;

    await this.saveInAppNotification({
      body: `${doseLabel} marked as taken.`,
      createdAt: new Date().toISOString(),
      doseIndex,
      read: false,
      referenceId: extra.medicineId,
      title: `${medicine.name} (${medicine.dosage})`,
      type: 'medicine',
      userId,
    });

    this._newNotification$.next();
  }

  private async handleSnoozeAction(
    notification: LocalNotificationSchema,
  ): Promise<void> {
    const snoozeAt = new Date();
    snoozeAt.setMinutes(snoozeAt.getMinutes() + 15);

    await LocalNotifications.schedule({
      notifications: [
        {
          ...notification,
          id: notification.id + 10000,
          schedule: { at: snoozeAt },
        },
      ],
    });
  }
}
