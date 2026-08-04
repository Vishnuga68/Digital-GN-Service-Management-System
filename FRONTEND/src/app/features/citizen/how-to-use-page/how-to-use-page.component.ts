import { Component, inject } from '@angular/core';
import { RouterModule, Router } from '@angular/router';

@Component({
  selector: 'app-how-to-use-page',
  standalone: true,
  imports: [RouterModule],
  templateUrl: './how-to-use-page.component.html',
  styleUrl: './how-to-use-page.component.scss'
})
export class HowToUsePageComponent {
  private router = inject(Router);

  scrollTo(event: Event, targetId: string) {
    event.preventDefault();
    const element = document.getElementById(targetId);
    if (element) {
      element.scrollIntoView({ behavior: 'smooth', block: 'start' });
      
      // Update URL hash without jumping
      window.history.pushState(null, '', `#${targetId}`);
    }
  }

  goBack() {
    this.router.navigate(['/citizen']);
  }
}
