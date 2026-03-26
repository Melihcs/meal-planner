import { Component, signal } from '@angular/core';
import { IonApp, IonContent, IonHeader, IonTitle, IonToolbar } from '@ionic/angular/standalone';

@Component({
  selector: 'app-root',
  imports: [IonApp, IonContent, IonHeader, IonTitle, IonToolbar],
  templateUrl: './app.html',
  styleUrl: './app.scss',
})
export class App {
  protected readonly title = signal('Meal Planner');
}
