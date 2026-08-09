import { CommonModule } from '@angular/common';
import { NgModule } from '@angular/core';
import { ReactiveFormsModule } from '@angular/forms';
import { IonicModule } from '@ionic/angular';
import { StepAllergiesComponent } from './components/step-allergies/step-allergies.component';
import { StepBloodTypeComponent } from './components/step-blood-type/step-blood-type.component';
import { StepCheckupComponent } from './components/step-checkup/step-checkup.component';
import { StepDobComponent } from './components/step-dob/step-dob.component';
import { StepHeightComponent } from './components/step-height/step-height.component';
import { StepNameComponent } from './components/step-name/step-name.component';
import { StepSexComponent } from './components/step-sex/step-sex.component';
import { StepWeightComponent } from './components/step-weight/step-weight.component';
import { OnboardingRoutingModule } from './onboarding-routing.module';
import { OnboardingPage } from './onboarding.page';

@NgModule({
  declarations: [
    OnboardingPage,
    StepAllergiesComponent,
    StepBloodTypeComponent,
    StepCheckupComponent,
    StepDobComponent,
    StepHeightComponent,
    StepNameComponent,
    StepSexComponent,
    StepWeightComponent,
  ],
  imports: [
    CommonModule,
    IonicModule,
    OnboardingRoutingModule,
    ReactiveFormsModule,
  ],
})
export class OnboardingModule {}
