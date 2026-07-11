import { Component, Output, EventEmitter } from '@angular/core';
import { NgComponentOutlet } from '@angular/common';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { NavigationService } from '../../../core/services/navigation.service';
import {
  LucideLayoutGrid,
  LucideChevronLeft,
} from '@lucide/angular';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [
    NgComponentOutlet,
    RouterLink,
    RouterLinkActive,
    LucideLayoutGrid,
    LucideChevronLeft,
  ],
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.css',
})
export class SidebarComponent {
  collapsed = false;

  @Output() collapsedChange = new EventEmitter<boolean>();

  get navItems() {
    return this.navService.items;
  }

  constructor(public navService: NavigationService) {}

  toggle() {
    this.collapsed = !this.collapsed;
    this.collapsedChange.emit(this.collapsed);
  }
}
