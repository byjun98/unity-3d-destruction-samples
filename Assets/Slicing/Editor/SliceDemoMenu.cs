using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Slicing;

namespace Slicing.EditorTools
{
    public static class SliceDemoMenu
    {
        const string ScenePath = "Assets/Slicing/SlicingDemo.unity";

        [MenuItem("Tools/Slicing/Open Demo Scene", priority = 0)]
        public static void OpenDemoScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            var dir = Path.GetDirectoryName(ScenePath);
            if (!AssetDatabase.IsValidFolder(dir))
            {
                Directory.CreateDirectory(dir);
                AssetDatabase.Refresh();
            }

            Scene scene;
            if (File.Exists(ScenePath))
            {
                scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            }
            else
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
                AddBootstrap(scene);
                EditorSceneManager.SaveScene(scene, ScenePath);
            }

            Selection.activeGameObject = GameObject.Find("SliceArena");
        }

        [MenuItem("Tools/Slicing/Setup In Current Scene", priority = 1)]
        public static void SetupInCurrentScene()
        {
            var active = SceneManager.GetActiveScene();
            AddBootstrap(active);
            EditorSceneManager.MarkSceneDirty(active);
        }

        [MenuItem("Tools/Slicing/Build Demo Now (Edit Mode preview)", priority = 20)]
        public static void BuildPreviewNow()
        {
            var bs = Object.FindObjectOfType<SliceArenaBootstrap>();
            if (bs == null)
            {
                var active = SceneManager.GetActiveScene();
                AddBootstrap(active);
                bs = Object.FindObjectOfType<SliceArenaBootstrap>();
            }
            if (bs != null) bs.Build();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        }

        static void AddBootstrap(Scene scene)
        {
            var existing = Object.FindObjectOfType<SliceArenaBootstrap>();
            if (existing != null)
            {
                Selection.activeGameObject = existing.gameObject;
                return;
            }
            var go = new GameObject("SliceArena");
            SceneManager.MoveGameObjectToScene(go, scene);
            go.AddComponent<SliceArenaBootstrap>();
            Selection.activeGameObject = go;
        }
    }
}
