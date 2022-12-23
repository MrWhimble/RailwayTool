using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MrWhimble.RailwayMaker
{
    [System.Serializable]
    public class RailwayManager : MonoBehaviour
    {
        //[SerializeReference] public RailwayNetwork railwayNetwork;

        [SerializeField] private RailPathData pathData;
        public RailPathData PathData => pathData;
    }
}