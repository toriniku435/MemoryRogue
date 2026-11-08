using System;

namespace Live2DCardRogue.Common.Audio
{
    // SeId / BgmId の各値が「どこで鳴る音か」。SoundLibrary の Inspector に並べて表示する。
    // 値を足したらこれも付けること（SoundLibraryTests が付け忘れを見つける）
    [AttributeUsage(AttributeTargets.Field)]
    public sealed class SoundUsageAttribute : Attribute
    {
        public string Usage { get; }

        public SoundUsageAttribute(string usage)
        {
            Usage = usage;
        }
    }
}
