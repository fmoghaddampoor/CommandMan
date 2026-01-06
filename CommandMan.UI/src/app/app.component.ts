import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FilePaneComponent } from './file-pane/file-pane.component';
import { AboutDialogComponent } from './about-dialog/about-dialog.component';
import { InputDialogComponent } from './input-dialog/input-dialog.component';
import { BridgeService } from './services/bridge.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FilePaneComponent, AboutDialogComponent, InputDialogComponent],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent {
  activePane: 'left' | 'right' = 'left';
  showAbout = false;
  showNewFolder = false;

  constructor(private bridgeService: BridgeService) { }

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
    } else if (event.key === 'F7') {
      event.preventDefault();
      this.showNewFolder = true;
    } else if (event.key === 'Escape') {
      if (this.showAbout) {
        event.preventDefault();
        this.showAbout = false;
      } else if (this.showNewFolder) {
        event.preventDefault();
        this.showNewFolder = false;
      }
    }
  }

  createFolder(name: string): void {
    const currentPath = this.bridgeService.getCurrentPath(this.activePane);
    if (currentPath) {
      this.bridgeService.createDirectory(currentPath, name, this.activePane);
      this.showNewFolder = false;
    }
  }
}
