using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

namespace Yvsim.Video
{
    /// <summary>
    /// 视频渲染器组件，负责视频播放、渲染和控制的统一管理
    /// 使用 RenderTexture 将视频渲染到 RawImage 上，并提供播放、暂停、进度控制等功能
    /// </summary>
    [DefaultExecutionOrder(-5000)]
    [Yvsim.InspectorGroups.HideScript]
    public class VideoRenderer : MonoBehaviour
    {
        /// <summary>
        /// 用于显示视频画面的 RawImage 组件
        /// </summary>
        [Tooltip("用于显示视频画面的 RawImage，运行时会绑定生成的视频渲染纹理。")]
        [SerializeField] private RawImage rawImage;
        
        /// <summary>
        /// Unity 的视频播放器组件，负责实际的视频解码和播放
        /// </summary>
        [Tooltip("负责视频解码和播放的 Unity VideoPlayer 组件。")]
        [SerializeField] private VideoPlayer videoPlayer;
        
        /// <summary>
        /// 用于渲染视频帧的 RenderTexture，视频内容会被渲染到此纹理上
        /// </summary>
        private RenderTexture renderTexture;
        
        /// <summary>
        /// 视频控制界面组件，包含播放控制、进度控制、音量控制等UI元素
        /// </summary>
        private VideoControl control;

        /// <summary>
        /// 视频播放监听协程，用于持续更新播放进度
        /// </summary>
        private IEnumerator videoPlayListener;
        
        /// <summary>
        /// 标识当前是否正在通过进度条控制视频进度
        /// 当为 true 时，暂停自动更新进度条显示
        /// </summary>
        private bool sliderControlling = false;
        
        /// <summary>
        /// 跳帧完成监听协程，用于检测拖动进度条后的跳帧操作是否完成
        /// </summary>
        private IEnumerator frameJumpFinishListener;

        // A canceled preparation must not pause a later video through a stale callback.
        private VideoPlayer.FrameReadyEventHandler firstFrameReadyHandler;
        
        /// <summary>
        /// 判断在拖动进度条时或跳帧时是否真正完成的间隔（以帧为单位）
        /// 一般30f=1秒，也就是1秒30帧。当实际帧与目标帧的差值小于此值时，认为跳帧完成
        /// </summary>
        [Yvsim.InspectorGroups.InfoBox("判断在拖动进度条时或跳帧时是否真正完成的间隔，一般30f=1秒，也就是1秒30帧")]
        [Tooltip("判断进度跳转已到达目标位置的帧差容差，单位为帧。值越大越容易判定完成；实际时间取决于视频帧率。")]
        [SerializeField, Min(1f)] private float jumpFinishInterval = 30f;
        
        /// <summary>
        /// 标识当前是否为全屏模式
        /// </summary>
        private bool isFullScreen = false;

        /// <summary>
        /// 获取或设置全屏状态
        /// </summary>
        public bool FullScreen { get { return isFullScreen; } set { SetFullScreen(value, false); } }

        /// <summary>
        /// 获取或设置静音状态
        /// </summary>
        public bool Mute { get { return GetMute(); } set { SetMute(value, false); } }

        /// <summary>
        /// 获取或设置音量大小（范围：0.0f - 1.0f）
        /// </summary>
        public float Volume { get { return GetVolume(); } set { SetVolume(value, false); } }

        /// <summary>
        /// 获取视频总时长（单位：秒）
        /// 通过总帧数除以帧率计算得出精确时长
        /// </summary>
        public double Length
        {
            get
            {
                return videoPlayer != null && videoPlayer.frameRate > 0f
                    ? (double)videoPlayer.frameCount / videoPlayer.frameRate
                    : 0d;
            }
        }

        /// <summary>
        /// 获取或设置当前播放时间（单位：秒）
        /// 获取时返回当前帧对应的时间加上一帧的时长
        /// 设置时会跳转到指定的时间位置
        /// </summary>
        public double CurrentTime
        {
            get
            {
                if (videoPlayer == null || videoPlayer.frameRate <= 0f || videoPlayer.frame < 0)
                    return 0d;

                double frameDuration = 1.0f / videoPlayer.frameRate;
                double cTime = (double)videoPlayer.frame / videoPlayer.frameRate + frameDuration;
                return cTime;
                //double frameDuration = 1.0f / videoPlayer.frameRate;
                //return (double)(videoPlayer.frame + videoPlayer.frameRate) / videoPlayer.frameRate;
            }
            set
            {
                // 确保视频已准备好
                if (videoPlayer == null || !videoPlayer.isPrepared)
                    return;

                // 将秒数转换为归一化值（0-1）
                double length = Length;
                if (length <= 0)
                    return;

                float normalization = (float)(value / length);

                // 调用 ChangedProgress 来设置进度
                ChangedProgress(normalization);
            }
        }

