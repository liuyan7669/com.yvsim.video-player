using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Video;

namespace Yvsim.Video
{
    /// <summary>
    /// 视频播放器 UGUI 组件
    /// 视频播放器的主入口类，封装了视频渲染器和控制UI，提供统一的视频播放接口
    /// 支持从 VideoClip 资源、URL 路径或 StreamingAssets 文件夹加载视频
    /// </summary>
    [AddComponentMenu("Yvsim/视频/UGUI 视频播放器")]
    [DefaultExecutionOrder(-4000)]
    [Yvsim.InspectorGroups.HideScript]
    public class VideoPlayerUGUI : MonoBehaviour
    {
        /// <summary>
        /// 视频渲染器组件，负责视频的实际渲染和播放控制
        /// </summary>
        private VideoRenderer videoRenderer;
        
        /// <summary>
        /// 视频控制UI组件，管理播放、音量、进度、设置等UI控制
        /// </summary>
        private VideoControl videoControl;
        
        /// <summary>
        /// 视频源类型，指定视频是从 VideoClip 资源还是 URL 路径加载
        /// </summary>
        [Yvsim.InspectorGroups.FoldoutGroup("视频源", true, style: Yvsim.InspectorGroups.InspectorGroupStyle.Framework)]
        [Tooltip("选择视频来源：VideoClip 使用项目内视频资源，Url 使用网址或文件路径。")]
        public VideoSource source = VideoSource.VideoClip;
        
        /// <summary>
        /// 视频文件的URL路径，当 source 为 VideoSource.Url 时使用
        /// </summary>
        [Yvsim.InspectorGroups.ShowIf("source", VideoSource.Url)]
        [Tooltip("视频来源为 Url 时使用的完整网址或本地文件路径，直接交给 VideoPlayer 加载。")]
        public string url = string.Empty;
        
        /// <summary>
        /// Unity 的 VideoClip 资源对象，当 source 为 VideoSource.VideoClip 时使用
        /// </summary>
        [Yvsim.InspectorGroups.ShowIf("source", VideoSource.VideoClip)]
        [Tooltip("视频来源为 VideoClip 时使用的项目内视频资源。")]
        public VideoClip clip = null;
        
        /// <summary>
        /// 是否在组件启用时自动打开视频
        /// 如果为 true，OnEnable 时会根据 source 类型自动打开对应的视频
        /// </summary>
        [Yvsim.InspectorGroups.FoldoutGroup("播放设置", true, style: Yvsim.InspectorGroups.InspectorGroupStyle.Framework)]
        [Tooltip("组件启用时自动打开所配置的视频源；关闭时需通过代码调用打开视频接口。")]
        public bool openOnEnable = false;
        
        /// <summary>
        /// 是否在打开视频后自动播放
        /// 如果为 true，打开视频后会立即开始播放
        /// </summary>
        [Tooltip("启用自动打开视频时，准备完成后是否立即播放；关闭则停在首帧等待播放。")]
        public bool playOnEnable = false;

        /// <summary>
        /// 组件唤醒时调用
        /// 获取子对象中的 VideoRenderer 和 VideoControl 组件，并初始化视频渲染器
        /// </summary>
        private void Awake()
        {
            videoRenderer = GetComponentInChildren<VideoRenderer>();
            videoControl = GetComponentInChildren<VideoControl>();
            videoRenderer.CreateRenderer(videoControl);
        }

