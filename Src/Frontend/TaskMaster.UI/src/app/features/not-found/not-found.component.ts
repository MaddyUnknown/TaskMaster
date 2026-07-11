import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { LucideArrowLeft } from '@lucide/angular';
import { NavigationService } from '../../core/services/navigation.service';

@Component({
  selector: 'app-not-found',
  standalone: true,
  imports: [RouterLink, LucideArrowLeft],
  templateUrl: './not-found.component.html',
  styleUrl: './not-found.component.css'
})
export class NotFoundComponent {
  nav = inject(NavigationService);
}
