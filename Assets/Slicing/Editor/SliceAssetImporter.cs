using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using Slicing;

namespace Slicing.EditorTools
{
    public static class SliceAssetImporter
    {
        const string CacheRel = "Unity/Asset Store-5.x";

        [MenuItem("Tools/Slicing/Import .unitypackage From Asset Store Cache...", priority = 40)]
        public static void ImportFromCache()
        {
            string roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string cacheRoot = Path.Combine(roaming, CacheRel);
            if (!Directory.Exists(cacheRoot))
            {
                EditorUtility.DisplayDialog("Slicing", $"Asset Store cache not found at:\n{cacheRoot}", "OK");
                return;
            }

            string path = EditorUtility.OpenFilePanel("Pick a .unitypackage from your Asset Store cache",
                cacheRoot, "unitypackage");
            if (string.IsNullOrEmpty(path)) return;

            // interactive=true shows the standard Import dialog so the user can deselect parts
            AssetDatabase.ImportPackage(path, true);
        }

        [MenuItem("Tools/Slicing/Make Selected Sliceable", priority = 60)]
        public static void MakeSelectedSliceable()
        {
            int added = 0, skippedNoMesh = 0, alreadyHas = 0;
            foreach (var go in Selection.gameObjects)
            {
                foreach (var mf in go.GetComponentsInChildren<MeshFilter>(true))
                {
                    if (mf.sharedMesh == null) continue;
                    if (mf.GetComponent<MeshRenderer>() == null) continue;
                    if (mf.GetComponent<SliceTarget>() != null) { alreadyHas++; continue; }
                    Undo.AddComponent<SliceTarget>(mf.gameObject);
                    if (mf.GetComponent<Collider>() == null)
                        Undo.AddComponent<MeshCollider>(mf.gameObject);
                    added++;
                }
                if (go.GetComponentInChildren<MeshFilter>(true) == null) skippedNoMesh++;
            }
            Debug.Log($"[Slicing] SliceTarget added: {added}, already had: {alreadyHas}, skipped (no mesh): {skippedNoMesh}");
        }

        [MenuItem("Tools/Slicing/Enable Read-Write On Selected Models", priority = 61)]
        public static void EnableReadWriteOnSelected()
        {
            int changed = 0;
            foreach (var obj in Selection.objects)
            {
                string path = AssetDatabase.GetAssetPath(obj);
                if (string.IsNullOrEmpty(path)) continue;
                var importer = AssetImporter.GetAtPath(path);
                if (importer is ModelImporter mi)
                {
                    if (!mi.isReadable)
                    {
                        mi.isReadable = true;
                        mi.SaveAndReimport();
                        changed++;
                    }
                }
                else if (importer is TextureImporter)
                {
                    // ignore
                }
            }
            Debug.Log($"[Slicing] Read/Write enabled on {changed} models. (Select the FBX/OBJ/etc. asset, not its instance.)");
        }

        [MenuItem("Tools/Slicing/Reveal Asset Store Cache In Explorer", priority = 80)]
        public static void RevealCache()
        {
            string roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string cacheRoot = Path.Combine(roaming, CacheRel);
            if (!Directory.Exists(cacheRoot))
            {
                EditorUtility.DisplayDialog("Slicing", $"Cache not found at:\n{cacheRoot}", "OK");
                return;
            }
            EditorUtility.RevealInFinder(cacheRoot);
        }
    }
}
