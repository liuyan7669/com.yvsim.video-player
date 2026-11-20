using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Yvsim.Video
{
    /// <summary>
    /// 视频控制组件，统一管理视频播放器的所有UI控制功能
    /// 包含播放控制、音量控制、进度控制和设置控制四个子模块
    /// </summary>
    [DefaultExecutionOrder(-4000)]
    [Yvsim.InspectorGroups.HideScript]
    public class VideoControl : MonoBehaviour
    {
        /// <summary>
        /// 播放控制模块，管理播放、暂停、停止按钮
        /// </summary>
        [Tooltip("播放控制配置，绑定播放、暂停和停止按钮并管理其显示状态。")]
        public PlayControl playControl;
        
        /// <summary>
        /// 音量控制模块，管理音量滑块和静音开关
        /// </summary>
        [Tooltip("音量控制配置，绑定静音开关、音量滑块和音量图标。")]
        public VolumeControl volumeControl;
        
        /// <summary>
        /// 进度控制模块，管理进度条和时间显示
        /// </summary>
        [Tooltip("进度控制配置，绑定播放进度条和当前时间、总时长显示。")]
        public ProgressControl progressControl;
        
        /// <summary>
        /// 设置控制模块，管理全屏/窗口模式切换
        /// </summary>
        [Tooltip("视频显示区域的全屏与窗口布局切换配置。")]
        public SettingControl settingControl;

        /// <summary>
        /// 进度条开始拖动时触发的事件
        /// </summary>
        public Action ProgressBeginDragAction;
        
        /// <summary>
        /// 进度条拖动过程中触发的事件，参数为当前进度值（0-1）
        /// </summary>
        public Action<float> ProgressDragAction;
        
        /// <summary>
        /// 进度条拖动结束时触发的事件
        /// </summary>
        public Action ProgressEndDragAction;

        /// <summary>
        /// 创建并初始化所有控制模块
        /// 依次初始化播放控制、音量控制、进度控制和设置控制
        /// </summary>
        public void CreateControl()
        {
            playControl.Init();
            volumeControl.Init();
            progressControl.Init();
            settingControl.Init();
        }

        /// <summary>
        /// 进度条开始拖动时调用
        /// 触发 ProgressBeginDragAction 事件
        /// </summary>
        public void ProgressBeginDrag()
        {
            ProgressBeginDragAction?.Invoke();
        }
        
        /// <summary>
        /// 进度条拖动过程中调用
        /// 获取当前进度条的值并触发 ProgressDragAction 事件
        /// </summary>
        public void ProgressDrag()
        {
            ProgressDragAction?.Invoke(progressControl.progressSlider.value);
        }
        
        /// <summary>
        /// 进度条拖动结束时调用
        /// 触发 ProgressEndDragAction 事件
        /// </summary>
        public void ProgressEndDrag()
        {
            ProgressEndDragAction?.Invoke();
        }

    }

    /// <summary>
    /// 播放控制类，管理视频的播放、暂停、停止功能
    /// 控制播放、暂停、停止三个按钮的显示状态和点击事件
    /// </summary>
    [Serializable]
    public class PlayControl
    {
        /// <summary>
        /// 播放按钮，点击后开始播放视频
        /// </summary>
        [Tooltip("开始或继续播放视频的按钮；初始化时自动绑定点击事件。")]
        [SerializeField] private Button playButton;
        
        /// <summary>
        /// 暂停按钮，点击后暂停视频播放
        /// </summary>
        [Tooltip("暂停视频播放的按钮；初始化时自动绑定点击事件。")]
        [SerializeField] private Button pauseButton;
        
        /// <summary>
        /// 停止按钮，点击后停止视频播放
        /// </summary>
        [Tooltip("停止视频播放的按钮；初始化时自动绑定点击事件。")]
        [SerializeField] private Button stopButton;

        /// <summary>
        /// 播放按钮被点击时触发的事件
        /// </summary>
        public Action OnPlay;
        
        /// <summary>
        /// 暂停按钮被点击时触发的事件
        /// </summary>
        public Action OnPause;
        
        /// <summary>
        /// 停止按钮被点击时触发的事件
        /// </summary>
        public Action OnStop;

        /// <summary>
        /// 初始化播放控制
        /// 设置按钮的初始显示状态，绑定按钮点击事件
        /// </summary>
        public void Init()
        {
            playButton.gameObject.SetActive(true);
            pauseButton.gameObject.SetActive(false);
            stopButton.gameObject.SetActive(false);
            playButton.onClick.RemoveAllListeners();
            pauseButton.onClick.RemoveAllListeners();
            stopButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(() =>
            {
                playButton.gameObject.SetActive(false);
                pauseButton.gameObject.SetActive(true);
                stopButton.gameObject.SetActive(true);
                OnPlay?.Invoke();
            });
            pauseButton.onClick.AddListener(() =>
            {
                playButton.gameObject.SetActive(true);
                pauseButton.gameObject.SetActive(false);
                stopButton.gameObject.SetActive(true);
                OnPause?.Invoke();
            });
            stopButton.onClick.AddListener(() =>
            {
                playButton.gameObject.SetActive(true);
                pauseButton.gameObject.SetActive(false);
                stopButton.gameObject.SetActive(false);
                OnStop?.Invoke();
            });
        }

        /// <summary>
        /// 切换到播放状态
        /// 暂停时显示播放和停止按钮，隐藏暂停按钮
        /// </summary>
        public void SwitchToPlayState()
        {
            playButton.gameObject.SetActive(true);
            pauseButton.gameObject.SetActive(false);
            stopButton.gameObject.SetActive(true);
        }
        
        /// <summary>
        /// 切换到暂停状态
        /// 播放时显示暂停和停止按钮，隐藏播放按钮
        /// </summary>
        public void SwitchToPauseState()
        {
            playButton.gameObject.SetActive(false);
            pauseButton.gameObject.SetActive(true);
            stopButton.gameObject.SetActive(true);
        }
        
        /// <summary>
        /// 切换到停止状态
        /// 停止时显示播放按钮，隐藏暂停和停止按钮
        /// </summary>
        public void SwitchToStopState()
        {
            playButton.gameObject.SetActive(true);
            pauseButton.gameObject.SetActive(false);
            stopButton.gameObject.SetActive(false);
        }

    }

    /// <summary>
    /// 音量控制类，管理视频的音量大小和静音功能
    /// 根据音量大小显示不同的音量图标，支持静音/取消静音切换
    /// </summary>
    [Serializable]
    public class VolumeControl
    {
        /// <summary>
        /// 静音开关 Toggle 组件
        /// </summary>
        [Tooltip("切换视频静音状态的 Toggle，勾选表示静音。")]
        public Toggle muteToggle;
        
        /// <summary>
        /// 音量滑块 Slider 组件，范围通常为 0-1
        /// </summary>
        [Tooltip("调整视频音量的 Slider，数值范围应设为 0–1。")]
        public Slider volumeSlider;
        
        /// <summary>
        /// 音量图标 Image 组件，根据音量大小显示不同的图标
        /// </summary>
        [Tooltip("显示当前静音或音量等级图标的 Image。")]
        public Image horn;
        
        /// <summary>
        /// 音量图标精灵列表，按音量大小顺序排列
        /// 索引0：静音图标
        /// 索引1：低音量图标（0-30%）
        /// 索引2：中音量图标（30%-60%）
        /// 索引3：高音量图标（60%以上）
        /// </summary>
        [Tooltip("依次配置 4 个图标：静音、低音量（不超过 30%）、中音量（30%–60%）、高音量（超过 60%）。")]
        public List<Sprite> hornSprites = new List<Sprite>();
        
        /// <summary>
        /// 音量改变时触发的事件，参数为新的音量值（0-1）
        /// </summary>
        public Action<float> VolumeChanged;
        
        /// <summary>
        /// 静音状态改变时触发的事件，参数为是否静音
        /// </summary>
        public Action<bool> MuteChanged;

        /// <summary>
        /// 临时保存的音量值，用于取消静音时恢复音量
        /// </summary>
        [Tooltip("暂存最近一次设置的音量，供取消静音时恢复，运行时会自动更新。")]
        [SerializeField] private float tempVolume;

        /// <summary>
        /// 初始化音量控制
        /// 绑定静音开关和音量滑块的值改变事件
        /// </summary>
        public void Init()
        {
            muteToggle.onValueChanged.RemoveAllListeners();
            muteToggle.onValueChanged.AddListener((isOn) =>
            {
                Mute(isOn);
            });
            volumeSlider.onValueChanged.RemoveAllListeners();
            volumeSlider.onValueChanged.AddListener((value) =>
            {
                UpdateVolume(value);
            });
        }

        /// <summary>
        /// 设置静音状态
        /// 如果静音，显示静音图标；如果取消静音，恢复之前保存的音量值
        /// </summary>
        /// <param name="mute">是否静音，true 表示静音，false 表示取消静音</param>
        /// <param name="triggerAction">是否触发 MuteChanged 事件，默认为 true。false 用于避免循环调用</param>
        public void Mute(bool mute, bool triggerAction = true)
        {
            if (mute)
            {
                horn.sprite = hornSprites[0];
            }
            else
            {
                UpdateVolume(tempVolume, triggerAction);
            }
            if (triggerAction)
            {
                muteToggle.isOn = mute;
            }
            else
            {
                muteToggle.SetIsOnWithoutNotify(mute);
            }

            if (triggerAction)
            {
                MuteChanged?.Invoke(mute);
            }
        }

        /// <summary>
        /// 更新音量值
        /// 根据音量大小更新音量图标，如果当前处于静音状态则自动取消静音
        /// </summary>
        /// <param name="volume">新的音量值（0-1），0 表示静音，1 表示最大音量</param>
        /// <param name="triggerAction">是否触发 VolumeChanged 事件，默认为 true。false 用于避免循环调用</param>
        public void UpdateVolume(float volume, bool triggerAction = true)
        {
            if (muteToggle.isOn)
            {
                if (triggerAction)
                {
                    muteToggle.isOn = false;
                }
                else
                {
                    muteToggle.SetIsOnWithoutNotify(false);
                }
            }
            if (!triggerAction)
            {
                volumeSlider.SetValueWithoutNotify(volume);
            }
            this.tempVolume = volume;
            if (volume == 0)
            {
                horn.sprite = hornSprites[0];
            }
            else if (volume > 0 && volume <= 0.3f)
            {
                horn.sprite = hornSprites[1];
            }
            else if (volume > 0.3f && volume <= 0.6f)
            {
                horn.sprite = hornSprites[2];
            }
            else if (volume > 0.6f)
            {
                horn.sprite = hornSprites[3];
            }
            if (triggerAction)
            {
                VolumeChanged?.Invoke(volume);
            }
        }
    }

    /// <summary>
    /// 进度控制类，管理视频播放进度条和时间显示
    /// 显示当前播放时间和总时长，支持拖动进度条跳转播放位置
    /// </summary>
    [Serializable]
    public class ProgressControl
    {
        /// <summary>
        /// 进度条 Slider 组件，显示和设置播放进度（0-1）
        /// </summary>
        [Tooltip("显示视频播放进度的 Slider，数值范围应设为 0–1；拖拽事件需连接到 VideoControl 对应方法。")]
        public Slider progressSlider;
        
        /// <summary>
        /// 时间文本 Text 组件，显示当前时间/总时长的格式（如：00:01:23/00:05:00）
        /// </summary>
        [Tooltip("显示当前播放时间与总时长的 Text，格式为 时:分:秒/时:分:秒。")]
        public Text timeText;
        /// <summary>
        /// 初始化进度控制
        /// 绑定进度条的值改变事件，初始化时间文本显示
        /// </summary>
        public void Init()
        {
            progressSlider.onValueChanged.RemoveAllListeners();
            progressSlider.onValueChanged.AddListener((value) =>
            {
                //timeText.text = $"{(int)(value * 100)}%";
            });

            UpdateTimeText(0, 0);
        }

        /// <summary>
        /// 更新播放进度
        /// 更新进度条的值和时间文本显示，会自动处理无效值和边界情况
        /// </summary>
        /// <param name="value">当前播放时间（单位：秒）</param>
        /// <param name="maxValue">视频总时长（单位：秒）</param>
        public void UpdateProgress(double value, double maxValue)
        {
            if (progressSlider != null)
            {
                // 处理无效或负值
                if (double.IsNaN(value) || double.IsInfinity(value) || value < 0f)
                    value = 0f;
                if (double.IsNaN(maxValue) || double.IsInfinity(maxValue) || maxValue <= 0f)
                    maxValue = 1f; // 避免除以零
                progressSlider.value = Mathf.Clamp01((float)(value / maxValue));
            }
            UpdateTimeText(value, maxValue);
        }

        /// <summary>
        /// 更新时间文本显示
        /// 将秒数转换为时:分:秒格式，并显示为"当前时间/总时长"的格式
        /// </summary>
        /// <param name="value">当前播放时间（单位：秒）</param>
        /// <param name="maxValue">视频总时长（单位：秒）</param>
        private void UpdateTimeText(double value, double maxValue)
        {
            value = Math.Min(value, maxValue);

            TimeSpan tsValue = TimeSpan.FromSeconds(value);
            TimeSpan tsMax = TimeSpan.FromSeconds(maxValue);

            string valueStr = string.Format("{0:D2}:{1:D2}:{2:D2}",
                tsValue.Hours, tsValue.Minutes, tsValue.Seconds);
            string maxStr = string.Format("{0:D2}:{1:D2}:{2:D2}",
                tsMax.Hours, tsMax.Minutes, tsMax.Seconds);

            timeText.text = valueStr + "/" + maxStr;
        }
    }

    /// <summary>
    /// 设置控制类，管理全屏/窗口模式的切换
    /// 保存窗口模式的 RectTransform 信息，支持在全屏和窗口模式之间切换
    /// </summary>
    [Serializable]
    public class SettingControl
    {
        /// <summary>
        /// RectTransform 备份类，用于保存和恢复 RectTransform 的布局信息
        /// 在全屏切换时保存窗口模式的布局，以便切换回窗口模式时恢复
        /// </summary>
        public class RectTransformBackup
        {
            /// <summary>
            /// 标识是否已记录过窗口模式的布局信息
            /// </summary>
            public bool isRecord = false;
            
            /// <summary>
            /// 保存的锚点最小值（左下角）
            /// </summary>
            public Vector2 anchorMin;
            
            /// <summary>
            /// 保存的锚点最大值（右上角）
            /// </summary>
            public Vector2 anchorMax;
            
            /// <summary>
            /// 保存的锚点位置
            /// </summary>
            public Vector2 anchoredPosition;
            
            /// <summary>
            /// 保存的大小增量
            /// </summary>
            public Vector2 sizeDelta;
            
            /// <summary>
            /// 构造函数，从指定的 RectTransform 创建备份
            /// </summary>
            /// <param name="rect">要备份的 RectTransform 组件</param>
            public RectTransformBackup(RectTransform rect)
            {
                anchorMin = rect.anchorMin;
                anchorMax = rect.anchorMax;
                anchoredPosition = rect.anchoredPosition;
                sizeDelta = rect.sizeDelta;
            }
            
            /// <summary>
            /// 将保存的布局信息恢复到指定的 RectTransform
            /// </summary>
            /// <param name="rect">要恢复的 RectTransform 组件</param>
            public void Reset(RectTransform rect)
            {
                rect.anchorMin = anchorMin;
                rect.anchorMax = anchorMax;
                rect.anchoredPosition = anchoredPosition;
                rect.sizeDelta = sizeDelta;
            }
        }

        /// <summary>
        /// 要控制的 RectTransform 组件，用于全屏/窗口模式切换
        /// </summary>
        [Tooltip("需要在全屏布局与原窗口布局之间切换的视频显示区域。")]
        public RectTransform rect;
        
        /// <summary>
        /// 全屏按钮，点击后切换到全屏模式
        /// </summary>
        [Tooltip("将视频显示区域扩展为全屏布局的按钮，不负责切换操作系统窗口模式。")]
        public Button fullButton;
        
        /// <summary>
        /// 窗口按钮，点击后切换到窗口模式
        /// </summary>
        [Tooltip("将视频显示区域恢复为之前窗口布局的按钮。")]
        public Button winButton;

        /// <summary>
        /// 全屏/窗口模式切换时触发的事件，参数为是否全屏（true=全屏，false=窗口）
        /// </summary>
        public Action<bool> SwitchChanged;

        /// <summary>
        /// 窗口模式的 RectTransform 备份，用于切换回窗口模式时恢复布局
        /// </summary>
        private RectTransformBackup rectBackup;

        /// <summary>
        /// 初始化设置控制
        /// 创建窗口模式的布局备份，绑定全屏/窗口按钮的点击事件
        /// </summary>
        public void Init()
        {
            rectBackup = new RectTransformBackup(rect);

            fullButton.gameObject.SetActive(true);
            winButton.gameObject.SetActive(false);
            fullButton.onClick.RemoveAllListeners();
            fullButton.onClick.AddListener(() =>
            {
                SwitchToFull();
            });
            winButton.onClick.RemoveAllListeners();
            winButton.onClick.AddListener(() =>
            {
                SwitchToWindow();
            });
        }

        /// <summary>
        /// 重新记录窗口模式的 Rect 信息，作为下次切换窗口时的状态
        /// 用于在窗口模式下调整布局后更新备份信息
        /// </summary>
        public void RecordWindowRect()
        {
            rectBackup = new RectTransformBackup(rect);
        }

        /// <summary>
        /// 切换到窗口模式
        /// 显示全屏按钮，隐藏窗口按钮，恢复之前保存的窗口布局
        /// </summary>
        /// <param name="triggerAction">是否触发 SwitchChanged 事件，默认为 true。false 用于避免循环调用</param>
        public void SwitchToWindow(bool triggerAction = true)
        {
            fullButton.gameObject.SetActive(true);
            winButton.gameObject.SetActive(false);
            if (rectBackup != null)
            {
                rectBackup.Reset(rect);
            }
            else
            {
                rectBackup = new RectTransformBackup(rect);
            }
            if (triggerAction)
            {
                SwitchChanged?.Invoke(false);
            }
        }

        /// <summary>
        /// 切换到全屏模式
        /// 显示窗口按钮，隐藏全屏按钮，将 RectTransform 设置为全屏布局（铺满整个父容器）
        /// 在切换前会保存当前窗口布局（如果尚未保存）
        /// </summary>
        /// <param name="triggerAction">是否触发 SwitchChanged 事件，默认为 true。false 用于避免循环调用</param>
        public void SwitchToFull(bool triggerAction = true)
        {
            if (rectBackup == null)
            {
                rectBackup = new RectTransformBackup(rect);
            }
            else
            {
                if (!rectBackup.isRecord)
                {
                    rectBackup = new RectTransformBackup(rect);
                    rectBackup.isRecord = true;
                }
            }

            fullButton.gameObject.SetActive(false);
            winButton.gameObject.SetActive(true);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            if (triggerAction)
            {
                SwitchChanged?.Invoke(true);
            }
        }

    }

}
