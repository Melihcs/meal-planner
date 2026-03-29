import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import {
  IonIcon,
  IonLabel,
  IonTabBar,
  IonTabButton,
  IonTabs,
} from '@ionic/angular/standalone';
import { addIcons } from 'ionicons';
import {
  bookOutline,
  calendarClearOutline,
  personCircleOutline,
  timeOutline,
} from 'ionicons/icons';

@Component({
  standalone: true,
  selector: 'app-tabs-shell',
  imports: [IonIcon, IonLabel, IonTabBar, IonTabButton, IonTabs, RouterLink],
  templateUrl: './tabs-shell.component.html',
})
export class TabsShellComponent {
  constructor() {
    addIcons({
      bookOutline,
      calendarClearOutline,
      personCircleOutline,
      timeOutline,
    });
  }
}
