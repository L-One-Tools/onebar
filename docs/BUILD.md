# 本地构建

## 环境

- Windows 10 / 11 x64
- .NET 8 SDK
- Inno Setup 6

## 构建

从仓库根目录运行：

```powershell
./BUILD_AND_PACKAGE.cmd
```

脚本会发布 self-contained 的 `OneBar.exe`，然后调用 Inno Setup 生成安装包。产物写入 `artifacts/`，该目录不会进入 Git。

## 构建后最小检查

1. 确认 `OneBar.exe` 的产品名为 OneBar。
2. 在干净的 Windows 用户环境中安装并启动。
3. 验证顶部热区、工作流展开、复制、拖动导出、设置与卸载。
4. 发布二进制前计算 SHA-256，并提供版本、安装说明、限制和回退信息。

不要把本机的 Workflow、设置、日志或构建缓存提交到仓库。
