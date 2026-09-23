# 从 PowerShell 原型迁移到 OneBar Desktop

## 原型

```text
powershell.exe
└─ OneBar.ps1
```

任务栏身份属于 PowerShell。

## Desktop v1.0

```text
OneBar.exe
```

任务栏身份属于 OneBar。

## 保留的数据

- `.md` / `.txt` 工作流文件原样继续使用。
- `%APPDATA%\OneBar\settings.json` 格式保持兼容。
- 默认关闭键逻辑仍是 `hide`。
- 自动收回参数仍是 `collapseDelayMs` / `collapseAnimMs`。

## 第一次正式安装建议

1. 退出旧 PowerShell OneBar。
2. 安装 OneBar Desktop。
3. 选择原来的 OneBar 安装目录或新的目录。
4. 新目录 Workflows 为空时，程序自动尝试迁移旧资料。
5. 验证新 OneBar.exe 后，再清理旧 Prototype。
