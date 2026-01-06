import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FilePaneComponent } from './file-pane/file-pane.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FilePaneComponent],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent {
  activePane: 'left' | 'right' = 'left';

  onPaneActivated(pane: 'left' | 'right'): void {
    this.activePane = pane;
  }

  onKeyDown(event: KeyboardEvent): void {
    if (event.key === 'Tab') {
      event.preventDefault();
      this.activePane = this.activePane === 'left' ? 'right' : 'left';
    }
  }
}
