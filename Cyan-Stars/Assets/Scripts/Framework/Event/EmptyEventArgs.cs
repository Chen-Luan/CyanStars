﻿using System;
using CatAsset.Runtime;


namespace CyanStars.Framework.Event
{
    public class EmptyEventArgs : EventArgs,IReference
    {
        public static EmptyEventArgs Create()
        {
            EmptyEventArgs eventArgs = ReferencePool.Get<EmptyEventArgs>();
            return eventArgs;
        }

        public void Clear()
        {

        }
    }
}