        /// <summary>
        /// 获取当前是否正在播放视频
        /// </summary>
        public bool IsPlaying { get; private set; }

        /// <summary>
        /// 获取当前是否处于暂停状态
        /// </summary>
        public bool IsPause { get; private set; }

        /// <summary>
        /// 视频播放结束时的回调事件
        /// 当视频播放到结尾时会触发此事件
        /// </summary>
        public Action PlayEndAction { get; set; }

        /// <summary>
        /// 创建并初始化视频渲染器
        /// 设置视频播放器参数，绑定控制UI的事件回调，创建 RenderTexture
        /// </summary>
        /// <param name="control">视频控制UI组件，包含播放、进度、音量、设置等控制元素</param>
        public void CreateRenderer(VideoControl control)
        {
            this.control = control;
            control.CreateControl();
            control.playControl.OnPlay = Play;
            control.playControl.OnPause = Pause;
            control.playControl.OnStop = Stop;
            control.ProgressBeginDragAction = () =>
            {
                if (frameJumpFinishListener != null) { StopCoroutine(frameJumpFinishListener); }
                sliderControlling = true;
                videoPlayer.Pause();
            };
            control.ProgressDragAction = (float value) =>
            {
                ChangedProgress(value);
            };
            control.ProgressEndDragAction = () =>
            {
                if (frameJumpFinishListener != null) { StopCoroutine(frameJumpFinishListener); }
                frameJumpFinishListener = FrameJumpFinishListener(control.progressControl.progressSlider.value);
                StartCoroutine(frameJumpFinishListener);
            };


            control.volumeControl.MuteChanged = (bool mute) =>
            {
                SetMute(mute, false);
            };
            control.volumeControl.VolumeChanged = (float volume) =>
            {
                SetVolume(volume, false);
            };
            Mute = false;
            Volume = 1;

            control.settingControl.SwitchChanged = (bool value) =>
            {
                SetFullScreen(value, false);
            };

            videoPlayer.skipOnDrop = true;//必须设置为true
            videoPlayer.waitForFirstFrame = true;
            videoPlayer.playOnAwake = false;
            videoPlayer.loopPointReached -= PlayEnd;
            videoPlayer.loopPointReached += PlayEnd;

            ApplyRTToVideo(1920, 1080);
        }

        /// <summary>
        /// 从 StreamingAssets 文件夹打开视频文件
        /// </summary>
        /// <param name="videoPathInStreamingAssets">视频文件在 StreamingAssets 文件夹中的相对路径</param>
        /// <param name="autoPlay">是否自动播放，默认为 true</param>
        /// <param name="firstFrameLoadCompleted">第一帧加载完成后的回调函数，默认为 null</param>
        public void OpenVideoFromStreamingAssets(string videoPathInStreamingAssets, bool autoPlay = true, Action firstFrameLoadCompleted = null)
        {
            string url = Path.Combine(Application.streamingAssetsPath, videoPathInStreamingAssets);
            OpenVideo(url, autoPlay, firstFrameLoadCompleted);
        }
        
        /// <summary>
        /// 通过URL打开视频文件
        /// 会先关闭当前视频，然后准备新视频并缓冲第一帧
        /// </summary>
        /// <param name="url">视频文件的完整URL路径</param>
        /// <param name="autoPlay">是否自动播放，默认为 true</param>
        /// <param name="firstFrameLoadCompleted">第一帧加载完成后的回调函数，默认为 null</param>
        public void OpenVideo(string url, bool autoPlay = true, Action firstFrameLoadCompleted = null)
        {
            CloseVideo();
            StopAllCoroutines();
            StartCoroutine(PrepareVideoAndBufferFirstFrame(videoPlayer, url, (tex) =>
            {
                CompletePreparation(autoPlay, firstFrameLoadCompleted);
            }));
        }
        /// <summary>
        /// 通过 VideoClip 资源打开视频
        /// 会先关闭当前视频，然后准备新视频并缓冲第一帧
        /// </summary>
        /// <param name="clip">Unity 的 VideoClip 资源对象</param>
        /// <param name="autoPlay">是否自动播放，默认为 true</param>
        /// <param name="firstFrameLoadCompleted">第一帧加载完成后的回调函数，默认为 null</param>
        public void OpenVideo(VideoClip clip, bool autoPlay = true, Action firstFrameLoadCompleted = null)
        {
            CloseVideo();
            StopAllCoroutines();
            StartCoroutine(PrepareVideoAndBufferFirstFrame(videoPlayer, clip, (tex) =>
            {
                CompletePreparation(autoPlay, firstFrameLoadCompleted);
            }));
        }

