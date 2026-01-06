import { Injectable, NgZone } from '@angular/core';
import * as signalR from '@microsoft/signalr';
import { BehaviorSubject } from 'rxjs';

export interface ProgressUpdate {
    fileName: string;
    percentage: number;
}

@Injectable({
    providedIn: 'root'
})
export class ProgressService {
    private hubConnection: signalR.HubConnection;
    private currentProgress$ = new BehaviorSubject<ProgressUpdate | null>(null);

    readonly currentProgress = this.currentProgress$.asObservable();

    constructor(private ngZone: NgZone) {
        this.hubConnection = new signalR.HubConnectionBuilder()
            .withUrl('http://localhost:5001/progress')
            .withAutomaticReconnect()
            .build();

        this.hubConnection.on('ReceiveProgress', (fileName: string | null, percentage: number) => {
            this.ngZone.run(() => {
                if (fileName === null) {
                    this.currentProgress$.next(null);
                } else {
                    this.currentProgress$.next({ fileName, percentage });
                }
            });
        });

        this.startConnection();
    }

    private async startConnection(): Promise<void> {
        try {
            await this.hubConnection.start();
            console.log('SignalR Connected.');
        } catch (err) {
            console.error('SignalR Connection Error: ', err);
            setTimeout(() => this.startConnection(), 5000);
        }
    }

    clearProgress(): void {
        this.currentProgress$.next(null);
    }
}
