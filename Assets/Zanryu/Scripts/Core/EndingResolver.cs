namespace Zanryu
{
    /// <summary>
    /// 最終日にシャットダウンしたときのエンド判定。
    /// END0（yui.exe を終了）と END2（初期化）はイベントで即時発生するので、ここでは扱わない。
    /// トゥルーENDは yui アカウントでのチャット選択で発生する。
    /// </summary>
    public static class EndingResolver
    {
        public static string ResolveFinalDay()
        {
            var gm = GameManager.I;
            var c = Content.Config;
            if (gm.CountFlags("seller_") >= c.sellerThreshold) return "end3";   // 出品者に連絡し続けた
            if (gm.CountFlags("pretend_") >= c.pretendThreshold) return "end4"; // ミサキのふりを続けた
            return "end1";                                                       // 何もしなかった
        }
    }
}
