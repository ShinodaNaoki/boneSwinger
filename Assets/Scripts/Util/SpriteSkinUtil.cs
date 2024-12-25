using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.U2D.Animation;


namespace Program.Util
{
    public static class SpriteSkinUtil
    {
        public static GameObject FindImporterRoot(GameObject go)
        {
            if (go == null)
            {
                UnityEngine.Debug.LogWarning("FindImporterRoot: argument is null.");
                return null;
            }
            go = go.FindClosest(x => x.GetComponent<SpriteSkin>() == null);
            var found = go.GetAllObjectRecursively()
                .FirstOrDefault(c => c.GetComponentInChildren<SpriteSkin>() != null);
            if (found == null)
            {
                if (go.transform.parent != null)
                {
                    found = FindImporterRoot(go.transform.parent.gameObject);
                }
                else
                {
                    UnityEngine.Debug.LogWarning("FindImporterRoot: No SpriteSkins are found.");
                }
            }
            return found;
        }

        public static bool IsImporterRoot(this GameObject go)
        {
            if (go == null) return false;
            if (go.GetComponentInChildren<SpriteSkin>() == null) return false;
            return go.GetComponent<SpriteSkin>() == null;
        }

        public static GameObject FindBoneRoot(GameObject go)
        {
            if (go == null)
            {
                UnityEngine.Debug.LogWarning("FindBoneRoot: argument is null.");
                return null;
            }

            var skin = go.GetComponent<SpriteSkin>();
            if (skin != null)
            { // goがSpriteSkinの一つだった場合
                go = go.FindClosest(x => x.GetComponent<SpriteSkin>() != null);
                return FindBoneRoot(go, skin);
            }
            var importer = FindImporterRoot(go);
            if (importer == null)
            {
                // 関係ないオブジェクトだった
                return null;
            }

            skin = importer.GetComponentInChildren<SpriteSkin>();
            UnityEngine.Debug.Assert(skin != null);

            var goal = importer.transform;
            var bone = skin.rootBone;
            // importerに直接ついてるboneを探す
            while (bone.parent != goal)
            {
                if (bone.parent == null) break;
                bone = bone.parent;
            }
            return bone.gameObject;
        }

        private static GameObject FindBoneRoot(GameObject root, SpriteSkin skin)
        {
            if (skin == null) return null;
            var bone = skin.rootBone;
            while (bone.parent != root.transform)
            {
                bone = bone.parent;
            }
            return bone.gameObject;
        }

        public static IEnumerable<List<Transform>> FindSequentialBoneBranches(GameObject go) {
            var boneRoot = FindBoneRoot(go);
            if (boneRoot == null) return Enumerable.Empty<List<Transform>>();
            return FindSequentialBranches(boneRoot.transform);
        }

        private static IEnumerable<List<Transform>> FindSequentialBranches(Transform bone)
        {
            var list = new List<Transform>();
            while (bone.childCount == 1)
            {
                list.Add(bone);
                bone = bone.GetChild(0);
            }
            list.Add(bone);
            if (bone.childCount == 0)
            {
                yield return list;
                yield break;
            }

            for(int i = 0; i< bone.childCount; i++)
            {                
                var children = FindSequentialBranches(bone.GetChild(i)).GetEnumerator();
                while(children.MoveNext())
                {
                    yield return children.Current;
                }
            }
        }

    }
}
