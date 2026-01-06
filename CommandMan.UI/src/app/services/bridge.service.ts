import { Injectable, NgZone } from '@angular/core';
import { BehaviorSubject, Subject } from 'rxjs';

export interface FileSystemItem {
  Name: string;
  Path: string;
  IsDirectory: boolean;
  Size: number;
  Modified: string;
  Extension?: string;
}

export interface DriveItem {
  Name: string;
  Label: string;
  TotalSize: number;
  FreeSpace: number;
  DriveType: string;
}

export interface BridgeResponse {
  Action: string;
  Data?: FileSystemItem[] | { LeftPath: string, RightPath: string } | { Version: string, AppName: string };
  CurrentPath?: string;
  Error?: string;
  Drives?: DriveItem[];
  PaneId?: string;
  FocusItem?: string;
}

export interface PaneState {
  items: FileSystemItem[];
  currentPath: string;
  focusItem?: string;
}

@Injectable({
  providedIn: 'root'
})
export class BridgeService {
  public leftPane$ = new BehaviorSubject<PaneState>({ items: [], currentPath: '' });
  public rightPane$ = new BehaviorSubject<PaneState>({ items: [], currentPath: '' });
  private drives$ = new BehaviorSubject<DriveItem[]>([]);
  private appInfo$ = new BehaviorSubject<{ Version: string, AppName: string } | null>(null);
  private error$ = new Subject<string>();
  private isWebView2 = false;
  private pendingPaneId: 'left' | 'right' = 'left';

  readonly leftPane = this.leftPane$.asObservable();
  readonly rightPane = this.rightPane$.asObservable();
  readonly drives = this.drives$.asObservable();
  readonly appInfo = this.appInfo$.asObservable();
  readonly error = this.error$.asObservable();

  constructor(private ngZone: NgZone) {
    this.initializeMessageHandler();
  }

  private initializeMessageHandler(): void {
    // Check if running inside WebView2
    if ((window as any).chrome?.webview) {
      this.isWebView2 = true;
      console.log('Running inside WebView2');

      (window as any).chrome.webview.addEventListener('message', (event: any) => {
        this.ngZone.run(() => {
          // Parse the JSON string if it's a string
          const data = typeof event.data === 'string' ? JSON.parse(event.data) : event.data;
          this.handleMessage(data);
        });
      });

      // Request drives after a short delay to ensure WebView2 is ready
      setTimeout(() => {
        this.getDrives();
        this.getAppInfo();
        this.getState();
      }, 100);
    } else {
      console.log('Not running inside WebView2 - using mock data');
      // Load mock drives for browser development
      setTimeout(() => {
        this.loadMockDrives();
        this.appInfo$.next({ Version: '1.0.0-dev', AppName: 'CommandMan (Web)' });
      }, 100);
    }
  }

  private handleMessage(response: BridgeResponse): void {
    console.log('Received message from C#:', response);

    switch (response.Action) {
      case 'directoryContents':
        const state: PaneState = {
          items: (response.Data as FileSystemItem[]) || [],
          currentPath: response.CurrentPath || '',
          focusItem: response.FocusItem
        };

        // Use the returned PaneId to route the response, falling back to pendingPaneId if not present (backward compatibility/mock)
        const targetPane = response.PaneId || this.pendingPaneId;

        if (targetPane === 'left') {
          this.leftPane$.next(state);
        } else if (targetPane === 'right') {
          this.rightPane$.next(state);
        }
        break;

      case 'drives':
        console.log('Received drives:', response.Drives);
        this.drives$.next(response.Drives || []);
        break;

      case 'appInfo':
        this.appInfo$.next(response.Data as { Version: string, AppName: string });
        break;

      case 'error':
        console.error('Bridge error:', response.Error);
        this.error$.next(response.Error || 'Unknown error');
        break;

      case 'state':
        const appState = response.Data as { LeftPath: string, RightPath: string };
        if (appState) {
          // Initialize panes with saved state
          this.getDirectoryContents(appState.LeftPath, 'left');
          this.getDirectoryContents(appState.RightPath, 'right');
        }
        break;
    }
  }

