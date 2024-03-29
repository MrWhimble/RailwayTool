﻿using UnityEngine;

namespace MrWhimble.RailwayMaker
{
    [System.Serializable]
    public class ControlPoint : Point
    {
        [SerializeReference] public AnchorPoint anchorPoint;

        public float distance;
        public bool flipped;

        public ControlPoint(){}
        
        public ControlPoint(AnchorPoint p, Vector3 pos)
        {
            position = pos;
            p.AddControlPoint(this);
            
        }
        public ControlPoint(AnchorPoint p, float dist, bool f)
        {
            p.AddControlPoint(this);
            distance = dist;
            flipped = f;
            UpdatePosition();
        }

        public void SetAnchorPoint(AnchorPoint p)
        {
            anchorPoint = p;
            distance = Vector3.Distance(position, anchorPoint.position);
        }

        public void UpdatePosition(Vector3 pos)
        {
            position = pos;
            distance = Vector3.Distance(position, anchorPoint.position);
            anchorPoint.UpdateRotation(this);
        }

        public void UpdatePosition()
        {
            Vector3 dir = flipped ? Vector3.back : Vector3.forward;
            dir *= distance;
            dir = anchorPoint.rotation * dir;
            position = anchorPoint.position + dir;
        }

        public void SetDistance(float dist)
        {
            distance = dist;
            UpdatePosition();
        }

        public void Flip()
        {
            flipped = !flipped;
            UpdatePosition();
        }
        public void Flip(bool f)
        {
            flipped = f;
            UpdatePosition();
        }
    }
}