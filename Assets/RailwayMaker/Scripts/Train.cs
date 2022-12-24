using System;
using System.Collections;
using System.Collections.Generic;
using MrWhimble.RailwayMaker.Graph;
using UnityEngine;

namespace MrWhimble.RailwayMaker
{
    public class Train : MonoBehaviour
    {
        [SerializeField] private RailwayNetwork network;

        private Node previousNode;
        private Node currentNode;
        private Node nextNode;

        private List<Node> path;

        private IEnumerator Start()
        {
            yield return new WaitForSeconds(2);

            previousNode = null;
            currentNode = network.GetRandomNode();
            Node endNode = network.GetRandomNode();
            currentNode = network.GetNodeAtIndex(0, false);
            endNode = network.GetNodeAtIndex(3, true);

            StartCoroutine(network.GetPath(currentNode, endNode));
            //path = network.GetPath(currentNode, endNode);
            
            DebugPath(currentNode, endNode, path, 30f);
        }

        private void DebugPath(Node start, Node end, List<Node> p, float t)
        {
            Debug.DrawRay(start.GetPosition(), Vector3.up, Color.red, t);

            Debug.DrawRay(end.GetPosition(), Vector3.up, Color.green, t);

            if (p == null)
            {
                Debug.LogError("Path is null!!");
                return;
            }
            
            for (int i = 0; i < p.Count-1; i++)
            {
                Node current = p[i];
                Node next = p[i + 1];
                Vector3 currentPos = current.GetPosition();
                Vector3 nextPos = next.GetPosition();
                
                Debug.DrawLine(currentPos, nextPos, Color.cyan, t);
            }
        }
    }
}