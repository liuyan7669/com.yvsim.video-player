# Video Player

基于 Unity VideoPlayer 和 uGUI 的视频播放器，主入口为 `Yvsim.Video.VideoPlayerUGUI`。
提供 VideoClip/URL/StreamingAssets 加载、第一帧回调、播放控制、进度、音量、静音和播放器区域铺满/恢复。

## 安装与依赖

当前源码版本为 **1.0.0**。Unity 最低版本为 **2021.3**。安装本地源码时：

1. 在 Package Manager 选择 **Add package from disk**，先选择本地 `com.yvsim.inspector-groups/package.json`。
2. 再选择本包 `com.yvsim.video-player/package.json`。
3. 等待 Unity 完成依赖解析和编译。也可将这两个完整包目录放入工程的 `Packages/`，作为 Embedded Packages。

本包声明 Inspector Groups **1.0.0** 依赖。其他 Unity 依赖由 `package.json` 声明并由 UPM 解析。
Inspector 绘制统一由 Inspector Groups 提供，包内配置通过其特性或扩展入口表达；不依赖其他 Yvsim 业务包。
本包提供本地源码、Git 和 OpenUPM 安装方式；固定版本使用 `1.0.0`，具体步骤见下文。

依赖 Unity uGUI 和 Video 模块。

## 从示例 Prefab 开始

1. 在 Package Manager 导入 **Video Player** Sample。
2. 将 `Examples/VideoPlayer.prefab` 放入场景 Canvas，并确认场景含 EventSystem。
3. 在 `VideoPlayerUGUI` 选择 VideoClip 或 Url，设置对应资源/地址。
4. 需要启用对象时自动播放，启用 `Open On Enable` 与 `Play On Enable`。

Sample 包含播放器 Prefab、皮肤 PNG/PSD 和 `Examples/mov_bbb.mp4`。
主组件可从 `Add Component > Yvsim > 视频 > UGUI 视频播放器` 添加，但单独添加不会生成 UI；
必须同时配置子对象中的 `VideoRenderer`、`VideoControl` 及其 UI 引用，推荐从示例 Prefab 修改。
Inspector 条件字段与浏览本地文件按钮由 Inspector Groups 统一绘制。

## 代码使用

```csharp
using Yvsim.Video;
using UnityEngine;
using UnityEngine.Video;

public sealed class VideoExample : MonoBehaviour
{
    [SerializeField] private VideoPlayerUGUI player;
    [SerializeField] private VideoClip clip;

    public void Open()
    {
        player.OpenVideo(clip, autoPlay: true,
            firstFrameLoadCompleted: () => Debug.Log("第一帧准备完成"));
    }
}
```

```csharp
player.OpenVideo("https://example.com/video.mp4");
player.OpenVideoFromStreamingAssets("Videos/intro.mp4");
player.Play();
player.Pause();
player.Stop();
player.CloseVideo(clearScreen: true);
```

StreamingAssets 入口接收相对路径。URL 或本地文件的可用格式、协议与解码能力由 Unity 和目标平台决定。
打开为异步准备过程；首帧就绪后先完成自动播放状态，再通知第一帧回调。回调内可停止、关闭或换源，不会被旧请求继续播放覆盖。当前 API 没有独立的加载失败事件，
无效媒体/网络问题需检查 Unity VideoPlayer 日志，停止/关闭可取消等待。

| 成员 | 含义 |
| --- | --- |
| `Length` / `CurrentTime` | 总时长/当前位置（秒），CurrentTime 可用于跳转；无有效帧率/帧位置时返回 0 |
| `IsPlaying` / `IsPause` | 本播放器维护的播放/暂停状态 |
| `Volume` / `Mute` | 直接音轨输出音量/静音 |
| `PlayEndAction` | 自然结束回调；主动 Stop 不调用 |
| `FullScreen` | 将播放器 RectTransform 铺满父容器，再恢复原布局 |
| `RecordWindowRect()` | 在窗口布局调整后重新记录恢复位置 |

`FullScreen` 是 uGUI 布局切换，不会调用浏览器 requestFullscreen 或系统 `Screen.fullScreen`。
`CloseVideo(true)` 释放显示纹理，组件销毁时清理其拥有的 RenderTexture。
视频源切换、Stop 和组件停用会解除未完成的第一帧监听，避免旧加载影响后续播放。

## 组件组成

`VideoPlayerUGUI` 协调 `VideoRenderer`（视频与纹理）和 `VideoControl`（按钮与控件）。
`PlayControl` 管理播放/暂停/停止；`VolumeControl` 管理 Slider、Toggle 和音量图标；
`ProgressControl` 管理进度和时长文本；`SettingControl` 记录并恢复 RectTransform。
业务一般只需要引用 `VideoPlayerUGUI`，不要直接驱动内部 VideoPlayer 绕过状态同步。

## 验证与许可

包内 EditMode 回归覆盖控制按钮、空视频时间和取消加载的监听清理。安装 Test Framework 并将
`com.yvsim.video-player` 加入项目 manifest 的 `testables` 后运行。
真实视频解码、首帧、拖动进度、音轨和 WebGL 用户手势限制仍需在目标设备验证。

代码使用 MIT License。Sample 中的视频、皮肤和 PSD 受其素材许可约束；包内没有逐项素材授权证明，
公开再分发这些媒体前应核实来源与授权。

## 从 GitHub 安装

仓库：[com.yvsim.video-player](https://github.com/liuyan7669/com.yvsim.video-player)。仓库名与 UPM 包名一致。

先通过 Package Manager 的 Add package from git URL 安装以下 Yvsim 依赖，再安装本包：

- `https://github.com/liuyan7669/com.yvsim.inspector-groups.git#main`（1.0.0）

在 Unity Package Manager 选择 Add package from git URL，输入：

```text
https://github.com/liuyan7669/com.yvsim.video-player.git#main
```

此地址跟随 main 分支。需要固定 1.0.0 源码时，将 `#main` 改为 `#v1.0.0`；Unity 官方依赖仍按 package.json 解析。

## 从 OpenUPM 安装

在 Unity 的 Project Settings > Package Manager 中添加 Scoped Registry：

- Name：OpenUPM
- URL：`https://package.openupm.com`
- Scope：`com.yvsim`

然后在 Package Manager 中通过 Add package by name 输入 `com.yvsim.video-player`，版本填 `1.0.0`。也可从 My Registries 中选择本包。声明的 Yvsim 依赖由 OpenUPM 解析，Unity 官方依赖仍由 Unity 注册表提供。

如果已安装 OpenUPM CLI，可在工程根目录执行：

```sh
openupm add com.yvsim.video-player@1.0.0
```

[OpenUPM 包页面](https://openupm.com/packages/com.yvsim.video-player/)
