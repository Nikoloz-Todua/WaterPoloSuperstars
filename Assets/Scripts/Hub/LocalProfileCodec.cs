using System;
using UnityEngine;

namespace ToduStudios.WaterPolo.Hub
{
    /// <summary>Explicit save migration and normalization of Unity's exact empty inline records to absence.</summary>
    public static class LocalProfileCodec
    {
        public static LocalProfile FromJson(string json) => Normalize(JsonUtility.FromJson<LocalProfile>(json));
        public static LocalProfile Copy(LocalProfile profile) => FromJson(JsonUtility.ToJson(profile));
        public static LocalProfile Normalize(LocalProfile profile)
        {
            if(profile==null)return null;
            // Explicit 1 -> 2 migration in memory. The first subsequent atomic save retains
            // the original schema-1 primary as .bak; loading alone never rewrites it.
            if(profile.schemaVersion==1)
            {
                JsonProfileRepository.ValidateLegacy(profile);
                profile.completedDivisions=new System.Collections.Generic.List<string>();
                profile.championship=null;profile.schemaVersion=2;
            }
            var session=profile.activeMatch;
            if(session!=null && string.IsNullOrEmpty(session.id) && string.IsNullOrEmpty(session.mode) && string.IsNullOrEmpty(session.divisionId) && string.IsNullOrEmpty(session.championshipId) && session.round==0 && session.lineup!=null && session.lineup.Count==0)
                profile.activeMatch=null;
            var result=profile.pendingResult;
            if(result!=null && string.IsNullOrEmpty(result.id) && string.IsNullOrEmpty(result.result) && string.IsNullOrEmpty(result.pack)
                && string.IsNullOrEmpty(result.divisionId) && string.IsNullOrEmpty(result.divisionMessage) && result.blue==0 && result.white==0 && result.coins==0 && result.xp==0 && result.trophies==0)
                profile.pendingResult=null;
            var run=profile.championship;
            if(run!=null && string.IsNullOrEmpty(run.id) && string.IsNullOrEmpty(run.divisionId) && string.IsNullOrEmpty(run.formatId) && run.wins==0 && run.matches==0)profile.championship=null;
            return profile;
        }
    }
}
