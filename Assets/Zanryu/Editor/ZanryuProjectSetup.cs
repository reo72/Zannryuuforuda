using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Zanryu.EditorTools
{
    /// <summary>
    /// プロジェクトを開いたときに一度だけ、ゲーム名とアイコンを設定する。
    /// もう一度やりたいときは、メニュー「残留ファイル > ゲーム名とアイコンを設定」。
    /// </summary>
    [InitializeOnLoad]
    static class ZanryuProjectSetup
    {
        const string Marker = "ProjectSettings/ZanryuSetupDone.txt";
        const string IconPath = "Assets/Zanryu/Branding/app_icon.png";

        static ZanryuProjectSetup()
        {
            if (File.Exists(Marker)) return;
            EditorApplication.delayCall += () =>
            {
                if (Apply()) File.WriteAllText(Marker, "done");
            };
        }

        [MenuItem("残留ファイル/ゲーム名とアイコンを設定", priority = 1)]
        static void ApplyFromMenu()
        {
            if (Apply()) Debug.Log("[残留ファイル] ゲーム名とアイコンを設定しました");
            else Debug.LogWarning($"[残留ファイル] アイコン画像が見つかりません: {IconPath}");
        }

        static bool Apply()
        {
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (icon == null) return false; // まだ読み込まれていない
            PlayerSettings.productName = "残留ファイル";
            PlayerSettings.SetIcons(NamedBuildTarget.Unknown, new[] { icon }, IconKind.Any);
            AssetDatabase.SaveAssets();
            return true;
        }
    }
}
