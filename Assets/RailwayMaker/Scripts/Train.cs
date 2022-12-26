using System;
using UnityEngine;

namespace MrWhimble.RailwayMaker.Train
{
    public class Train : MonoBehaviour
    {
        [SerializeField] private Bogie startBogie;
        [SerializeField] private Bogie endBogie;

        private void Update()
        {
            if (startBogie == null || endBogie == null)
                return;

            Vector3 endBogiePosition = endBogie.transform.position;
            Vector3 direction = (startBogie.transform.position - endBogiePosition) *0.5f;
            Vector3 position = endBogiePosition + direction;
            Quaternion rotation = Quaternion.Lerp(startBogie.transform.rotation, endBogie.transform.rotation, 0.5f);
            rotation = Quaternion.LookRotation(direction, rotation * Vector3.up);
            
            transform.SetPositionAndRotation(position, rotation);
        }
    }
}