        private void CompletePreparation(bool autoPlay, Action firstFrameLoadCompleted)
        {
            if (autoPlay)
                Play();
            // The callback may stop, close, or replace this video. Do not resume the old request afterward.
            firstFrameLoadCompleted?.Invoke();
        }

        /// <summary>
        /// 关闭当前视频
        /// </summary>
        /// <param name="clearScreen">是否清空屏幕显示（释放 RenderTexture），默认为 true</param>
        public void CloseVideo(bool clearScreen = true)
        {
            Stop();

            if (clearScreen && renderTexture != null)
            {
                renderTexture.Release();
            }
        }

        /// <summary>
        /// 播放视频
        /// 设置播放状态，更新UI显示，启动播放监听协程
        /// </summary>
        public void Play()
        {
            if (videoPlayListener != null)
            {
                StopCoroutine(videoPlayListener);
            }
            IsPlaying = true;
            IsPause = false;
            control.playControl.SwitchToPauseState();
            videoPlayer.Play();

            videoPlayListener = VideoPlayListener();
            StartCoroutine(videoPlayListener);
        }

        /// <summary>
        /// 暂停视频播放
        /// 设置暂停状态，更新UI显示，暂停 VideoPlayer
        /// </summary>
        public void Pause()
        {
            IsPlaying = false;
            IsPause = true;
            control.playControl.SwitchToPlayState();
            videoPlayer.Pause();
        }

        /// <summary>
        /// 停止视频播放
        /// 停止 VideoPlayer，重置所有状态，停止所有协程，更新UI显示
        /// </summary>
        public void Stop()
        {
            ClearFirstFrameHandler();
            if (videoPlayer != null)
                videoPlayer.Stop();
            sliderControlling = false;
            IsPlaying = false;
            IsPause = false;
            if (control != null)
                control.playControl.SwitchToStopState();
            StopAllCoroutines();
        }


