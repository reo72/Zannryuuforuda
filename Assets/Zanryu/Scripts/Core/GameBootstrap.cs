using UnityEngine;
using UnityEngine.EventSystems;

namespace Zanryu
{
    /// <summary>
    /// 起動の入口。空のシーンに置いた GameObject にこれを1つ付けるだけで全部が組み上がる。
    /// （メニューの「残留ファイル > メインシーンを作成」で自動作成もできる）
    /// </summary>
    public class GameBootstrap : MonoBehaviour
    {
        void Awake()
        {
            Application.targetFrameRate = 60;
            Content.Load();
            EnsureCamera();
            EnsureEventSystem();

            var os = new GameObject("OS").AddComponent<OSRoot>();
            os.Build();

            var game = new GameObject("Game");
            var gm = game.AddComponent<GameManager>();
            gm.Init();

            if (Application.isEditor || Debug.isDebugBuild)
                game.AddComponent<DebugTools>();

            Settings.Apply(gm.Global);
            os.ShowTitle();
        }

        static void EnsureCamera()
        {
            if (Camera.main != null) return;
            var cam = new GameObject("Main Camera").AddComponent<Camera>();
            cam.tag = "MainCamera";
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            cam.cullingMask = 0;
        }

        static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
#if ENABLE_INPUT_SYSTEM
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();
#else
            es.AddComponent<StandaloneInputModule>();
#endif
        }
    }
}
