import { Component, ViewChild, HostListener, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FilePaneComponent } from './file-pane/file-pane.component';
import { AboutDialogComponent } from './about-dialog/about-dialog.component';
import { InputDialogComponent } from './input-dialog/input-dialog.component';
import { ProgressOverlayComponent } from './progress-overlay/progress-overlay.component';
import { BridgeService } from './services/bridge.service';
import { ProgressService } from './services/progress.service';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FilePaneComponent, AboutDialogComponent, InputDialogComponent, ProgressOverlayComponent],
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.scss']
})
export class AppComponent implements AfterViewInit {
  @ViewChild('leftPane') leftPane!: FilePaneComponent;
  @ViewChild('rightPane') rightPane!: FilePaneComponent;

  activePane: 'left' | 'right' = 'left';
  showAbout = false;
  showNewFolder = false;
  itemToRename: any = null;

  ngAfterViewInit(): void {
  }

  constructor(private bridgeService: BridgeService, private progressService: ProgressService) {
    this.bridgeService.error.subscribe(msg => {
      this.progressService.clearProgress();
      alert(msg);
    });
  }

  onPaneActivated(pane: 'left' | 'right'): void {
    this.activePane = pane;
  }

  @HostListener('window:keydown', ['$event'])
  onKeyDown(event: KeyboardEvent): void {
    // Only handle global shortcuts if no dialog is open or if it's Escape
    if (this.showAbout || this.showNewFolder || this.itemToRename) {
      if (event.key === 'Escape') {
        event.preventDefault();
        this.showAbout = false;
        this.showNewFolder = false;
        this.itemToRename = null;
      }
      return;
    }

    if (event.key === 'Tab') {
      event.preventDefault();
      this.activePane = this.activePane === 'left' ? 'right' : 'left';
    } else if (event.key === 'F1') {
      event.preventDefault();
      this.showAbout = true;
    } else if (event.key === 'F7') {
      event.preventDefault();
      this.showNewFolder = true;
    } else if (event.key === 'F5') {
      event.preventDefault();
      this.copySelectedItems();
    } else if (event.key === 'F6') {
      if (!event.shiftKey) {
        event.preventDefault();
        this.moveSelectedItems();
      }
    } else if (event.key === 'F8' || event.key === 'Delete') {
      // Deletion is often handled by the pane, but if we want it global:
      // However, Delete is used in input fields too.
      // So we only handle it if no input is focused.
      const activeElement = document.activeElement;
      if (activeElement?.tagName !== 'INPUT' && activeElement?.tagName !== 'TEXTAREA') {
        event.preventDefault();
        this.deleteSelectedItems();
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

  onRenameRequested(item: any): void {
    this.itemToRename = item;
  }

  renameItem(newName: string): void {
    if (this.itemToRename) {
      this.bridgeService.renameItem(this.itemToRename.Path, newName, this.activePane);
      this.itemToRename = null;
    }
  }

  public copySelectedItems(): void {
    const activePaneInstance = this.activePane === 'left' ? this.leftPane : this.rightPane;
    if (!activePaneInstance) return;

    const items = activePaneInstance.getSelectedItems();
    if (items.length === 0) {
      alert('Please select a file or folder to copy first.');
      return;
    }

    const targetPaneId = this.activePane === 'left' ? 'right' : 'left';
    const targetPath = this.bridgeService.getCurrentPath(targetPaneId);

    if (confirm(`Copy ${items.length} item(s) to ${targetPath}?`)) {
      this.bridgeService.copyItems(items.map(i => i.Path), targetPath, this.activePane);
    }
  }

  public moveSelectedItems(): void {
    const activePaneInstance = this.activePane === 'left' ? this.leftPane : this.rightPane;
    if (!activePaneInstance) return;

    const items = activePaneInstance.getSelectedItems();
    if (items.length === 0) {
      alert('Please select a file or folder to move first.');
      return;
    }

    const targetPaneId = this.activePane === 'left' ? 'right' : 'left';
    const targetPath = this.bridgeService.getCurrentPath(targetPaneId);

    if (confirm(`Move ${items.length} item(s) to ${targetPath}?`)) {
      this.bridgeService.moveItems(items.map(i => i.Path), targetPath, this.activePane);
    }
  }

  public deleteSelectedItems(): void {
    const activePaneInstance = this.activePane === 'left' ? this.leftPane : this.rightPane;
    if (!activePaneInstance) return;

    const items = activePaneInstance.getSelectedItems();
    if (items.length === 0) {
      alert('Please select a file or folder to delete first.');
      return;
    }

    if (confirm(`Delete ${items.length} item(s)?`)) {
      items.forEach(item => {
        this.bridgeService.deleteItem(item.Path, this.activePane);
      });
    }
  }
}