        /// <summary>
        /// 准备视频并缓冲第一帧（URL方式）
        /// 协程方法，用于异步准备视频资源，等待第一帧渲染完成后再继续
        /// </summary>
        /// <param name="vp">要使用的 VideoPlayer 组件</param>
        /// <param name="url">视频文件的URL路径</param>
        /// <param name="onPrepared">第一帧准备完成后的回调函数，参数为渲染好的纹理</param>
        /// <returns>协程迭代器</returns>
        private IEnumerator PrepareVideoAndBufferFirstFrame(VideoPlayer vp, string url, System.Action<Texture> onPrepared)
        {
            //vp.waitForFirstFrame = true;  // 等待第一帧 GPU 渲染
            //vp.skipOnDrop = false;        // 第一帧不要被跳过
            //vp.playOnAwake = false;       // 手动控制播放
            vp.source = VideoSource.Url;
            vp.url = url;
            vp.sendFrameReadyEvents = true;

            // 使用已有动态创建的 RenderTexture
            vp.renderMode = VideoRenderMode.RenderTexture;
            //vp.targetTexture = renderTexture;

            bool firstFrameReady = false;

            /// <summary>
            /// frameReady 回调函数，确保第一帧被 GPU 渲染
            /// 当视频帧准备好时被 VideoPlayer 调用，用于检测第一帧是否已准备好
            /// </summary>
            /// <param name="source">触发事件的 VideoPlayer 组件</param>
            /// <param name="frameIdx">准备好的帧索引</param>
            void OnFrameReady(VideoPlayer source, long frameIdx)
            {
                if (!firstFrameReady)
                {
                    firstFrameReady = true;
                    source.Pause(); // 停在第一帧
                }
            }

            ClearFirstFrameHandler();
            firstFrameReadyHandler = OnFrameReady;
            vp.frameReady += firstFrameReadyHandler;
            if (renderTexture != null && !renderTexture.IsCreated())
                renderTexture.Create();

            vp.Prepare();
            while (!vp.isPrepared)
                yield return null;

            // 播放以触发第一帧渲染（即使第一帧不在时间0，VideoPlayer也会自动处理）
            vp.Play();

            // 等待第一帧准备完成
            while (!firstFrameReady)
                yield return null;

            // 等待 GPU 提交
            yield return new WaitForEndOfFrame();
            yield return null;

            ClearFirstFrameHandler();

            // 给 RawImage 或材质赋值
            if (rawImage != null)
            {
                rawImage.texture = renderTexture;
            }

            //Debug.Log($"视频 [{videoPathInStreamingAssets}] 第一帧已缓冲，RenderTexture 保留！当前帧: {vp.frame}, 时间: {vp.time}");

            // 回调第一帧 RenderTexture
            onPrepared?.Invoke(vp.targetTexture);
        }
        /// <summary>
        /// 准备视频并缓冲第一帧（VideoClip方式）
        /// 协程方法，用于异步准备视频资源，等待第一帧渲染完成后再继续
        /// </summary>
        /// <param name="vp">要使用的 VideoPlayer 组件</param>
        /// <param name="clip">Unity 的 VideoClip 资源对象</param>
        /// <param name="onPrepared">第一帧准备完成后的回调函数，参数为渲染好的纹理</param>
        /// <returns>协程迭代器</returns>
        private IEnumerator PrepareVideoAndBufferFirstFrame(VideoPlayer vp, VideoClip clip, System.Action<Texture> onPrepared)
        {
            //vp.waitForFirstFrame = true;  // 等待第一帧 GPU 渲染
            //vp.skipOnDrop = false;        // 第一帧不要被跳过
            //vp.playOnAwake = false;       // 手动控制播放
            vp.source = VideoSource.VideoClip;
            vp.clip = clip;
            vp.sendFrameReadyEvents = true;

            // 使用已有动态创建的 RenderTexture
            vp.renderMode = VideoRenderMode.RenderTexture;
            //vp.targetTexture = renderTexture;

            bool firstFrameReady = false;

            /// <summary>
            /// frameReady 回调函数，确保第一帧被 GPU 渲染
            /// 当视频帧准备好时被 VideoPlayer 调用，用于检测第一帧是否已准备好
            /// </summary>
            /// <param name="source">触发事件的 VideoPlayer 组件</param>
            /// <param name="frameIdx">准备好的帧索引</param>
            void OnFrameReady(VideoPlayer source, long frameIdx)
            {
                if (!firstFrameReady)
                {
                    firstFrameReady = true;
                    source.Pause(); // 停在第一帧
                }
            }

            ClearFirstFrameHandler();
            firstFrameReadyHandler = OnFrameReady;
            vp.frameReady += firstFrameReadyHandler;
            if (renderTexture != null && !renderTexture.IsCreated())
                renderTexture.Create();

            vp.Prepare();
            while (!vp.isPrepared)
                yield return null;

            // 播放以触发第一帧渲染（即使第一帧不在时间0，VideoPlayer也会自动处理）
            vp.Play();

            // 等待第一帧准备完成
            while (!firstFrameReady)
                yield return null;

            // 等待 GPU 提交
            yield return new WaitForEndOfFrame();
            yield return null;

            ClearFirstFrameHandler();

            // 给 RawImage 或材质赋值
            if (rawImage != null)
            {
                rawImage.texture = renderTexture;
            }

            //Debug.Log($"视频 [{videoPathInStreamingAssets}] 第一帧已缓冲，RenderTexture 保留！当前帧: {vp.frame}, 时间: {vp.time}");

            // 回调第一帧 RenderTexture
            onPrepared?.Invoke(vp.targetTexture);
        }

        /// <summary>
        /// 视频播放监听协程
        /// 持续更新播放进度条显示，直到协程被停止
        /// </summary>
        /// <returns>协程迭代器</returns>
        private IEnumerator VideoPlayListener()
        {
            while (true)
            {
                if (!sliderControlling)
                {
                    control.progressControl.UpdateProgress(CurrentTime, Length);
                }
                yield return null;
            }
        }

