using UnityEditor;
using UnityEngine;
using UnityEngine.Video;
using Yvsim.InspectorGroups.Editor;

namespace Yvsim.Video.Editor
{
    [CustomEditor(typeof(VideoPlayerUGUI))]
    [CanEditMultipleObjects]
    public class VideoPlayerUGUIEditor : InspectorGroupsEditor<VideoPlayerUGUI>
    {
        protected override void BuildInspector(InspectorContent content)
        {
            var source = serializedObject.FindProperty("source");
            if (source.hasMultipleDifferentValues || source.intValue != (int)VideoSource.Url) return;
            content.Group("VideoFile", "视频文件", actions => actions.Action("浏览视频文件…", BrowseVideo), style: Yvsim.InspectorGroups.InspectorGroupStyle.Framework);
        }

        private void BrowseVideo()
        {
            string path = EditorUtility.OpenFilePanelWithFilters("选择视频文件",
                Application.streamingAssetsPath, new[] { "Unity 视频文件", "mp4,mov,avi,webm,ogg" });
            if (string.IsNullOrEmpty(path)) return;
            serializedObject.FindProperty("url").stringValue = path;
            serializedObject.ApplyModifiedProperties();
        }
    }
}
