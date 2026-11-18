using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Zanryu
{
    /// <summary>
    /// 新旧どちらの Input 設定でも動くようにするための窓口。
    /// Unity 6 の新規プロジェクトは Input System（新）が標準なので両対応にしている。
    /// </summary>
    public static class InputCompat
    {
        public static Vector2 MousePosition
        {
            get
            {
#if ENABLE_INPUT_SYSTEM
                var m = Mouse.current;
                return m != null ? m.position.ReadValue() : Vector2.zero;
#else
                return Input.mousePosition;
#endif
            }
        }

        /// <summary>メッセージ送り（Space / Enter）が押された瞬間か</summary>
        public static bool AdvancePressed()
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k == null) return false;
            return k.spaceKey.wasPressedThisFrame || k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame;
#else
            return Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter);
#endif
        }

        /// <summary>F1〜F12 が押された瞬間か（デバッグ用）</summary>
        public static bool FunctionKeyDown(int n)
        {
#if ENABLE_INPUT_SYSTEM
            var k = Keyboard.current;
            if (k == null) return false;
            switch (n)
            {
                case 1: return k.f1Key.wasPressedThisFrame;
                case 2: return k.f2Key.wasPressedThisFrame;
                case 3: return k.f3Key.wasPressedThisFrame;
                case 4: return k.f4Key.wasPressedThisFrame;
                case 5: return k.f5Key.wasPressedThisFrame;
                default: return false;
            }
#else
            return Input.GetKeyDown(KeyCode.F1 + (n - 1));
#endif
        }
    }
}
