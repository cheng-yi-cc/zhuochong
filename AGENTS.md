# 项目协作约定

- 这是 Windows 桌面应用，主体代码集中在 `src/ReptileDesktopPet.cs`，使用系统自带的 .NET Framework 4.8 编译器。
- 修改源码后必须运行 `./build.ps1`，并将生成的 `dist/ReptileDesktopPet.exe` 与源码一起提交。
- 用户可见行为发生变化时，同步更新 `README.md`；项目规模较小时不要为了记录单次变更创建额外文档。
- 腿对数设置保存在 `HKEY_CURRENT_USER\Software\ReptileDesktopPet`，开机自启设置位于当前用户的 `Run` 注册表项；修改相关逻辑时不要混用两个位置。
- 验证脚本、截图、视频和临时程序统一放在 `.codex-work/`，不得提交到仓库。
