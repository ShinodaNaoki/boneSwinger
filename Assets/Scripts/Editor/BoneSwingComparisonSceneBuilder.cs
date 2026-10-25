using Duel.BoneDragger.Sample;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class BoneSwingComparisonSceneBuilder
{
    private const string ScenePath = "Assets/Scenes/SwingCompare.unity";
    private const string PrefabPath = "Assets/prefab/Chara.prefab";

    [MenuItem("Tools/BoneSwinger/Create Comparison Scene")]
    public static void CreateScene()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
        {
            Debug.LogError($"{PrefabPath} が見つかりません。");
            return;
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        var camera = new GameObject("Main Camera").AddComponent<Camera>();
        camera.tag = "MainCamera";
        camera.orthographic = true;
        camera.transform.position = new Vector3(0, 0, -10);

        var root = new GameObject("BoneSwing Comparison");
        var comparison = root.AddComponent<BoneSwingComparison>();
        var so = new SerializedObject(comparison);
        so.FindProperty("characterPrefab").objectReferenceValue = prefab;
        so.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(scene, ScenePath);
        Selection.activeGameObject = root;
        Debug.Log($"{ScenePath} を作成しました。再生して左右を比べてください。");
    }
}