        /// <summary>
        /// 跳帧完成监听协程
        /// 检测视频跳转到目标帧后是否真正完成，当实际帧与目标帧的差值小于 jumpFinishInterval 时认为完成
        /// </summary>
        /// <param name="normalization">目标位置的归一化值（0-1之间）</param>
        /// <returns>协程迭代器</returns>
        private IEnumerator FrameJumpFinishListener(float normalization)
        {
            if (videoPlayer == null || !videoPlayer.isPrepared || videoPlayer.frameCount == 0)
            {
                sliderControlling = false;
                yield break;
            }
            while (true)
            {
                long targetFrame = GetTargetFrame(normalization, videoPlayer.frameCount);
                float offset = videoPlayer.frame - targetFrame;
                float tolerance = float.IsNaN(jumpFinishInterval) ? 1f : Mathf.Max(1f, jumpFinishInterval);
                if (Mathf.Abs(offset) < tolerance)
                {
                    break;
                }
                yield return null;
            }
            sliderControlling = false;
            if (IsPlaying)
            {
                videoPlayer.Play();
            }
        }

        /// <summary>
        /// 改变视频播放进度
        /// 根据归一化值（0-1）计算目标帧并跳转，同时更新进度条显示
        /// </summary>
        /// <param name="normalization">目标位置的归一化值（0-1之间），会被自动限制在此范围内</param>
        private void ChangedProgress(float normalization)
        {
            // 确保视频已准备好
            if (videoPlayer == null || !videoPlayer.isPrepared ||
                videoPlayer.frameCount == 0 || videoPlayer.frameRate <= 0f)
                return;

            long targetFrame = GetTargetFrame(normalization, videoPlayer.frameCount);

            double frameDuration = 1.0f / videoPlayer.frameRate;
            double tempCurrentTime = (double)targetFrame / videoPlayer.frameRate + frameDuration;

            control.progressControl.UpdateProgress((float)tempCurrentTime, (float)Length);
            videoPlayer.frame = targetFrame;
        }

        private static long GetTargetFrame(float normalization, ulong frameCount)
        {
            if (frameCount == 0 || float.IsNaN(normalization))
                return 0;
            return (long)Math.Min((double)frameCount - 1d, Mathf.Clamp01(normalization) * (double)frameCount);
        }

        /// <summary>
        /// 获取视频音量
        /// 遍历所有音频轨道，返回最大的音量值
        /// </summary>
        /// <param name="trackIndex">音频轨道索引（当前未使用，保留用于未来扩展），默认为 0</param>
        /// <returns>音量值（0.0f - 1.0f）</returns>
        private float GetVolume(ushort trackIndex = 0)
        {
            float volume = 0.0f;
            for (int i = 0; i < videoPlayer.audioTrackCount; i++)
            {
                if (videoPlayer.GetDirectAudioVolume((ushort)i) > volume)
                {
                    volume = videoPlayer.GetDirectAudioVolume((ushort)i);
                }
            }
            return volume;
        }

        /// <summary>
        /// 设置视频音量
        /// 如果当前处于静音状态，会先取消静音，然后为所有音频轨道设置音量
        /// </summary>
        /// <param name="volume">音量值（0.0f - 1.0f）</param>
        /// <param name="triggerAction">是否触发音量控制UI的更新事件，false 表示不触发以避免循环调用</param>
        /// <param name="trackIndex">音频轨道索引（当前未使用，保留用于未来扩展），默认为 0</param>
        private void SetVolume(float volume, bool triggerAction, ushort trackIndex = 0)
        {
            if (Mute)
            {
                Mute = false;
            }
            for (int i = 0; i < videoPlayer.audioTrackCount; i++)
            {
                videoPlayer.SetDirectAudioVolume((ushort)i, volume);
            }
            control.volumeControl.UpdateVolume(volume, triggerAction);
        }

        /// <summary>
        /// 获取视频静音状态
        /// 检查所有音频轨道，如果所有轨道都静音则返回 true，否则返回 false
        /// </summary>
        /// <param name="trackIndex">音频轨道索引（当前未使用，保留用于未来扩展），默认为 0</param>
        /// <returns>是否静音，true 表示所有轨道都静音，false 表示至少有一个轨道未静音</returns>
        private bool GetMute(ushort trackIndex = 0)
        {
            bool mute = true;
            for (int i = 0; i < videoPlayer.audioTrackCount; i++)
            {
                if (!videoPlayer.GetDirectAudioMute((ushort)i))
                {
                    mute = false;
                    break;
                }
            }
            return mute;
        }

