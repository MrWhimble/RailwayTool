using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MrWhimble.RailwayMaker
{
    [System.Serializable]
    public class RailwayManager : MonoBehaviour
    {
        //[SerializeReference] public RailwayNetwork railwayNetwork;

        [SerializeField] public RailPathData pathData;
    }
}