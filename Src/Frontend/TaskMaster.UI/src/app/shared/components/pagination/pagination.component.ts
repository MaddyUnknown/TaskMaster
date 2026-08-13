import { Component, EventEmitter, Input, Output } from '@angular/core';
import {
  LucideChevronLeft,
  LucideChevronRight,
  LucideChevronsLeft,
  LucideChevronsRight,
} from '@lucide/angular';

@Component({
  selector: 'app-pagination',
  standalone: true,
  imports: [
    LucideChevronLeft,
    LucideChevronRight,
    LucideChevronsLeft,
    LucideChevronsRight,
  ],
  templateUrl: './pagination.component.html',
  styleUrl: './pagination.component.css',
})
export class PaginationComponent {
  @Input() page = 1;
  @Input() totalPages = 0;
  @Output() pageChange = new EventEmitter<number>();

  get pages(): (number | null)[] {
    const total = this.totalPages;
    if (total <= 1) return [1];
    if (total <= 7) return this.range(1, total);

    const current = this.page;
    const pages: (number | null)[] = [1];

    if (current > 4) pages.push(null);

    const start = Math.max(2, current - 1);
    const end = Math.min(total - 1, current + 1);
    for (let p = start; p <= end; p++) pages.push(p);

    if (current < total - 3) pages.push(null);

    pages.push(total);
    return pages;
  }

  get hasPrevious(): boolean {
    return this.page > 1;
  }

  get hasNext(): boolean {
    return this.page < this.totalPages;
  }

  goToPage(p: number) {
    if (Number.isNaN(p) || this.totalPages <= 0) return;
    const target = Math.min(Math.max(p, 1), this.totalPages);
    if (target !== this.page) this.pageChange.emit(target);
  }

  goToInput(value: string) {
    this.goToPage(parseInt(value, 10));
  }

  first() {
    this.goToPage(1);
  }

  previous() {
    this.goToPage(this.page - 1);
  }

  next() {
    this.goToPage(this.page + 1);
  }

  last() {
    this.goToPage(this.totalPages);
  }

  private range(from: number, to: number): number[] {
    const arr: number[] = [];
    for (let i = from; i <= to; i++) arr.push(i);
    return arr;
  }
}
