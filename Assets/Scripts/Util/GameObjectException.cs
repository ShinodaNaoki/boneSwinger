using System;
using System.Collections.Generic;
using UnityEngine;

public static class GameObjectExtensions
{
    public static void SetLayerRecursively(this GameObject obj, int layer)
    {
        obj.layer = layer;

        foreach (Transform n in obj.transform)
        {
            SetLayerRecursively(n.gameObject, layer);
        }
    }

    /**
     * 再帰的に全ての子孫オブジェクトを列挙します
     */
    public static IEnumerable<GameObject> GetAllObjectRecursively(this GameObject root)
    {
        var cn = root.transform.childCount;
        for (int i = 0; i < cn; i++)
        {
            var c1 = root.transform.GetChild(i);
            yield return c1.gameObject;

            var sub = GetAllObjectRecursively(c1.gameObject).GetEnumerator();
            while (sub.MoveNext())
            {
                yield return sub.Current;
            }

        }
    }

    /**
     * 子孫オブジェクトを再帰的に検索して、名前に一致する最初のものを返します
     */
    public static GameObject FindByNameRecursively(this GameObject root, string name)
    {
        var transform = root.transform.Find(name);
        if (transform != null) return transform.gameObject;

        var cn = root.transform.childCount;
        for (int i = 0; i < cn; i++)
        {
            var c1 = root.transform.GetChild(i);
            var go = FindByNameRecursively(c1.gameObject, name);
            if (go != null) return go;
        }
        return null;
    }

    /**
     * 条件を満たす全ての子孫オブジェクトを再帰的に列挙します
     */
    public static IEnumerable<GameObject> FindRecursively(this GameObject root, Func<GameObject, bool> predicate)
    {
        var cn = root.transform.childCount;
        for (int i = 0; i < cn; i++)
        {
            var c1 = root.transform.GetChild(i);
            var go = c1.gameObject;
            if (predicate(go))
            {
                yield return go;
            }

            var sub = FindRecursively(go, predicate).GetEnumerator();
            while (sub.MoveNext())
            {
                yield return sub.Current;
            }
        }
    }

    /**
     * 条件を満たす全ての子孫オブジェクトを再帰的に列挙します
     */
    public static IEnumerable<T> FindRecursively<T>(this GameObject root) where T : UnityEngine.Object
    {
        var cmps = root.GetComponents<T>();
        var cs = cmps.Length;
        for (int j = 0; j < cs; j++)
        {
            yield return cmps[j];
        }

        var cn = root.transform.childCount;
        for (int i = 0; i < cn; i++)
        {
            var c1 = root.transform.GetChild(i);
            var go = c1.gameObject;

            var sub = FindRecursively<T>(go).GetEnumerator();
            while (sub.MoveNext())
            {
                yield return sub.Current;
            }
        }
    }

    /**
     * 条件を満たす自身または最初の先祖オブジェクトを返します
    */
    public static GameObject FindClosest(this GameObject obj, Func<GameObject, bool> predicate)
    {
        if (predicate(obj))
        {
            return obj;
        }
        var parent = obj.transform.parent;
        return parent == null ? null : FindClosest(parent.gameObject, predicate);
    }

    public static T FindClosest<T>(this GameObject obj) where T : UnityEngine.Object
    {
        var found = obj.GetComponent<T>();
        if (found != null)
        {
            return found;
        }
        var parent = obj.transform.parent;
        return parent == null ? null : FindClosest<T>(parent.gameObject);
    }
}