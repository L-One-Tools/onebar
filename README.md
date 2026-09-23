# OneBar

OneBar 是一个 Windows 桌面顶部工作流启动器。

把常用提示词或说明文件放进 Workflow 文件夹后，鼠标移到屏幕顶部即可展开 OneBar：选择工作流、单击步骤复制完整内容，或拖动步骤导出真实的 Markdown / TXT 文件。

## 当前状态

- 当前工程：OneBar Desktop v1.0.0
- 技术：C#、.NET 8、WPF
- 支持：Windows 10 / 11 x64
- 公开源码仓库候选：尚未包含已编译的 EXE 或安装包

本仓库用于保存和协作源码。它不是已验证的下载发行页；若要发布安装包，应另行提供安装包、版本号、SHA-256 和安装验证记录。

## 功能

- 屏幕顶部隐藏热区与横向展开界面
- 文件夹作为工作流，`.md` / `.txt` 文件作为步骤
- 单击步骤复制全文；拖动步骤导出文件
- 锁定、刷新、自定义工作流目录和设置
- 系统托盘、任务栏图标、开机启动、桌面和开始菜单快捷方式
- 单实例运行与旧工作流目录迁移

## 本地构建

需要 Windows 10 / 11 x64、.NET 8 SDK 和 Inno Setup 6。

```powershell
./BUILD_AND_PACKAGE.cmd
```

构建会生成：

```text
artifacts/publish/win-x64/OneBar.exe
artifacts/installer/OneBar_Setup_v1.0.0.exe
```

构建脚本在缺少依赖时会提示或可经 `winget` 安装依赖。请先阅读 [docs/BUILD.md](docs/BUILD.md)。

## 默认使用方式

默认工作流位于安装目录的 `Workflows` 文件夹。你可以在设置中改成自己的目录。

```text
Workflow 文件夹
└─ 一个步骤.md
```

## 已知边界

- 本仓库没有附带当前 v1.0.0 的最终 EXE 或 Setup EXE；它们曾在原开发电脑构建和安装，但未包含在本交接包中。
- 当前交接资料记录了成功安装这一结果，不等于完整的逐项运行回归报告。
- 当前源码使用 MIT 许可证；它允许使用、修改和再发布，但须保留版权与许可证文本。

## 参与和安全

请先阅读 [贡献说明](CONTRIBUTING.md) 与 [安全说明](SECURITY.md)。

## 版权与许可证

版权所有 © 2026 L-One，依据 [MIT License](LICENSE) 发布。
