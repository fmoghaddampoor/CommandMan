import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ProgressService, ProgressUpdate } from '../services/progress.service';
import { Observable } from 'rxjs';

@Component({
  selector: 'app-progress-overlay',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="overlay" *ngIf="progress$ | async as progress">
      <div class="progress-dialog">
        <h3>File Operation in Progress</h3>
        <p class="file-name">{{ progress.fileName }}</p>
        <div class="progress-bar-container">
          <div class="progress-bar" [style.width.%]="progress.percentage"></div>
          <span class="percentage-text">{{ progress.percentage }}%</span>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .overlay {
      position: fixed;
      top: 0;
      left: 0;
      width: 100vw;
      height: 100vh;
      background: rgba(0, 0, 0, 0.7);
      display: flex;
      justify-content: center;
      align-items: center;
      z-index: 2000;
      backdrop-filter: blur(2px);
    }

    .progress-dialog {
      background: #1e1e1e;
      border: 1px solid #333;
      padding: 24px;
      border-radius: 8px;
      width: 400px;
      box-shadow: 0 8px 32px rgba(0, 0, 0, 0.5);
      color: #e0e0e0;
    }

    h3 {
      margin-top: 0;
      font-size: 1.1rem;
      color: #bb86fc;
    }

    .file-name {
      font-size: 0.9rem;
      color: #aaa;
      margin-bottom: 20px;
      overflow: hidden;
      text-overflow: ellipsis;
      white-space: nowrap;
    }

    .progress-bar-container {
      height: 24px;
      background: #333;
      border-radius: 12px;
      position: relative;
      overflow: hidden;
      border: 1px solid #444;
    }

    .progress-bar {
      height: 100%;
      background: linear-gradient(90deg, #bb86fc, #03dac6);
      transition: width 0.1s linear;
    }

    .percentage-text {
      position: absolute;
      top: 50%;
      left: 50%;
      transform: translate(-50%, -50%);
      font-size: 0.8rem;
      font-weight: bold;
      color: #fff;
      text-shadow: 1px 1px 2px rgba(0, 0, 0, 0.8);
    }
  `]
})
export class ProgressOverlayComponent {
  progress$: Observable<ProgressUpdate | null>;

  constructor(private progressService: ProgressService) {
    this.progress$ = this.progressService.currentProgress;
  }
}
