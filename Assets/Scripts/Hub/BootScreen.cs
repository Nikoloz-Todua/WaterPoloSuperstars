using System.Collections;
using TMPro;
using UnityEngine;

namespace ToduStudios.WaterPolo.Hub
{
    public sealed class BootScreen : MonoBehaviour
    {
        public TMP_Text status; public RectTransform indicator;
        IEnumerator Start()
        {
            if(GetComponent<GamePreferences>()==null)gameObject.AddComponent<GamePreferences>();
            status.text="Loading local preferences…";yield return null;
            var app=HubApplication.Ensure();status.text="Validating profile & preparing your squad…";yield return null;
            if(app.Progression==null){status.text=app.Error;HubWidgets.Button(transform,"Retry","RETRY",.4f,.12f,.2f,.08f,()=>{app.Initialize();if(app.Progression!=null)app.OpenLobby();});yield break;}
            status.text="Ready • offline play";yield return null;app.OpenLobby();
        }
        void Update(){if(indicator!=null)indicator.localRotation=Quaternion.Euler(0,0,-Time.unscaledTime*140);}
    }
}
