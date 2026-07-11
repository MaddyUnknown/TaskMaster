import { Component, Input } from '@angular/core';
import { DatePipe } from '@angular/common';
import { CardComponent } from '../../../shared/components/card/card.component';
import { TooltipComponent } from '../../../shared/components/tooltip/tooltip.component';

export interface ChartDataPoint {
  value: number;
  height: number;
  label: string;
  start: string;
  end: string;
}

@Component({
  selector: 'app-jobs-per-hour-chart',
  standalone: true,
  imports: [DatePipe, CardComponent, TooltipComponent],
  templateUrl: './jobs-per-hour-chart.component.html',
  styleUrl: './jobs-per-hour-chart.component.css',
})
export class JobsPerHourChartComponent {
  @Input() data: ChartDataPoint[] = [];
  @Input() loading = false;
  hoveredIndex: number | null = null;
}
