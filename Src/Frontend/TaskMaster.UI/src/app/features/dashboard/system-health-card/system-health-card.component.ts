import { Component, Input } from '@angular/core';
import { LucideServer, LucideDatabase, LucideUsers } from '@lucide/angular';
import { CardComponent } from '../../../shared/components/card/card.component';
import { SystemHealth } from '../../../core/models';

@Component({
  selector: 'app-system-health-card',
  standalone: true,
  imports: [CardComponent, LucideServer, LucideDatabase, LucideUsers],
  templateUrl: './system-health-card.component.html',
  styleUrl: './system-health-card.component.css',
})
export class SystemHealthCardComponent {
  @Input() health: SystemHealth | null = null;
  @Input() loading = false;

  healthColor(status: string): string {
    switch (status?.toLowerCase()) {
      case 'healthy': return 'var(--color-success)';
      case 'degraded': return 'var(--color-warning)';
      default: return 'var(--color-error)';
    }
  }

  workersStatus(): string {
    return this.health?.workers?.status ?? 'Unknown';
  }

  workersColor(): string {
    return this.healthColor(this.health?.workers?.status ?? 'Healthy');
  }
}
