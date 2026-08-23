import { AfterViewInit, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet],
  templateUrl: './app.component.html',
  styleUrl: './app.component.css',
})
export class AppComponent implements AfterViewInit {
  ngAfterViewInit(): void {
    const loader = document.getElementById('initial-loader');

    if (loader) {
      requestAnimationFrame(() => {
        loader.classList.add('loaded');

        setTimeout(() => {
          loader.remove();
        }, 400);
      });
    }
  }
}
