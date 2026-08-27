# 项目说明

这是一个仅面向 Windows 的透明桌宠。程序使用 .NET Framework 4.8、WPF 和 Win32 API，保持单文件源码与免安装构建流程。

## 目录与交付

- `src/ReptileDesktopPet.cs`：应用入口、托盘、追踪模型、渲染窗口及 Win32 互操作。
- `app.manifest`：DPI 与 Windows 运行配置。
- `build.ps1`：调用系统自带的 64 位 C# 编译器。
- `dist/ReptileDesktopPet.exe`：唯一最终交付产物；构建时直接覆盖。
- `README.md`：面向使用者的运行、操作和构建说明。

不要提交截图、临时探针、测试 EXE 或其他验证过程文件。

## 构建与预览

在项目根目录运行：

```powershell
.\build.ps1
```

构建后先退出正在运行的旧实例，再双击 `dist\ReptileDesktopPet.exe`。程序使用全局单实例锁，旧实例未退出时新版不会启动。

## 实现约束

- 桌宠必须保持点击穿透，并位于动态壁纸之上、普通应用窗口之下；不要破坏 Wallpaper Engine 兼容性。
- 所有显示器共用虚拟桌面坐标；显示设置变化后必须重建各屏渲染窗口。
- 暂停状态统一使用 `CreatureController.IsPaused`，托盘操作与桌面空白点击必须通过同一个切换入口，避免状态分叉。
- 全局鼠标钩子回调只记录按下和抬起坐标，耗时的桌面判定应投递到 WPF Dispatcher，避免系统移除超时钩子。
- 桌面空白判定必须同时满足：实际命中窗口属于 Explorer 的 `SysListView32/FolderView`，且 MSAA 命中测试没有返回图标。窗口、任务栏、菜单或判定异常时一律不切换。
- 左键拖动超过系统拖动阈值时不得切换暂停状态，以免影响桌面框选。

## 修改后检查

至少完成以下检查：

1. `git diff --check` 无错误。
2. `.\build.ps1` 编译成功并更新 `dist\ReptileDesktopPet.exe`。
3. 桌面空白单击可停止、再次单击可继续。
4. 文件、快捷方式、任务栏、普通窗口和桌面框选均不会触发切换。
5. 托盘“暂停/继续”与桌面点击后的实际状态一致。
