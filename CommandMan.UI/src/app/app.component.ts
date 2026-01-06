import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FilePaneComponent } from './file-pane/file-pane.component';
import { AboutDialogComponent } from './about-dialog/about-dialog.component';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FilePaneComponent, AboutDialogComponent],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent {
  activePane: 'left' | 'right' = 'left';
  showAbout = false;

  onPaneActivated(pane: 'left' | 'right'): void {
    this.activePane = pane;
  }

  onKeyDown(event: KeyboardEvent): void {
    if (event.key === 'Tab') {
      event.preventDefault();
      this.activePane = this.activePane === 'left' ? 'right' : 'left';
    } else if (event.key === 'F1') {
      event.preventDefault();
      this.showAbout = true;
    } else if (event.key === 'Escape' && this.showAbout) {
      event.preventDefault();
      this.showAbout = false;
    }
  }
}
