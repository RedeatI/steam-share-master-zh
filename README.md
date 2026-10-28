# 简体中文版本

本分支基于原作者的 Steam Share Master 0.1.4，提供简体中文界面、托盘菜单和日志，并在没有有效手动路径时从 Windows 注册表查找 Steam。已保存且存在的手动路径优先；未找到时可以手动选择。

切换网络会强制关闭并重启 Steam，请先保存游戏进度。程序通过 Windows 防火墙隔离 Steam，通常需要管理员权限。离线游戏兼容性、Steam 家庭库并行使用效果取决于游戏和 Steam 客户端，未保证可用。

请遵循下方原项目的 GPL-3.0 许可证说明。

---

# Steam Share Master - SSM

## 📝 Description
Steam Share Master (SSM) is a portable Windows software coded in Python that isolates Steam from the internet on demand while keeping your PC online.  
It lets you play Steam Family Sharing games simultaneously, so you and your family members can enjoy the same game without interruptions.  

13/02/2025 : first release (python) -
22/06/2025 : migrated the app to electronJS

SSM has NO affiliation with Steam.  
Steam is a trademark of Valve Corporation.

## 🚀 Features
- Keep Steam isolated from the network without disconnecting your PC from the internet
- Play shared family games simultaneously
- Simple and intuitive interface
- Lightweight and easy to set up
- System tray icon with options

## 📥 Installation
1. [Download](https://github.com/p4ul-t/steam-share-master-ssm/releases/download/v0.1.4.electron_js/Steam.Share.Master.Setup.0.1.4.exe)
2. Click on the `.exe` file to install the program  
   *When the program is launched, pin it to your taskbar for quicker access in the future.*

## 🛠 Usage
1. Launch Steam Share Master
2. Locate your `Steam.exe` file if it's not located in `C:\Program Files (x86)\Steam\Steam.exe` by default [(optional)](https://github.com/p4ul-t/steam-share-master-ssm/blob/main/find-Steam.exe.md)
3. Click on **Block Steam** to isolate Steam from the network if someone from your Steam Family is currently playing a game you want to play
4. Click on **Unblock Steam** to reconnect Steam to the network when you're done playing  

   **DISCLAIMER:** You must deactivate Steam Cloud on the game you want to play after blocking Steam, but don't worry : Steam Cloud saves will resynchronize and reactivate by itself after unblocking Steam.





https://github.com/user-attachments/assets/c67c62ed-fe92-435e-8836-c0ed193b9482




## 🔥 Known Issues & Limitations
- Some games with always-online DRM may not work.
- If Steam updates its Family Sharing policies, the method may require adjustments.

## 🔎 Troubleshooting Guide
"I don't know where my `Steam.exe` file is and my Steam.exe path is not by default `C:\Program Files (x86)\Steam\Steam.exe`" [INSTRUCTIONS](https://github.com/p4ul-t/steam-share-master-ssm/blob/main/find-Steam.exe.md)    

## 📄 License
This project is licensed under the GNU General Public License v3.0. See [LICENSE](https://github.com/p4ul-t/steam-share-master-ssm/blob/main/LICENSE) for details.

## 🤝 Contributing
Contributions are welcome! Feel free to fork the project, submit issues, or create pull requests.

### ❤️ Support the Project
If you find this project useful, ⭐ star the repository and share it!



