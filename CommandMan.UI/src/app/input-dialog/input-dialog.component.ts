import { Component, EventEmitter, Input, Output, ElementRef, ViewChild, AfterViewInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';

@Component({
    selector: 'app-input-dialog',
    standalone: true,
    imports: [CommonModule, FormsModule],
    template: `
    <div class="dialog-overlay" (click)="close()">
      <div class="dialog-content" (click)="$event.stopPropagation()">
        <div class="header">
          <h2>{{ title }}</h2>
          <button class="close-btn" (click)="close()">×</button>
        </div>
        
        <div class="body">
          <label>{{ prompt }}</label>
          <input #inputField type="text" [(ngModel)]="value" (keydown.enter)="submit()" (keydown.escape)="close()">
          <div class="error" *ngIf="error">{{ error }}</div>
        </div>

        <div class="footer">
          <button class="btn-secondary" (click)="close()">Cancel</button>
          <button class="btn-primary" (click)="submit()">OK</button>
        </div>
      </div>
    </div>
  `,
    styles: [`
    .dialog-overlay {
      position: fixed;
      top: 0;
      left: 0;
      width: 100%;
      height: 100%;
      background: rgba(0, 0, 0, 0.7);
      backdrop-filter: blur(2px);
      display: flex;
      justify-content: center;
      align-items: center;
      z-index: 2000;
    }

    .dialog-content {
      background: #1e1e24;
      border: 1px solid #333;
      border-radius: 8px;
      width: 400px;
      box-shadow: 0 10px 30px rgba(0,0,0,0.5);
      animation: slideIn 0.2s ease-out;
    }

    .header {
      padding: 15px 20px;
      border-bottom: 1px solid #333;
      display: flex;
      justify-content: space-between;
      align-items: center;
    }

    .header h2 {
      margin: 0;
      font-size: 1.1rem;
      color: #e0e0e0;
    }

    .close-btn {
      background: none;
      border: none;
      color: #888;
      font-size: 1.5rem;
      cursor: pointer;
      line-height: 1;
    }
    .close-btn:hover { color: #fff; }

    .body {
      padding: 2px 20px;
    }
    
    label {
      display: block;
      margin-bottom: 8px;
      color: #aaa;
    }

    input {
      width: 100%;
      padding: 8px 12px;
      background: #2a2a30;
      border: 1px solid #444;
      border-radius: 4px;
      color: #fff;
      font-size: 1rem;
      outline: none;
    }
    input:focus {
      border-color: #7b68ee;
      box-shadow: 0 0 0 2px rgba(123, 104, 238, 0.2);
    }

    .error {
      color: #ff5555;
      font-size: 0.9rem;
      margin-top: 5px;
    }

    .footer {
      padding: 15px 20px;
      display: flex;
      justify-content: flex-end;
      gap: 10px;
    }

    button {
      padding: 8px 16px;
      border-radius: 4px;
      border: none;
      font-weight: 500;
      cursor: pointer;
      transition: all 0.2s;
    }

    .btn-secondary {
      background: #333;
      color: #ccc;
    }
    .btn-secondary:hover { background: #444; }

    .btn-primary {
      background: #7b68ee;
      color: #fff;
    }
    .btn-primary:hover { background: #6a5acd; }

    @keyframes slideIn {
      from { transform: translateY(-20px); opacity: 0; }
      to { transform: translateY(0); opacity: 1; }
    }
  `]
})
export class InputDialogComponent implements AfterViewInit {
    @Input() title = 'Input';
    @Input() prompt = 'Enter value:';
    @Input() initialValue = '';
    @Output() submitted = new EventEmitter<string>();
    @Output() closed = new EventEmitter<void>();

    @ViewChild('inputField') inputField!: ElementRef;

    value = '';
    error = '';

    ngOnInit() {
        this.value = this.initialValue;
    }

    ngAfterViewInit() {
        setTimeout(() => this.inputField.nativeElement.focus(), 0);
    }

    submit() {
        if (!this.value.trim()) {
            this.error = 'Value cannot be empty';
            return;
        }
        this.submitted.emit(this.value);
    }

    close() {
        this.closed.emit();
    }
}
