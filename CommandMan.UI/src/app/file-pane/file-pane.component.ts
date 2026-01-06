import { Component, Input, Output, EventEmitter, OnInit, OnDestroy, ViewChild } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ScrollingModule, CdkVirtualScrollViewport } from '@angular/cdk/scrolling';
import { BridgeService, FileSystemItem, DriveItem } from '../services/bridge.service';
import { BreadcrumbComponent } from '../breadcrumb/breadcrumb.component';
import { Subject, takeUntil } from 'rxjs';

@Component({
    selector: 'app-file-pane',
    standalone: true,
    imports: [CommonModule, ScrollingModule, BreadcrumbComponent],
    templateUrl: './file-pane.component.html',
    styleUrls: ['./file-pane.component.scss']
})
export class FilePaneComponent implements OnInit, OnDestroy {
    @ViewChild(CdkVirtualScrollViewport) viewport!: CdkVirtualScrollViewport;
    @Input() paneId: 'left' | 'right' = 'left';
    @Input() isActive = false;
    @Output() activated = new EventEmitter<'left' | 'right'>();
    @Output() renameRequested = new EventEmitter<FileSystemItem>();

    items: FileSystemItem[] = [];
    currentPath = '';
    drives: DriveItem[] = [];
    selectedIndex = 0;

    private destroy$ = new Subject<void>();

    constructor(private bridgeService: BridgeService) { }

    ngOnInit(): void {
        // Subscribe to the correct pane's state
        const paneState$ = this.paneId === 'left'
            ? this.bridgeService.leftPane
            : this.bridgeService.rightPane;

        paneState$
            .pipe(takeUntil(this.destroy$))
            .subscribe(state => {
                const prevPath = this.currentPath;
                this.items = state.items;
                this.currentPath = state.currentPath;
                this.selectedIndex = 0;

                // Save state if path changed and we have a path
                if (this.currentPath && this.currentPath !== prevPath) {
                    this.saveCurrentState();
                }

                // Handle focus/selection
                if (state.focusItem) {
                    const focusIndex = this.items.findIndex(i => i.Name === state.focusItem);
                    if (focusIndex !== -1) {
                        this.selectedIndex = focusIndex;
                        setTimeout(() => {
                            if (this.viewport) {
                                this.viewport.scrollToIndex(focusIndex);
                            }
                        });
                    }
                }
            });

        // Drives are shared between panes
        this.bridgeService.drives
            .pipe(takeUntil(this.destroy$))
            .subscribe(drives => {
                this.drives = drives;
            });
    }

    ngOnDestroy(): void {
        this.destroy$.next();
        this.destroy$.complete();
    }

    onPaneClick(): void {
        this.activated.emit(this.paneId);
    }

    onDriveSelect(drive: DriveItem): void {
        this.activated.emit(this.paneId);
        this.bridgeService.getDirectoryContents(drive.Name, this.paneId);
    }

    onBreadcrumbNavigate(path: string): void {
        this.bridgeService.getDirectoryContents(path, this.paneId);
    }

    onItemClick(item: FileSystemItem, index: number): void {
        this.selectedIndex = index;
        this.activated.emit(this.paneId);
    }

    onItemDoubleClick(item: FileSystemItem): void {
        if (item.IsDirectory) {
            this.bridgeService.getDirectoryContents(item.Path, this.paneId);
        } else {
            this.bridgeService.openPath(item.Path);
        }
    }

    onKeyDown(event: KeyboardEvent): void {
        const item = this.items[this.selectedIndex];
        switch (event.key) {
            case 'ArrowUp':
                event.preventDefault();
                if (this.selectedIndex > 0) {
                    this.selectedIndex--;
                }
                break;
            case 'ArrowDown':
                event.preventDefault();
                if (this.selectedIndex < this.items.length - 1) {
                    this.selectedIndex++;
                }
                break;
            case 'Enter':
                event.preventDefault();
                if (item?.Name === '..') {
                    this.bridgeService.getDirectoryContents(item.Path, this.paneId);
                } else if (item?.IsDirectory) {
                    this.bridgeService.getDirectoryContents(item.Path, this.paneId);
                } else if (item) {
                    this.bridgeService.openPath(item.Path);
                }
                break;
            case 'F8':
            case 'Delete':
                event.preventDefault();
                if (item && item.Name !== '..') {
                    if (confirm(`Are you sure you want to delete ${item.Name}?`)) {
                        this.bridgeService.deleteItem(item.Path, this.paneId);
                    }
                }
                break;
            case 'F6':
                if (event.shiftKey) {
                    event.preventDefault();
                    if (item && item.Name !== '..') {
                        this.requestRename(item);
                    }
                }
                break;
        }
    }

    private requestRename(item: FileSystemItem): void {
        // We'll emit an event to AppComponent to show the rename dialog
        // or just handle it here if we bring InputDialog in.
        // For now, let's use a simple prompt or emit.
        // I'll add an @Output for rename requested.
        this.renameRequested.emit(item);
    }

    formatSize(bytes: number): string {
        if (bytes === 0) return '';
        const k = 1024;
        const sizes = ['B', 'KB', 'MB', 'GB', 'TB'];
        const i = Math.floor(Math.log(bytes) / Math.log(k));
        return parseFloat((bytes / Math.pow(k, i)).toFixed(1)) + ' ' + sizes[i];
    }

    formatDate(dateString: string): string {
        if (!dateString) return '';
        const date = new Date(dateString);
        return date.toLocaleDateString() + ' ' + date.toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
    }

    getFileIcon(item: FileSystemItem): string {
        if (item.Name === '..') return '⬆️';
        if (item.IsDirectory) return '📁';

        const ext = item.Extension?.toLowerCase() || '';
        switch (ext) {
            case '.txt': return '📄';
            case '.pdf': return '📕';
            case '.doc':
            case '.docx': return '📘';
            case '.xls':
            case '.xlsx': return '📗';
            case '.jpg':
            case '.jpeg':
            case '.png':
            case '.gif': return '🖼️';
            case '.mp3':
            case '.wav': return '🎵';
            case '.mp4':
            case '.avi':
            case '.mkv': return '🎬';
            case '.zip':
            case '.rar':
            case '.7z': return '📦';
            case '.exe': return '⚙️';
            default: return '📄';
        }
    }

    private saveCurrentState(): void {
        this.bridgeService.updatePanePath(this.paneId, this.currentPath);
    }
}
