import { Component, EventEmitter, Output } from '@angular/core';
import { CommonModule } from '@angular/common';
import { BridgeService } from '../services/bridge.service';

@Component({
  selector: 'app-about-dialog',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="dialog-overlay" (click)="close()">
      <div class="dialog-content" (click)="$event.stopPropagation()">
        <div class="header">
          <h2>About CommandMan</h2>
          <button class="close-btn" (click)="close()">×</button>
        </div>
        
        <div class="body">
          <div class="logo">
            <span class="logo-icon">⚡</span>
          </div>
          
          <div class="info" *ngIf="appInfo$ | async as info">
            <h3>{{ info.AppName }}</h3>
            <p class="version">Version {{ info.Version }}</p>
            <p class="description">
              A modern file manager built with<br>
              Angular 18 + C# WPF WebView2
            </p>
            <p class="copyright">© 2026 CommandMan Team</p>
          </div>
        </div>

        <div class="footer">
          <button class="btn-primary" (click)="close()">OK</button>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .dialog-overlay {
      position: fixed;
      top: 0;
      left: 0;
      right: 0;
      bottom: 0;
      background: rgba(0, 0, 0, 0.5);
      display: flex;
      align-items: center;
      justify-content: center;
      z-index: 1000;
      backdrop-filter: blur(2px);
    }

    .dialog-content {
      background: #1e1e2e;
      border: 1px solid #313244;
      border-radius: 8px;
      width: 350px;
      box-shadow: 0 4px 20px rgba(0, 0, 0, 0.3);
      animation: popIn 0.2s cubic-bezier(0.18, 0.89, 0.32, 1.28);
    }

    @keyframes popIn {
      from { transform: scale(0.9); opacity: 0; }
      to { transform: scale(1); opacity: 1; }
    }

    .header {
      display: flex;
      justify-content: space-between;
      align-items: center;
      padding: 12px 16px;
      border-bottom: 1px solid #313244;
      
      h2 {
        font-size: 16px;
        color: #cdd6f4;
        margin: 0;
      }
    }

    .close-btn {
      background: none;
      border: none;
      color: #a6adc8;
      font-size: 20px;
      cursor: pointer;
      line-height: 1;
      padding: 0;

      &:hover {
        color: #f38ba8;
      }
    }

    .body {
      padding: 24px 16px;
      text-align: center;
    }

    .logo-icon {
      font-size: 48px;
      display: block;
      margin-bottom: 16px;
      filter: drop-shadow(0 0 10px rgba(137, 180, 250, 0.5));
    }

    .info {
      h3 {
        color: #89b4fa;
        margin: 0 0 4px;
        font-size: 20px;
      }

      .version {
        color: #fab387;
        font-family: monospace;
        margin-bottom: 16px;
      }

      .description {
        color: #bac2de;
        line-height: 1.5;
        margin-bottom: 16px;
        font-size: 14px;
      }

      .copyright {
        color: #6c7086;
        font-size: 12px;
      }
    }

    .footer {
      padding: 12px 16px;
      border-top: 1px solid #313244;
      display: flex;
      justify-content: flex-end;
    }

    .btn-primary {
      background: #89b4fa;
      color: #1e1e2e;
      border: none;
      padding: 6px 16px;
      border-radius: 4px;
      font-weight: 600;
      cursor: pointer;
      transition: all 0.2s;

      &:hover {
        background: #b4befe;
      }
    }
  `]
})
export class AboutDialogComponent {
  @Output() closed = new EventEmitter<void>();

  appInfo$;

  constructor(private bridgeService: BridgeService) {
    this.appInfo$ = this.bridgeService.appInfo;
  }

  close() {
    this.closed.emit();
  }
}
