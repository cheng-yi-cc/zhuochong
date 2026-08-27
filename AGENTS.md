# 项目协作约定

这是仅面向 Windows 的透明桌宠，使用 .NET Framework 4.8、WPF 和 Win32 API。主体代码集中在 `src/ReptileDesktopPet.cs`。

## 工作规则

- 修改源码后运行 `.\build.ps1`，并将生成的 `dist\ReptileDesktopPet.exe` 与源码一起提交。
- 用户可见行为变化时同步 `README.md`；项目较小时不要为单次变更新建额外文档。
- 验证脚本、截图、视频和临时程序放在已忽略的 `.codex-work/`，不得提交。
- 预览前退出旧实例，再双击 `dist\ReptileDesktopPet.exe`；全局单实例锁会阻止新版与旧版同时运行。

## 实现红线

- 保持桌宠点击穿透，并位于动态壁纸之上、普通窗口之下；不要破坏 Wallpaper Engine 兼容性。
- 所有显示器共用虚拟桌面坐标；显示设置变化后必须重建各屏渲染窗口。
- 暂停统一使用 `CreatureController.IsPaused`，托盘与桌面空白点击共用切换入口。
- 腿对数保存在 `HKEY_CURRENT_USER\Software\ReptileDesktopPet`，开机自启保存在当前用户的 `Run` 注册表项；不要混用。
- 鼠标钩子回调只记录坐标；桌面判定投递到 WPF Dispatcher，避免钩子超时。
- 桌面空白必须同时满足：实际窗口属于 Explorer 的 `SysListView32/FolderView`，且 MSAA 未命中图标；异常时不切换。
- 左键拖动超过系统阈值时不得切换，以免影响桌面框选。

## 修改后检查

1. `git diff --check` 无错误，`.\build.ps1` 编译成功。
2. 桌面空白可切换追踪；图标、任务栏、窗口和框选不会触发。
3. 托盘暂停状态同步；腿对数可设为 1～50、立即生效并持久化。
