import { enableProdMode } from '@angular/core';
import { platformBrowserDynamic } from '@angular/platform-browser-dynamic';
import { App } from '@capacitor/app';
import { Preferences } from '@capacitor/preferences';
import { defineCustomElements } from '@ionic/pwa-elements/loader';
import { AppModule } from './app/app.module';
import { environment } from './environments/environment';

App.addListener('appRestoredResult', async (result) => {
  if (!result.success || !result.data) return;

  const base64 = result.data.base64String as string | undefined;
  if (!base64) return;

  await Preferences.set({ key: 'pending_photo_base64', value: base64 });
  await Preferences.set({
    key: 'pending_photo_format',
    value: result.data.format ?? 'jpeg',
  });
});

if (environment.production) {
  enableProdMode();
}

platformBrowserDynamic()
  .bootstrapModule(AppModule)
  .catch((err) => console.error(err));

defineCustomElements(window);
