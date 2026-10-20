# OneBar

Windows 顶部工作流工具，支持本地 .md/.txt 文件、递归子文件夹、紧贴排列的浮动胶囊、五级选中色调与按深度回收。

当前源码版本 1.1.0-local.2，C# / .NET 8 / WPF，MIT 许可见 LICENSE。这是本机验证的更新候选，不表示已发布正式版。

## 构建

Windows x64 安装 .NET 8 SDK 和 Inno Setup 6，然后执行：

```powershell
powershell -ExecutionPolicy Bypass -File .\build\Build-OneBar.ps1
```

脚本输出到 artifacts/publish/win-x64 和 artifacts/installer，只构建，不安装、不启动。也可先执行 `dotnet build .\src\OneBar\OneBar.csproj -c Release` 检查源码。

## 使用和升级

安装后在设置中选择工作流目录；点击分类逐层展开，点击文件复制内容，拖动文件到支持的接收应用。默认回收等待及动画各 333ms；升级保留原设置，要使用新速度需分别手动设为 333ms。设置保存在当前用户 AppData 中，工作流目录可自行选择。

安装包自包含运行时；源码仓库不提交 EXE、bin、obj、个人设置、工作流或使用记录。默认目录中只有通用示例。主导航白色/深灰，弹出分支未选白色、选中按类别着色；五级为色阶，不限制递归深度。

Windows x64 为构建目标；多屏、不同 DPI 及全部 Windows 版本尚未完整验证。接收应用决定拖放能力。请勿仅因本机通过就宣称全面兼容。

## 仓库资料

参阅 [更新说明](docs/CHANGELOG.md)、[安装与升级](docs/INSTALL.md)、[验证边界](docs/VERIFICATION.md)、[贡献说明](CONTRIBUTING.md) 和 [安全说明](SECURITY.md)。
