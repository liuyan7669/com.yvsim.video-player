using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Yvsim.Video;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;
using PackageInfo = UnityEditor.PackageManager.PackageInfo;

namespace Yvsim.VideoPlayerTests
{
    /// <summary>Checks control wiring and lifecycle boundaries without relying on a video decoder.</summary>
    public sealed class VideoControlTests
    {
        private GameObject root;
        private Button play;
        private Button pause;
        private Button stop;
        private VideoControl control;
        private VideoRenderer renderer;
        private VideoPlayer source;

        [SetUp]
        public void SetUp()
        {
            root = new GameObject("Video controls", typeof(RectTransform));
            play = Child<Button>("Play");
            pause = Child<Button>("Pause");
            stop = Child<Button>("Stop");
            control = root.AddComponent<VideoControl>();
            control.playControl = new PlayControl();
            SetField(control.playControl, "playButton", play);
            SetField(control.playControl, "pauseButton", pause);
            SetField(control.playControl, "stopButton", stop);
            control.volumeControl = new VolumeControl
            {
                muteToggle = Child<Toggle>("Mute"),
                volumeSlider = Child<Slider>("Volume"),
                horn = Child<Image>("Horn"),
                hornSprites = new List<Sprite> { null, null, null, null }
            };
            control.progressControl = new ProgressControl
            {
                progressSlider = Child<Slider>("Progress"),
                timeText = Child<Text>("Time")
            };
            control.settingControl = new SettingControl
            {
                rect = root.GetComponent<RectTransform>(),
                fullButton = Child<Button>("Full"),
                winButton = Child<Button>("Window")
            };
            source = root.AddComponent<VideoPlayer>();
            renderer = root.AddComponent<VideoRenderer>();
            SetField(renderer, "videoPlayer", source);
            SetField(renderer, "rawImage", Child<RawImage>("Video"));
        }

        [TearDown]
        public void TearDown() => UnityEngine.Object.DestroyImmediate(root);

        [Test]
        public void StopIsAvailableDuringPlaybackAndPause()
        {
            control.playControl.Init();
            Assert.That(stop.gameObject.activeSelf, Is.False);
            play.onClick.Invoke();
            Assert.That(stop.gameObject.activeSelf, Is.True);
            pause.onClick.Invoke();
            Assert.That(stop.gameObject.activeSelf, Is.True);
            bool stopped = false;
            control.playControl.OnStop = () => stopped = true;
            stop.onClick.Invoke();
            Assert.That(stopped, Is.True);
            Assert.That(stop.gameObject.activeSelf, Is.False);
            Assert.That(play.gameObject.activeSelf, Is.True);
        }

        [Test]
        public void RendererConnectsStopButtonToPlaybackStop()
        {
            renderer.CreateRenderer(control);
            renderer.Pause();
            Assert.That(renderer.IsPause, Is.True);
            stop.onClick.Invoke();
            Assert.That(renderer.IsPause, Is.False);
            Assert.That(renderer.IsPlaying, Is.False);
        }

        [Test]
        public void UnpreparedVideoReportsFiniteZeroTimes()
        {
            Assert.That(renderer.Length, Is.EqualTo(0d));
            Assert.That(renderer.CurrentTime, Is.EqualTo(0d));
        }

        [Test]
        public void StopDetachesCanceledFirstFrameHandler()
        {
            bool called = false;
            VideoPlayer.FrameReadyEventHandler handler = (_, __) => called = true;
            source.frameReady += handler;
            SetField(renderer, "firstFrameReadyHandler", handler);
            renderer.Stop();

            // Invoke the publisher's remaining callbacks, as a late native frame would.
            FieldInfo eventField = typeof(VideoPlayer).GetField("frameReady", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(eventField, Is.Not.Null);
            var remaining = (VideoPlayer.FrameReadyEventHandler)eventField.GetValue(source);
            remaining?.Invoke(source, 0);
            Assert.That(called, Is.False);
        }

        [Test]
        public void EndCallbackObservesStoppedStateAndCanStartNextAction()
        {
            renderer.CreateRenderer(control);
            renderer.Pause();
            renderer.PlayEndAction = () =>
            {
                Assert.That(renderer.IsPause, Is.False);
                renderer.Pause();
            };
            typeof(VideoRenderer).GetMethod("PlayEnd", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(renderer, new object[] { source });
            Assert.That(renderer.IsPause, Is.True, "Completion must not stop a new callback action.");
        }

        [TestCase("stop")]
        [TestCase("close")]
        [TestCase("replace")]
        public void FirstFrameCallbackCanOverrideAutomaticPlayback(string action)
        {
            renderer.CreateRenderer(control);
            string packagePath = PackageInfo.FindForAssembly(typeof(VideoRenderer).Assembly).resolvedPath;
            source.source = VideoSource.Url;
            source.url = new Uri(Path.Combine(packagePath, "Samples~/Video Player/Examples/mov_bbb.mp4")).AbsoluteUri;
            bool callbackRan = false;
            Action callback = () =>
            {
                callbackRan = true;
                Assert.That(renderer.IsPlaying, Is.True, "Auto-play must settle before publishing completion.");
                if (action == "stop") renderer.Stop();
                else if (action == "close") renderer.CloseVideo();
                else renderer.OpenVideo(source.url, autoPlay: false);
            };

            typeof(VideoRenderer).GetMethod("CompletePreparation", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(renderer, new object[] { true, callback });

            Assert.That(callbackRan, Is.True);
            Assert.That(renderer.IsPlaying, Is.False, "The completed request must not restart after its callback.");
        }

        [TestCase(0f, 120UL, 0L)]
        [TestCase(0.5f, 120UL, 60L)]
        [TestCase(1f, 120UL, 119L)]
        [TestCase(2f, 120UL, 119L)]
        [TestCase(-1f, 120UL, 0L)]
        [TestCase(1f, 0UL, 0L)]
        public void SeekTargetStaysInsideAvailableFrames(float progress, ulong count, long expected)
        {
            object frame = typeof(VideoRenderer).GetMethod("GetTargetFrame", BindingFlags.Static | BindingFlags.NonPublic)
                .Invoke(null, new object[] { progress, count });
            Assert.That(frame, Is.EqualTo(expected));
        }

        [Test]
        public void EndingDragWithoutPreparedVideoDoesNotWaitForever()
        {
            SetField(renderer, "sliderControlling", true);
            var wait = (IEnumerator)typeof(VideoRenderer)
                .GetMethod("FrameJumpFinishListener", BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(renderer, new object[] { 1f });
            Assert.That(wait.MoveNext(), Is.False);
            Assert.That(typeof(VideoRenderer).GetField("sliderControlling", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(renderer), Is.False);
        }

        private T Child<T>(string name) where T : Component
        {
            GameObject child = new GameObject(name, typeof(RectTransform));
            child.transform.SetParent(root.transform, false);
            return child.AddComponent<T>();
        }

        private static void SetField(object target, string name, object value)
        {
            target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).SetValue(target, value);
        }
    }
}
