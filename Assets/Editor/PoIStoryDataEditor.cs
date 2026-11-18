using UnityEngine;
using UnityEditor;
using System.IO;

[CustomEditor(typeof(PoIStoryData))]
public class PoIStoryDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        PoIStoryData storyData = (PoIStoryData)target;

        // 绘制默认属性（除 dialogueLines 和 voiceClips 以外）
        DrawPropertiesExcluding(serializedObject, "dialogueLines", "voiceClips");

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Dialogue & Voice-over Mapping", EditorStyles.boldLabel);

        SerializedProperty linesProp = serializedObject.FindProperty("dialogueLines");
        SerializedProperty clipsProp = serializedObject.FindProperty("voiceClips");

        // 自动保持 voiceClips 与 dialogueLines 数组长度一致
        if (clipsProp.arraySize != linesProp.arraySize)
        {
            clipsProp.arraySize = linesProp.arraySize;
        }

        // 绘制每一行的文字、音频插槽和导入按钮
        for (int i = 0; i < linesProp.arraySize; i++)
        {
            EditorGUILayout.BeginVertical("box");
            EditorGUILayout.LabelField($"Line {i + 1}", EditorStyles.miniBoldLabel);

            // 台词输入框
            EditorGUILayout.PropertyField(linesProp.GetArrayElementAtIndex(i), GUIContent.none);

            EditorGUILayout.BeginHorizontal();
            // 音频插槽
            EditorGUILayout.PropertyField(clipsProp.GetArrayElementAtIndex(i), new GUIContent("Voice Clip"));

            // 导入按钮
            if (GUILayout.Button("Add Voice-over", GUILayout.Width(120)))
            {
                string path = EditorUtility.OpenFilePanel("Select Voiceover Audio", Application.dataPath, "wav,mp3,ogg,aif");
                if (!string.IsNullOrEmpty(path))
                {
                    // 拷贝至 Assets/Audio
                    string targetFolder = Path.Combine(Application.dataPath, "Audio");
                    if (!Directory.Exists(targetFolder)) Directory.CreateDirectory(targetFolder);

                    string fileName = Path.GetFileName(path);
                    string destPath = Path.Combine(targetFolder, fileName);
                    File.Copy(path, destPath, true);

                    AssetDatabase.Refresh();

                    string relativePath = "Assets/Audio/" + fileName;
                    AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>(relativePath);
                    clipsProp.GetArrayElementAtIndex(i).objectReferenceValue = clip;
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }

        // Add/delete lines
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ Add Line"))
        {
            linesProp.arraySize++;
            clipsProp.arraySize = linesProp.arraySize;
        }
        if (GUILayout.Button("- Remove Last Line") && linesProp.arraySize > 0)
        {
            linesProp.arraySize--;
            clipsProp.arraySize = linesProp.arraySize;
        }
        EditorGUILayout.EndHorizontal();

        serializedObject.ApplyModifiedProperties();
    }
}