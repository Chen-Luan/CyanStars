using System;
using CyanStars.Framework;

namespace CyanStars.Gameplay.MusicGame
{
    public class MusicGameTrackModule : BaseDataModule
    {
        public override void OnInit()
        {
        }

        public bool TryGetTrackLoader(Type chartTrackType, out ITrackLoader trackLoader)
            => TrackLoaderRegistry.TryGetTrackLoader(chartTrackType, out trackLoader);
    }
}
