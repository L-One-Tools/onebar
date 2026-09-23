# OneBar Desktop Architecture

## Runtime

```text
OneBar.exe
├─ AppIdentity
│  └─ SetCurrentProcessExplicitAppUserModelID("L-One.OneBar")
├─ SingleInstanceService
│  ├─ Mutex
│  └─ named EventWaitHandle
├─ SettingsService
│  └─ %APPDATA%\OneBar\settings.json
├─ WorkflowService
│  └─ <WorkflowRoot>\<Workflow>\*.md|*.txt
├─ ShellIntegrationService
│  ├─ HKCU Run
│  ├─ Desktop shortcut
│  └─ Start Menu shortcut
├─ MainWindow
│  ├─ collapsed top handle
│  ├─ workflow catalog
│  ├─ workflow steps
│  ├─ clipboard copy
│  ├─ file drag-drop
│  ├─ collapse animation
│  └─ tray icon
└─ SettingsWindow
```

## Data model

```text
Workflows/
├─ Image Lib/
│  ├─ 01-Analyze.md
│  ├─ 02-Build.md
│  └─ 03-Submit.md
└─ Web/
   ├─ 01-Brief.md
   └─ 02-Build.md
```

No database is required.
