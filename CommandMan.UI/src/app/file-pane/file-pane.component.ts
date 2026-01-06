import { Component, Input, Output, EventEmitter, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ScrollingModule } from '@angular/cdk/scrolling';
import { BridgeService, FileSystemItem, DriveItem } from '../services/bridge.service';
import { Subject, takeUntil } from 'rxjs';

@Component({
    selector: 'app-file-pane',
    standalone: true,
    imports: [CommonModule, ScrollingModule],
    templateUrl: './file-pane.component.html',
    styleUrls: ['./file-pane.component.scss']
})
export class FilePaneComponent implements OnInit, OnDestroy {
    @Input() paneId: 'left' | 'right' = 'left';
    @Input() isActive = false;
    @Output() activated = new EventEmitter<'left' | 'right'>();

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

    onItemClick(item: FileSystemItem, index: number): void {
        this.selectedIndex = index;
        this.activated.emit(this.paneId);
    }

    onItemDoubleClick(item: FileSystemItem): void {
        if (item.IsDirectory) {
            this.bridgeService.getDirectoryContents(item.Path, this.paneId);
        }
    }

    onKeyDown(event: KeyboardEvent): void {
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
                const item = this.items[this.selectedIndex];
                if (item?.IsDirectory) {
                    this.bridgeService.getDirectoryContents(item.Path, this.paneId);
                }
                break;
        }
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
