import { Component, Input, Output, EventEmitter, OnInit, OnDestroy, ViewChild, HostBinding } from '@angular/core';
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
    @Input() @HostBinding('class.active') isActive = false;
    @Output() activated = new EventEmitter<'left' | 'right'>();
    @Output() renameRequested = new EventEmitter<FileSystemItem>();

    items: FileSystemItem[] = [];
    currentPath = '';
    drives: DriveItem[] = [];
    selectedIndex = 0;
    markedIndexes = new Set<number>();

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
                this.markedIndexes.clear();

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
                break;
            case ' ':
            case 'Insert':
                event.preventDefault();
                if (item && item.Name !== '..') {
                    this.toggleMark(this.selectedIndex);
                    if (this.selectedIndex < this.items.length - 1) {
                        this.selectedIndex++;
                    }
                }
                break;
            case 'a':
                if (event.ctrlKey) {
                    event.preventDefault();
                    this.items.forEach((item, index) => {
                        if (item.Name !== '..') this.markedIndexes.add(index);
                    });
                }
                break;
            case 'd':
                if (event.ctrlKey) {
                    event.preventDefault();
                    this.markedIndexes.clear();
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

    private toggleMark(index: number): void {
        if (this.markedIndexes.has(index)) {
            this.markedIndexes.delete(index);
        } else {
            this.markedIndexes.add(index);
        }
    }

    private requestRename(item: FileSystemItem): void {
        // We'll emit an event to AppComponent to show the rename dialog
        // or just handle it here if we bring InputDialog in.
        // For now, let's use a simple prompt or emit.
        // I'll add an @Output for rename requested.
        this.renameRequested.emit(item);
    }

    getSelectedItems(): FileSystemItem[] {
        if (this.markedIndexes.size > 0) {
            return Array.from(this.markedIndexes).map(idx => this.items[idx]);
        }

        if (this.selectedIndex >= 0 && this.selectedIndex < this.items.length) {
            const item = this.items[this.selectedIndex];
            return item.Name !== '..' ? [item] : [];
        }
        return [];
    }

    getSelectionStats(): { count: number, size: number } {
        const selected = this.getSelectedItems();
        return {
            count: selected.length,
            size: selected.reduce((sum, item) => sum + (item.Size || 0), 0)
        };
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
