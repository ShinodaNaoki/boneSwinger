using Duel.BoneDragger;
using Program.Util;
using Program.Utils;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using UnityEngine.U2D.IK;

[CustomEditor(typeof(BoneDraggerManager),false)]
public class BoneDraggerManagerEditor : Editor
{

    private string keywords;
    private GameObject destination;

    private bool clearPreviouslyAssociated = false;
    private bool includeAssociatedToOtherObject = false;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        keywords = EditorGUILayout.TextField("Keyword for bone.", keywords);
        var label = new GUIContent("GameObject to setup.", "Create new if not specified.");
        destination = EditorGUILayout.ObjectField(label, destination, typeof(GameObject), true) as GameObject;
        var label2 = new GUIContent("Clear current draggers on the object.", "Reset the object before setup.");
        clearPreviouslyAssociated = EditorGUILayout.Toggle(label2, clearPreviouslyAssociated);
        var label3 = new GUIContent("Include associated to others.", "Include bones associated to the dragger in other object.");
        includeAssociatedToOtherObject = EditorGUILayout.Toggle(label3, includeAssociatedToOtherObject);

        var keys = (keywords ?? "").Split(',').Select(k => k.Trim()).Where(k => k.Length > 0).Distinct().ToList();
        if (destination == null)
        {
            if (GUILayout.Button("Create Object And Auto Setup"))
            {
                destination = CreateGameObject(keys.FirstOrDefault());
                Setup(destination, keys);
            }
        }
        else
        {
            if (GUILayout.Button("Auto Setup"))
            {
                Setup(destination, keys);
            }
        }

        var manager = target as BoneDraggerManager;

        if (GUILayout.Button("Save Current Rotations"))
        {
            manager.SaveCurrentRotations();
        }

        if (GUILayout.Button("Restore saved Rotations"))
        {
            manager.RestoreSavedRotations();
        }


        if (GUILayout.Button("Check duplicate associations"))
        {
            manager.CheckDuplicatedNodes();
        }

    }

    private void Setup(GameObject go, List<string> keys)
    {
        AssetDatabase.Refresh();
        UnityEditor.EditorUtility.SetDirty(go);
        var branches = ListUpNodes(go, keys);
        var curDraggers = go.GetComponents<BoneDragger>();
        var lastOne = curDraggers.LastOrDefault();
        var draggers = branches.Select((br, i) => {
            UnityEngine.Debug.Assert(br.Count > 0);
            var overwrite = clearPreviouslyAssociated && curDraggers.Length > i;
            var dragger = overwrite ? curDraggers[i] : go.AddComponent<BoneDragger>();
            if (!overwrite)
            {
                ComponentUtility.CopyComponent(lastOne);
                ComponentUtility.PasteComponentValues(dragger);
            }
            UnityEngine.Debug.Log($"setup component {i}, for {br.First().name}".cyan());

            dragger.InitWithBones(br);
            return dragger;
        }).ToList();
        if (clearPreviouslyAssociated)
        {
            var remains = curDraggers.Skip(draggers.Count).ToArray();
            for (int i = 0; i < remains.Length; i++)
            {
                DestroyImmediate(remains[i]);
                UnityEngine.Debug.Log($"remove component {i}".red());
            }
        }
        AssetDatabase.SaveAssetIfDirty(go);
    }

    private GameObject CreateGameObject(string key)
    {
        var go = new GameObject();
        go.name = key != null ? $"Dragger {key}" : "Draggers";
        var manager = target as BoneDraggerManager;
        go.transform.parent = manager.transform;
        go.AddComponent<BoneDraggerParameters>();
        return go;
    }

    private IEnumerable<List<Transform>> ListUpNodes(GameObject go, List<string> keys)
    {
        var manager = target as BoneDraggerManager;
        go.transform.parent = manager.transform;
        var branches = SpriteSkinUtil.FindSequentialBoneBranches(go).Where(br => br.Count > 2);
        UnityEngine.Debug.Log($"branches ALL: {branches.Count()}");

        var ik = manager.GetComponent<IKManager2D>();
        if (ik != null)
        {
            var used = ik.solvers.Select(sol => sol.GetChain(0).effector).ToList();
            branches = branches.Where(b => !used.Contains(b.Last()));
            UnityEngine.Debug.Log($"branches AFTER IK used check: {branches.Count()}");
        }

        if (!includeAssociatedToOtherObject)
        {
            var others = go.transform.parent.GetComponentsInChildren<BoneDragger>()
                .Where(x => x.gameObject != go).Select(x => x.baseBone).Where(b => b != null).ToList();
            branches = branches.Where(b => !others.Contains(b.First()));
            UnityEngine.Debug.Log($"branches AFTER other check: {branches.Count()}".lime());
        }
        if (!clearPreviouslyAssociated)
        {
            var used = go.GetComponents<BoneDragger>()
                .Select(x => x.baseBone).Where(b => b != null).ToList(); ;
            branches = branches.Where(b => !used.Contains(b.First()));
            UnityEngine.Debug.Log($"branches AFTER already associated check: {branches.Count()}".yellow());
        }

        if (keys.Count > 0)
        {
            branches = branches.Where(b => keys.Any(key => b.First().name.Contains(key)));
            UnityEngine.Debug.Log($"branches AFTER keyword check: {branches.Count()}".orange());
        }

        return branches;
    }

}

