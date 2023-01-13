using UnityEngine;

namespace MrWhimble.RailwayMaker
{
    [System.Serializable]
    public class RailwayManager : MonoBehaviour
    {
        [SerializeField] private RailPathData pathData;
        public RailPathData PathData => pathData;
    }
}