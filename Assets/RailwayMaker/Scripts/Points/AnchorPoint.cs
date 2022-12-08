using System.Collections.Generic;
using UnityEngine;

namespace MrWhimble.RailwayMaker
{
    [System.Serializable]
    public class AnchorPoint : Point
    {
        public Quaternion rotation;

        [SerializeReference] public List<ControlPoint> controlPoints;

        public AnchorPoint()
        {
            controlPoints = new List<ControlPoint>();
        }
        
        public void AddControlPoint(ControlPoint control)
        {
            if (controlPoints.Contains(control))
                return;
            
            controlPoints.Add(control);
            control.SetAnchorPoint(this);
        }

        public void RemoveControlPoint(ControlPoint control)
        {
            if (!controlPoints.Contains(control))
                return;

            controlPoints.Remove(control);
        }
        
        public void UpdateRotation(ControlPoint effector)
        {
            Vector3 normal = rotation * Vector3.up;
            Vector3 dirTo = effector.position - position;
            if (effector.flipped)
                dirTo = -dirTo;
            rotation = Quaternion.LookRotation(dirTo, normal);
            
            UpdateControls(effector);
            
        }

        public void UpdateControls(ControlPoint ignore = null)
        {
            if (controlPoints == null)
                return;

            foreach (ControlPoint c in controlPoints)
            {
                if (c == ignore)
                    continue;
                c.UpdatePosition();
            }
        }

        public void UpdatePosition(Vector3 pos)
        {
            position = pos;
            
            if (controlPoints == null)
                return;

            foreach (ControlPoint c in controlPoints)
            {
                c.UpdatePosition();
            }
        }
    }
}