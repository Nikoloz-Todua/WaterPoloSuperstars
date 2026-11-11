using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ToduStudios.WaterPolo.Hub
{
    public sealed class HubApplication : MonoBehaviour
    {
        public const string Boot="Boot", Lobby="MainLobby", Match="PrototypePool";
        public static HubApplication Instance {get;private set;}
        public HubProgression Progression {get;private set;}
        public string Error {get;private set;} public bool Busy {get;private set;}
        public void ClearError()=>Error=null;
#if UNITY_EDITOR
        // Isolated Editor verification only; never compiled into a player.
        public void UseVerificationProfile(HubProgression service)=>Progression=service;
#endif
        public static string SavePath=>Path.Combine(Application.persistentDataPath,"profile-v1.json");
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)] static void ResetStatics()=>Instance=null;
        public static HubApplication Ensure()
        {
            if(Instance==null)new GameObject("OfflineGameServices").AddComponent<HubApplication>();return Instance;
        }
        void Awake()
        {
            if(Instance!=null&&Instance!=this){Destroy(gameObject);return;}Instance=this;DontDestroyOnLoad(gameObject);Initialize();
        }
        public void Initialize()
        {
            if(Progression!=null)return;
            try{var catalog=Resources.Load<HubCatalog>("HubCatalog");if(catalog==null)throw new InvalidOperationException("Run Tools > Water Polo > Set Up Milestone 12 Hub first.");Progression=new HubProgression(catalog,new JsonProfileRepository(SavePath));Error=null;}
            catch(Exception e){Error="Progress could not be loaded. Your save files are retained. Retry after restoring file access or a compatible backup.";Debug.LogError("Hub initialization failed ("+e.GetType().Name+"). No default save was written.");}
        }
        public void Launch(string mode)
        {
            if(Busy||Progression==null)return;
            if(Progression.BeginMatch(mode,out var message))StartCoroutine(Load(Match));else Error=message;
        }
        public void ReturnToLobby(bool abandon)
        {
            if(Busy)return;if(abandon&&!Progression.LeaveMatch(out var message)){Error=message;return;}StartCoroutine(Load(Lobby));
        }
        public void OpenLobby(){if(!Busy)StartCoroutine(Load(Lobby));}
        IEnumerator Load(string name)
        {
            Busy=true;var op=SceneManager.LoadSceneAsync(name,LoadSceneMode.Single);while(!op.isDone)yield return null;Busy=false;
        }
    }
}
