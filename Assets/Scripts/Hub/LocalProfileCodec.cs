using System;
using UnityEngine;

namespace ToduStudios.WaterPolo.Hub
{
    /// <summary>Unity's inline serializer materializes null nested records as empty objects. Schema 1 reserves these exact empty records for absence.</summary>
    public static class LocalProfileCodec
    {
        public static LocalProfile FromJson(string json) => Normalize(JsonUtility.FromJson<LocalProfile>(json));
        public static LocalProfile Copy(LocalProfile profile) => FromJson(JsonUtility.ToJson(profile));
        public static LocalProfile Normalize(LocalProfile profile)
        {
            if(profile==null)return null;
            var session=profile.activeMatch;
            if(session!=null && string.IsNullOrEmpty(session.id) && string.IsNullOrEmpty(session.mode) && session.lineup!=null && session.lineup.Count==0)
                profile.activeMatch=null;
            var result=profile.pendingResult;
            if(result!=null && string.IsNullOrEmpty(result.id) && string.IsNullOrEmpty(result.result) && string.IsNullOrEmpty(result.pack)
                && result.blue==0 && result.white==0 && result.coins==0 && result.xp==0 && result.trophies==0)
                profile.pendingResult=null;
            return profile;
        }
    }
}