        /// <summary>
        /// 组件启用时调用
        /// 如果 openOnEnable 为 true，根据 source 类型自动打开对应的视频
        /// </summary>
        private void OnEnable()
        {
            if (openOnEnable)
            {
                switch (source)
                {
                    case VideoSource.VideoClip:
                        {
                            if (clip != null)
                            {
                                OpenVideo(clip, playOnEnable);
                            }
                        }
                        break;
                    case VideoSource.Url:
                        {
                            if (!string.IsNullOrEmpty(url))
                            {
                                OpenVideo(url, playOnEnable);
                            }
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// 获取或设置全屏状态
        /// </summary>
        public bool FullScreen { get { return videoRenderer.FullScreen; } set { videoRenderer.FullScreen = value; } }

        /// <summary>
        /// 获取或设置静音状态
        /// </summary>
        public bool Mute { get { return videoRenderer.Mute; } set { videoRenderer.Mute = value; } }

        /// <summary>
        /// 获取或设置音量大小（范围：0.0f - 1.0f）
        /// </summary>
        public float Volume { get { return videoRenderer.Volume; } set { videoRenderer.Volume = value; } }

        /// <summary>
        /// 获取视频总时长（单位：秒）
        /// </summary>
        public double Length { get { return videoRenderer.Length; } }

        /// <summary>
        /// 获取或设置当前播放时间（单位：秒）
        /// 设置时会跳转到指定的时间位置
        /// </summary>
        public double CurrentTime { get { return videoRenderer.CurrentTime; } set { videoRenderer.CurrentTime = value; } }

        /// <summary>
        /// 获取当前是否正在播放视频
        /// </summary>
        public bool IsPlaying { get { return videoRenderer.IsPlaying; } }

        /// <summary>
        /// 获取当前是否处于暂停状态
        /// </summary>
        public bool IsPause { get { return videoRenderer.IsPause; } }

        /// <summary>
        /// 获取或设置视频播放结束时的回调事件
        /// 当视频播放到结尾时会触发此事件
        /// </summary>
        public Action PlayEndAction { get { return videoRenderer.PlayEndAction; } set { videoRenderer.PlayEndAction = value; } }


        /// <summary>
        /// 从 StreamingAssets 文件夹打开视频文件
        /// </summary>
        /// <param name="videoPathInStreamingAssets">视频文件在 StreamingAssets 文件夹中的相对路径</param>
        /// <param name="autoPlay">是否自动播放，默认为 true</param>
        /// <param name="firstFrameLoadCompleted">第一帧加载完成后的回调函数，默认为 null</param>
        public void OpenVideoFromStreamingAssets(string videoPathInStreamingAssets, bool autoPlay = true, Action firstFrameLoadCompleted = null)
        {
            videoRenderer.OpenVideoFromStreamingAssets(videoPathInStreamingAssets, autoPlay, firstFrameLoadCompleted);
        }
        
        /// <summary>
        /// 通过URL打开视频文件
        /// </summary>
        /// <param name="url">视频文件的完整URL路径</param>
        /// <param name="autoPlay">是否自动播放，默认为 true</param>
        /// <param name="firstFrameLoadCompleted">第一帧加载完成后的回调函数，默认为 null</param>
        public void OpenVideo(string url, bool autoPlay = true, Action firstFrameLoadCompleted = null)
        {
            videoRenderer.OpenVideo(url, autoPlay, firstFrameLoadCompleted);
        }
        
        /// <summary>
        /// 通过 VideoClip 资源打开视频
        /// </summary>
        /// <param name="clip">Unity 的 VideoClip 资源对象</param>
        /// <param name="autoPlay">是否自动播放，默认为 true</param>
        /// <param name="firstFrameLoadCompleted">第一帧加载完成后的回调函数，默认为 null</param>
        public void OpenVideo(VideoClip clip, bool autoPlay = true, Action firstFrameLoadCompleted = null)
        {
            videoRenderer.OpenVideo(clip, autoPlay, firstFrameLoadCompleted);
        }

        /// <summary>
        /// 关闭当前视频
        /// </summary>
        /// <param name="clearScreen">是否清空屏幕显示（释放 RenderTexture），默认为 true</param>
        public void CloseVideo(bool clearScreen = true)
        {
            videoRenderer.CloseVideo(clearScreen);
        }

        /// <summary>
        /// 播放视频
        /// </summary>
        public void Play()
        {
            videoRenderer.Play();
        }

        /// <summary>
        /// 暂停视频播放
        /// </summary>
        public void Pause()
        {
            videoRenderer.Pause();
        }

        /// <summary>
        /// 停止视频播放
        /// </summary>
        public void Stop()
        {
            videoRenderer.Stop();
        }

        /// <summary>
        /// 重新记录窗口模式的 Rect 信息，作为下次切换窗口时的状态
        /// 用于在窗口模式下调整布局后更新备份信息，确保切换回窗口模式时能正确恢复布局
        /// </summary>
        public void RecordWindowRect()
        {
            videoControl.settingControl.RecordWindowRect();
        }

    }

}
