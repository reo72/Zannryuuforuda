using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Zanryu.EditorTools
{
    /// <summary>Unity 上部メニュー「残留ファイル」</summary>
    public static class ZanryuMenu
    {
        const string ScenePath = "Assets/Zanryu/Scenes/Main.unity";

        [MenuItem("残留ファイル/メインシーンを作成", priority = 0)]
        static void CreateScene()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            new GameObject("Bootstrap").AddComponent<GameBootstrap>();
            Directory.CreateDirectory(Path.GetDirectoryName(ScenePath));
            EditorSceneManager.SaveScene(scene, ScenePath);

            var scenes = EditorBuildSettings.scenes.ToList();
            if (scenes.All(s => s.path != ScenePath))
            {
                scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
            Debug.Log($"[残留ファイル] {ScenePath} を作成しました。再生ボタンで起動できます。");
        }

        [MenuItem("残留ファイル/セーブデータを全部削除", priority = 20)]
        static void DeleteSave()
        {
            SaveSystem.DeleteAll();
            Debug.Log("[残留ファイル] セーブデータを削除しました");
        }

        [MenuItem("残留ファイル/セーブフォルダを開く", priority = 21)]
        static void OpenSaveFolder() => EditorUtility.RevealInFinder(Application.persistentDataPath + "/");

        [MenuItem("残留ファイル/テスト：END1〜4を見たことにする", priority = 40)]
        static void UnlockEndings()
        {
            var g = SaveSystem.LoadGlobal();
            foreach (var id in new[] { "end1", "end2", "end3", "end4" })
                if (!g.endings.Contains(id)) g.endings.Add(id);
            SaveSystem.SaveGlobal(g);
            Debug.Log("[残留ファイル] END1〜4 を到達済みにしました（ログイン画面で yui が選べます）");
        }
    }
}
