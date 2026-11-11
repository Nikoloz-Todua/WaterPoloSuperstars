using System;
using System.IO;
using UnityEngine;

namespace ToduStudios.WaterPolo.Hub
{
    /// <summary>Atomic snapshot + previous valid snapshot. Never falls back to defaults when existing data is unreadable.</summary>
    public sealed class JsonProfileRepository : IProfileRepository
    {
        public string Path {get;}
        Action<LocalProfile> contentValidation;
        public JsonProfileRepository(string path){Path=path;}
        public void SetContentValidation(Action<LocalProfile> validation)=>contentValidation=validation;
        void ValidateSnapshot(LocalProfile p){Validate(p);contentValidation?.Invoke(p);}
        public LocalProfile Load()
        {
            bool found=false;
            foreach(string file in new[]{Path,Path+".bak",Path+".tmp"})
            {
                if(!File.Exists(file))continue;found=true;
                try {var p=JsonUtility.FromJson<LocalProfile>(File.ReadAllText(file));ValidateSnapshot(p);return p;}
                catch(Exception e) when(e is IOException || e is ArgumentException || e is InvalidDataException){Debug.LogWarning("Local profile snapshot could not be loaded ("+e.GetType().Name+"). Trying recovery.");}
            }
            if(found)throw new IOException("Existing progress could not be recovered. Original files retained; retry loading or restore a backup.");
            return null;
        }
        public static void Validate(LocalProfile p)
        {
            // A newer valid schema must never be silently downgraded to an older backup.
            if(p!=null&&p.schemaVersion!=1)throw new NotSupportedException("This profile requires a compatible save migration.");
            if(p==null || string.IsNullOrWhiteSpace(p.profileId) || p.coins<0 || p.gems<0 || p.xp<0 || p.seasonXp<0 || p.revision<0 || p.collection==null || p.lineup==null || p.packs==null || p.transactions==null || p.claimedRewards==null || p.history==null)
                throw new InvalidDataException("Unsupported or invalid profile schema.");
        }
        public void Save(LocalProfile p)
        {
            ValidateSnapshot(p);Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path));
            string json=JsonUtility.ToJson(p);File.WriteAllText(Path+".tmp",json);
            // Validate the exact snapshot before replacing the primary.
            ValidateSnapshot(JsonUtility.FromJson<LocalProfile>(File.ReadAllText(Path+".tmp")));
            if(File.Exists(Path))
            {
                // Only a known-valid primary may replace the recovery backup.
                bool valid=false;try{ValidateSnapshot(JsonUtility.FromJson<LocalProfile>(File.ReadAllText(Path)));valid=true;}catch(ArgumentException){}catch(InvalidDataException){}
                if(valid)File.Replace(Path+".tmp",Path,Path+".bak");
                else {File.Copy(Path+".tmp",Path,true);File.Delete(Path+".tmp");}
            }
            else File.Move(Path+".tmp",Path);
        }
    }
}
