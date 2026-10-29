# Steam Share Master（简体中文版）

Steam Share Master（SSM）是一款 Windows 工具，可以按需阻止 Steam 访问网络，同时保持电脑其他程序的网络连接。本分支基于原作者的 0.1.4 版本，提供简体中文界面、托盘菜单和操作提示，并支持自动查找 Steam 安装路径。

原项目最初于 2025 年 2 月 13 日使用 Python 发布，2025 年 6 月 22 日迁移至 Electron。

SSM 与 Steam 没有官方关联。Steam 是 Valve Corporation 的商标。

## 功能

- 按需断开或恢复 Steam 的网络连接，不需要让整台电脑断网。
- 自动查找 Steam 安装路径，也可以手动选择。
- 使用简体中文界面和系统托盘菜单。
- 尝试在一人离线时，让家庭成员使用同一款共享单机游戏；实际效果取决于游戏和 Steam 客户端版本，不保证所有游戏都可用。

## 下载与使用

从 [GitHub 发布页面](https://github.com/RedeatI/steam-share-master-zh/releases/latest) 下载 `Steam-Share-Master-Lite.exe`，双击即可打开，无需安装。程序需要管理员权限来管理防火墙；双击后可能出现 Windows 管理员授权提示。

精简版使用 Windows 自带的 .NET Framework 4.x，不再包含 Electron 的浏览器运行环境。程序会将 Steam 路径保存在当前用户的应用数据目录中；断网功能使用 Windows 筛选平台（WFP），无需启用 Windows 防火墙配置，但需要基础筛选引擎（BFE）服务运行。

程序没有数字签名，Windows 可能提示发布者未知。可以在发布页面查看 SHA-256 校验值。

WFP 网络规则和切换逻辑的源码可在 [WfpSession.cs](lite/WfpSession.cs) 和 [NetworkSwitch.cs](lite/NetworkSwitch.cs) 中查看，相关验证代码位于 [lite/tests](lite/tests)。

原作者的 [0.1.4 安装包](https://github.com/p4ul-t/steam-share-master-ssm/releases/download/v0.1.4.electron_js/Steam.Share.Master.Setup.0.1.4.exe) 是原版，并不包含本分支的汉化和路径自动查找功能。

本仓库也保留原来的 Electron 版本。如果要从 Electron 版本的源代码运行，需要安装 Node.js，然后在项目目录中执行：

```bash
npm install
npm start
```

程序需要管理员权限才能添加和移除 WFP 网络筛选规则。原项目的安装包配置支持选择安装目录。

要在 Windows 上构建 Electron 版本安装包，可以执行 `npm run build -- --win nsis --x64 --publish never`；构建结果位于 `dist` 目录。精简版源码及构建脚本位于 `lite` 目录，运行 `powershell -ExecutionPolicy Bypass -File lite/build.ps1` 可构建免安装程序；如需自行构建安装包，需安装 NSIS 3 并在命令末尾添加 `-Installer`。

## 使用方法

1. 启动 Steam Share Master。
2. 程序会优先使用已保存且有效的 Steam 路径；如果没有有效路径，会尝试从 Windows 注册表查找。未能找到时，点击 **选择 Steam 程序位置**，手动选择 `steam.exe`。可参考[路径查找说明](find-Steam.exe.md)。
3. 确认游戏进度已保存后，点击 **断开 Steam 网络**。程序会添加仅针对 Steam 程序的临时 WFP 规则，然后关闭并重启 Steam。
4. 游玩结束后，点击 **恢复 Steam 网络**。程序会再次关闭并重启 Steam。

断网期间请保持本软件运行：退出或异常关闭后，临时 WFP 规则会自动移除，Steam 可以重新联网。精简版不会移除旧版创建的 Windows 防火墙规则；如曾在旧版中断开 Steam，请先在旧版中恢复。精简版不包含原版的开机启动选项。

**注意：**切换时会强制关闭 Steam，操作前请先保存并退出正在运行的游戏。离线期间 Steam 云存档不会同步；重新联网后如果出现存档冲突，请仔细确认要保留的版本。原作者建议断开 Steam 后关闭目标游戏的 Steam 云功能，但不能保证该设置会自动恢复，请自行检查。

以下是原项目的演示视频，画面中的界面为原版：

https://github.com/user-attachments/assets/c67c62ed-fe92-435e-8836-c0ed193b9482

## 已知问题与限制

- 需要持续联网或使用额外联网验证的游戏可能无法运行。
- Steam 的家庭共享规则或客户端更新后，此方法可能失效或需要调整。
- 同时使用家庭库中同一款游戏的效果尚未得到本分支的完整验证。
- 离线期间无法使用需要 Steam 网络连接的功能。

## 遇到问题

如果程序没有找到 Steam，可以点击 **选择 Steam 程序位置**，手动选择 `steam.exe`。查找方法也可以参考[说明文档](find-Steam.exe.md)。

## 许可证

本项目采用 GNU 通用公共许可证第 3 版（GPL-3.0）。详情请查看 [LICENSE](https://github.com/p4ul-t/steam-share-master-ssm/blob/main/LICENSE)。

## 参与贡献

欢迎 fork 本项目、提交问题或发起拉取请求。

如果你觉得原项目有用，也欢迎为[原项目](https://github.com/p4ul-t/steam-share-master-ssm)点星并分享。
