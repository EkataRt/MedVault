import { inject, NgModule } from '@angular/core';
import { PreloadAllModules, RouterModule, Routes } from '@angular/router';
import { Router } from '@angular/router';
import { map } from 'rxjs/operators';

import { AuthGuard } from './shared/service/guards/auth-guard';
import { StorageService } from './shared/service/storage/storage-service';

const smartRedirect = () => {
  const storage = inject(StorageService);
  const router = inject(Router);

  const syncUser = storage.get('active_user');

  if (syncUser) {
    const hasRestoredPhoto = !!localStorage.getItem('restored_photo_uri');

    if (hasRestoredPhoto) {
      return router.createUrlTree(['/main/document-vault']);
    }

    return router.createUrlTree(['/main']);
  }

  return storage.getAsync('active_user').pipe(
    map((value) => {
      if (value) {
        localStorage.setItem('active_user', value);

        const hasRestoredPhoto = !!localStorage.getItem('restored_photo_uri');

        if (hasRestoredPhoto) {
          return router.createUrlTree(['/main/document-vault']);
        }

        return router.createUrlTree(['/main']);
      }

      return router.createUrlTree(['/home/login']);
    }),
  );
};

const routes: Routes = [
  {
    path: '',
    canActivate: [() => smartRedirect()],
    children: [],
  },
  {
    path: 'home',
    loadChildren: () =>
      import('./modules/home/home.module').then((m) => m.HomePageModule),
  },
  {
    path: 'main',
    loadChildren: () =>
      import('./modules/main/main.module').then((m) => m.MainPageModule),
    canActivate: [AuthGuard],
  },
  {
    path: 'onboarding',
    loadChildren: () =>
      import('./modules/onboarding/onboarding.module').then(
        (m) => m.OnboardingModule,
      ),
    canActivate: [AuthGuard],
  },
  {
    path: '**',
    canActivate: [() => smartRedirect()],
    children: [],
  },
];

@NgModule({
  imports: [
    RouterModule.forRoot(routes, { preloadingStrategy: PreloadAllModules }),
  ],
  exports: [RouterModule],
})
export class AppRoutingModule {}
