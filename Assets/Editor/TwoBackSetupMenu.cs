using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// 開いているシーンにBootstrapを1つ配置し，既定のConfigアセット
// （AreaColorConfig / SequenceSetLibrary）が無ければ作成して割り当てる．
public static class TwoBackSetupMenu
{
    private const string AreaColorConfigPath = "Assets/Config/AreaColorConfig.asset";
    private const string SequenceSetLibraryPath = "Assets/Config/SequenceSetLibrary.asset";

    [MenuItem("2BackTask/Scene/Add Bootstrap To Current Scene")]
    public static void AddBootstrap()
    {
        if (Object.FindObjectOfType<Bootstrap>() != null)
        {
            EditorUtility.DisplayDialog("2BackTask", "Bootstrap はすでにこのシーンに存在します。", "OK");
            return;
        }

        var go = new GameObject("Bootstrap");
        var bootstrap = go.AddComponent<Bootstrap>();

        bootstrap.areaColorConfig = FindOrCreateAsset<AreaColorConfig>(AreaColorConfigPath);
        bootstrap.sequenceSetLibrary = FindOrCreateAsset<SequenceSetLibrary>(SequenceSetLibraryPath);

        Undo.RegisterCreatedObjectUndo(go, "Add 2BackTask Bootstrap");
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Selection.activeGameObject = go;

        EditorUtility.DisplayDialog(
            "2BackTask",
            "Bootstrapを配置しました。\n\n" +
            "残りの手動設定:\n" +
            "1. Window > TextMeshPro > Font Asset Creator で日本語対応SDFフォント（例:Noto Sans JP）を生成\n" +
            "2. 生成したフォントをBootstrapのJapanese Fontに割り当て\n" +
            "3. Assets/Config/ 配下のAreaColorConfig / SequenceSetLibraryを実際の実験プロトコルに合わせて編集",
            "OK");
    }

    private static T FindOrCreateAsset<T>(string path) where T : ScriptableObject
    {
        var existing = AssetDatabase.LoadAssetAtPath<T>(path);
        if (existing != null) return existing;

        string dir = Path.GetDirectoryName(path);
        if (!AssetDatabase.IsValidFolder(dir))
        {
            Directory.CreateDirectory(dir);
            AssetDatabase.Refresh();
        }

        // ObjectFactory.CreateInstanceはCreateAssetMenu経由の生成と同様にReset()を
        // 呼び出すため，AreaColorConfig/SequenceSetLibraryの既定値が確実に入る．
        var asset = ObjectFactory.CreateInstance<T>();
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();
        return asset;
    }
}
