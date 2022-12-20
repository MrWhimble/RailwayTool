using System;
using UnityEngine;

namespace MrWhimble.ConstantConsole
{
    public class BallTest : MonoBehaviour
    {
        private Rigidbody rb;

        private void Awake()
        {
            rb = GetComponent<Rigidbody>();
            //ConstantDebug.Log("Hello");
        }

        private void Update()
        {
            ConstantDebug.Log($"{name}'s position is {transform.position}", this);
        }
        
        private void FixedUpdate()
        {
            ConstantDebug.Log(rb.velocity, this);
        }
    }
}