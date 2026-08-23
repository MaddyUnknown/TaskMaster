import { Component, OnInit, inject } from '@angular/core';
import { RouterOutlet, Router, NavigationEnd } from '@angular/router';
import { filter } from 'rxjs';
import { SidebarComponent } from '../sidebar/sidebar.component';
import { HeaderComponent } from '../header/header.component';
import { ToastComponent } from '../../components/notification/toast.component';
import { routeAnimations } from '../../animations/route.animations';

@Component({
  selector: 'app-layout',
  standalone: true,
  imports: [RouterOutlet, SidebarComponent, HeaderComponent, ToastComponent],
  templateUrl: './layout.component.html',
  animations: [routeAnimations],
  styleUrl: './layout.component.css',
})
export class LayoutComponent implements OnInit {
  private router = inject(Router);
  sidebarCollapsed = false;
  routeState = '';

  ngOnInit(): void {
    this.router.events
      .pipe(filter((event) => event instanceof NavigationEnd))
      .subscribe((event) => (this.routeState = (event as NavigationEnd).url));
  }

  onCollapsedChange(collapsed: boolean) {
    this.sidebarCollapsed = collapsed;
  }
}