        /// <summary>
        /// 设置视频静音状态
        /// 为所有音频轨道设置静音状态
        /// </summary>
        /// <param name="mute">是否静音，true 表示静音，false 表示取消静音</param>
        /// <param name="triggerAction">是否触发音量控制UI的更新事件，false 表示不触发以避免循环调用</param>
        /// <param name="trackIndex">音频轨道索引（当前未使用，保留用于未来扩展），默认为 0</param>
        private void SetMute(bool mute, bool triggerAction, ushort trackIndex = 0)
        {
            for (int i = 0; i < videoPlayer.audioTrackCount; i++)
            {
                videoPlayer.SetDirectAudioMute((ushort)i, mute);
            }
            control.volumeControl.Mute(mute, triggerAction);
        }

        /// <summary>
        /// 设置全屏状态
        /// 更新全屏标志，并调用控制UI的相应方法切换全屏/窗口模式
        /// </summary>
        /// <param name="full">是否全屏，true 表示全屏，false 表示窗口模式</param>
        /// <param name="triggerAction">是否触发设置控制UI的更新事件，false 表示不触发以避免循环调用</param>
        private void SetFullScreen(bool full, bool triggerAction)
        {
            isFullScreen = full;
            if (full)
            {
                control.settingControl.SwitchToFull(triggerAction);
            }
            else
            {
                control.settingControl.SwitchToWindow(triggerAction);
            }
        }

        /// <summary>
        /// 视频播放结束回调
        /// 当视频播放到结尾时被 VideoPlayer 调用，先停止当前播放再触发结束事件
        /// </summary>
        /// <param name="source">触发事件的 VideoPlayer 组件</param>
        private void PlayEnd(VideoPlayer source)
        {
            //UnityEngine.Debug.Log("视频播放完成");
            // Stop the completed video before callbacks are allowed to open a new one.
            Stop();
            PlayEndAction?.Invoke();
        }

        private void OnDisable()
        {
            Stop();
        }

        private void OnDestroy()
        {
            ClearFirstFrameHandler();
            if (videoPlayer != null)
            {
                videoPlayer.loopPointReached -= PlayEnd;
                if (videoPlayer.targetTexture == renderTexture)
                    videoPlayer.targetTexture = null;
            }
            if (rawImage != null && rawImage.texture == renderTexture)
                rawImage.texture = null;
            if (renderTexture != null)
            {
                renderTexture.Release();
                if (Application.isPlaying)
                    Destroy(renderTexture);
                else
                    DestroyImmediate(renderTexture);
                renderTexture = null;
            }
        }

        private void ClearFirstFrameHandler()
        {
            if (videoPlayer != null && firstFrameReadyHandler != null)
                videoPlayer.frameReady -= firstFrameReadyHandler;
            firstFrameReadyHandler = null;
        }

        /// <summary>
        /// 为视频应用指定尺寸的 RenderTexture
        /// 如果已存在 RenderTexture，会先释放并销毁，然后创建新的并应用到 VideoPlayer 和 RawImage
        /// </summary>
        /// <param name="width">RenderTexture 的宽度（像素）</param>
        /// <param name="height">RenderTexture 的高度（像素）</param>
        private void ApplyRTToVideo(int width, int height)
        {
            if (videoPlayer.targetTexture != null)
            {// 释放旧的
                videoPlayer.targetTexture.Release();
                Destroy(videoPlayer.targetTexture);
            }

            renderTexture = CreateRT(width, height);

            videoPlayer.renderMode = VideoRenderMode.RenderTexture;
            videoPlayer.targetTexture = renderTexture;

            rawImage.texture = renderTexture;
        }

        /// <summary>
        /// 创建指定尺寸的 RenderTexture
        /// 创建一个新的 RenderTexture，禁用 MipMap，并立即创建纹理资源
        /// </summary>
        /// <param name="width">RenderTexture 的宽度（像素）</param>
        /// <param name="height">RenderTexture 的高度（像素）</param>
        /// <returns>创建好的 RenderTexture 对象</returns>
        private RenderTexture CreateRT(int width, int height)
        {
            RenderTexture rt = new RenderTexture(width, height, 0)
            {
                name = $"VideoRT_{width}x{height}",
                useMipMap = false,
                autoGenerateMips = false,
            };
            rt.Create();

            return rt;
        }

    }

}