  getDirectoryContents(path: string, paneId: 'left' | 'right'): void {
    // We still set pendingPaneId for backward compatibility or mock data
    this.pendingPaneId = paneId;
    this.postMessage({ Action: 'getDirectoryContents', Path: path, PaneId: paneId });
  }

  getDrives(): void {
    this.postMessage({ Action: 'getDrives' });
  }

  getAppInfo(): void {
    this.postMessage({ Action: 'getAppInfo' });
  }

  createDirectory(path: string, name: string, paneId: 'left' | 'right'): void {
    this.postMessage({ Action: 'createDirectory', Path: path, Name: name, PaneId: paneId });
  }

  openPath(path: string): void {
    this.postMessage({ Action: 'openPath', Path: path });
  }

  deleteItem(path: string, paneId: 'left' | 'right'): void {
    this.postMessage({ Action: 'deleteItem', Path: path, PaneId: paneId });
  }

  renameItem(oldPath: string, newName: string, paneId: 'left' | 'right'): void {
    this.postMessage({ Action: 'renameItem', Path: oldPath, Name: newName, PaneId: paneId });
  }

  copyItems(items: string[], targetPath: string, paneId: 'left' | 'right'): void {
    this.postMessage({ Action: 'copyItems', Items: items, TargetPath: targetPath, PaneId: paneId });
  }

  moveItems(items: string[], targetPath: string, paneId: 'left' | 'right'): void {
    this.postMessage({ Action: 'moveItems', Items: items, TargetPath: targetPath, PaneId: paneId });
  }

  getCurrentPath(paneId: 'left' | 'right'): string {
    return paneId === 'left' ? this.leftPane$.value.currentPath : this.rightPane$.value.currentPath;
  }

  saveState(leftPath: string, rightPath: string): void {
    this.postMessage({
      Action: 'saveState',
      State: { LeftPath: leftPath, RightPath: rightPath }
    });
  }

  updatePanePath(paneId: 'left' | 'right', path: string): void {
    const currentLeft = this.leftPane$.value.currentPath;
    const currentRight = this.rightPane$.value.currentPath;

    if (paneId === 'left') {
      this.saveState(path, currentRight);
    } else {
      this.saveState(currentLeft, path);
    }
  }

  getState(): void {
    this.postMessage({ Action: 'getState' });
  }

  private postMessage(message: any): void {
    if (this.isWebView2) {
      const json = JSON.stringify(message);
      console.log('Sending to C#:', json);
      (window as any).chrome.webview.postMessage(json);
    } else {
      // Mock data for development outside WebView2
      this.mockResponse(message);
    }
  }

  private loadMockDrives(): void {
    // Only show C: drive for mock data (since user only has C:)
    this.drives$.next([
      { Name: 'C:\\', Label: 'System', TotalSize: 500000000000, FreeSpace: 200000000000, DriveType: 'Fixed' }
    ]);
  }

  private mockResponse(message: any): void {
    if (message.Action === 'getDrives') {
      this.loadMockDrives();
    } else if (message.Action === 'getDirectoryContents') {
      const mockItems: FileSystemItem[] = [
        { Name: '..', Path: 'C:\\', IsDirectory: true, Size: 0, Modified: '' },
        { Name: 'Documents', Path: 'C:\\Documents', IsDirectory: true, Size: 0, Modified: '2024-01-01T10:00:00' },
        { Name: 'Downloads', Path: 'C:\\Downloads', IsDirectory: true, Size: 0, Modified: '2024-01-01T10:00:00' },
        { Name: 'readme.txt', Path: 'C:\\readme.txt', IsDirectory: false, Size: 1024, Modified: '2024-01-01T10:00:00', Extension: '.txt' }
      ];

      const state: PaneState = {
        items: mockItems,
        currentPath: message.Path
      };

      if (this.pendingPaneId === 'left') {
        this.leftPane$.next(state);
      } else {
        this.rightPane$.next(state);
      }
    }
  }
}
