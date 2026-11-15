using TMPro;
using UnityEngine;

namespace ToduStudios.WaterPolo.WaterVisuals
{
    /// <summary>Small contrast plates follow existing UI visibility without handling input.</summary>
    public sealed class ArenaHudReadability : MonoBehaviour
    {
        public TMP_Text[] labels;
        public GameObject[] plates;
        void LateUpdate()
        {
            for(int i=0;i<labels.Length;i++)
            {
                bool show=labels[i]!=null && labels[i].gameObject.activeInHierarchy && labels[i].text.Length>0;
                if(plates[i].activeSelf!=show)plates[i].SetActive(show);
            }
        }
    }
}
