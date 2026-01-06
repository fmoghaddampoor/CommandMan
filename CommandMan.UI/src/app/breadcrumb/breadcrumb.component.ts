import { Component, Input, Output, EventEmitter, OnChanges, SimpleChanges } from '@angular/core';
import { CommonModule } from '@angular/common';

interface BreadcrumbSegment {
  name: string;
  path: string;
}

@Component({
  selector: 'app-breadcrumb',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="breadcrumb-container">
      <span class="path-icon">📂</span>
      <ng-container *ngFor="let segment of segments; let last = last">
        <span 
          class="segment" 
          [class.clickable]="!last" 
          (click)="!last && onSegmentClick(segment)">
          {{ segment.name }}
        </span>
        <span *ngIf="!last" class="separator">›</span>
      </ng-container>
    </div>
  `,
  styles: [`
    .breadcrumb-container {
      display: flex;
      align-items: center;
      white-space: nowrap;
      overflow: hidden;
      text-overflow: ellipsis;
      color: #e0e0e0;
      font-family: 'Consolas', 'Monaco', monospace;
      font-size: 0.9rem;
    }

    .path-icon {
      margin-right: 8px;
    }

    .segment {
      padding: 2px 4px;
      border-radius: 3px;
      transition: background 0.2s;
    }

    .segment.clickable {
      cursor: pointer;
      color: #aaa;
    }

    .segment.clickable:hover {
      background: #333;
      color: #fff;
    }

    .separator {
      margin: 0 4px;
      color: #666;
    }
  `]
})
export class BreadcrumbComponent implements OnChanges {
  @Input() path = '';
  @Output() navigate = new EventEmitter<string>();

  segments: BreadcrumbSegment[] = [];

  ngOnChanges(changes: SimpleChanges): void {
    if (changes['path']) {
      this.parsePath();
    }
  }

  private parsePath(): void {
    if (!this.path) {
      this.segments = [];
      return;
    }

    const parts = this.path.split('\\').filter(p => p);
    this.segments = [];
    let accumulatedPath = '';

    for (let i = 0; i < parts.length; i++) {
      const part = parts[i];
      if (i === 0 && part.endsWith(':')) {
        accumulatedPath = part + '\\'; // Drive root
      } else {
        // If accumulatedPath ends with \, don't add another one
        if (accumulatedPath.endsWith('\\')) {
          accumulatedPath += part;
        } else {
          accumulatedPath += '\\' + part;
        }
      }

      this.segments.push({
        name: part,
        path: accumulatedPath
      });
    }
  }

  onSegmentClick(segment: BreadcrumbSegment): void {
    this.navigate.emit(segment.path);
  }
}
