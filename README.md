# ⚡️ CommandMan

![CommandMan Screenshot](docs/images/screenshot.png?v=2)

**CommandMan** is a modern, high-performance dual-pane file manager designed for power users who demand speed, precision, and a premium aesthetic. Built with a hybrid architecture of WPF and Angular, it combines the robustness of .NET with the fluidity of modern web technologies.

---

## ✨ Features

### 🚀 Performance & UX
- **Dual-Pane Interface**: Maximize productivity with a classic side-by-side view for seamless file operations.
- **Premium Dark Theme**: Sleek, high-contrast design inspired by modern development environments.
- **Accessibility First**: Large, accessible buttons and high-visibility typography.

### 📋 Advanced Selection Logic
- **Marking System**: Quickly mark multiple items using `Space` or `Insert` keys.
- **Explorer-like Selection**: Full support for `Ctrl + Click` to toggle individual items and `Shift + Click` for range selection.
- **Global Commands**: `Ctrl+A` to select all, `Ctrl+D` to deselect all.

### 🛠️ Bulk File Operations
- **Real-Time Progress**: Powered by **SignalR**, get live progress updates for bulk Delete, Copy, and Move operations.
- **Intelligent Error Handling**: Robust handling of file collisions and system locks.
- **Smart Editor Integration**: Edit files instantly with `F4`, prioritizing **Notepad++** with an automatic fallback to system **Notepad**.

### ⌨️ Professional Workflow
- **Function Key Navigation**: Classic commander-style shortcuts (`F1-F8`, `Tab`, `Delete`).
- **Responsive Navigation**: Virtualized scrolling handles thousands of files with zero lag.
- **Breadcrumb Navigation**: Path tracking and breadcrumb jumping for deep directory structures.

---

## 🛠️ Tech Stack

- **Shell**: WPF (.NET Core) with **WebView2** for high-fidelity UI rendering.
- **Frontend**: **Angular 18+**, SCSS (Vanilla), CDK Virtual Scroll.
- **Communication**: 
  - **WebView2 Bridge**: High-speed, low-latency JSON bridge for C# to JavaScript interaction.
  - **SignalR Core**: Dedicated progress hub for asynchronous background tasks.

---

## 🚦 Getting Started

### Prerequisites
- .NET SDK (latest)
- Node.js & npm
- Microsoft Edge WebView2 Runtime

### Setup & Run
1. **Frontend**:
   ```pwsh
   cd CommandMan.UI
   npm install
   npm start
   ```
2. **Backend**:
   ```pwsh
   cd CommandMan.Shell
   dotnet run
   ```

---

## 🦾 Built with #antigravity

This project was developed in partnership with **AntiGravity**, an agentic AI pair-programmer. The partnership allowed for rapid prototyping, complex feature implementation, and rigorous UI/UX refinements.

---

*Command your files. Faster. Better.*
