using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorld;
using Verse;

namespace achan1989.dehydration
{
    public static class StoragePriority_Extensions
    {
        public static StoragePriority HigherPriority(this StoragePriority priority)
        {
            switch (priority)
            {
                case StoragePriority.Unstored:
                    return StoragePriority.Low;
                case StoragePriority.Low:
                    return StoragePriority.Normal;
                case StoragePriority.Normal:
                    return StoragePriority.Preferred;
                case StoragePriority.Preferred:
                    return StoragePriority.Important;
                case StoragePriority.Important:
                    return StoragePriority.Critical;
                case StoragePriority.Critical:
                    return StoragePriority.Low;
                default:
                    throw new InvalidOperationException();
            }
        }

        public static StoragePriority LowerPriority(this StoragePriority priority)
        {
            switch (priority)
            {
                case StoragePriority.Unstored:
                    return StoragePriority.Critical;
                case StoragePriority.Low:
                    return StoragePriority.Critical;
                case StoragePriority.Normal:
                    return StoragePriority.Low;
                case StoragePriority.Preferred:
                    return StoragePriority.Normal;
                case StoragePriority.Important:
                    return StoragePriority.Preferred;
                case StoragePriority.Critical:
                    return StoragePriority.Important;
                default:
                    throw new InvalidOperationException();
            }
        }
    }
}
