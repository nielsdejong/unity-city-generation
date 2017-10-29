using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Editor.LotsGenerator
{
    class NodeTriple
    {
		public Node n1 { get; set; }
		public Node n2 { get; set; }
		public Node n3 { get; set; }
		public float angle { get; set; }
        public NodeTriple(Node n1, Node n2, Node n3)
        {
            this.n1 = n1;
            this.n2 = n2;
            this.n3 = n3;
			computeAngle ();
        }

		private void computeAngle(){
			//check if this triple goes right or left
			Vector2 A = n2.pos - n1.pos;
			Vector2 B = n3.pos - n1.pos;
			bool right = (AngleDir(A, B) >= 0);

			if (right) {
				angle = 180 - Vector2.Angle ((n2.pos - n1.pos).normalized, (n3.pos - n2.pos).normalized);
			} else {
				angle = 180 + Vector2.Angle ((n2.pos - n1.pos).normalized, (n3.pos - n2.pos).normalized);
			}
		}

		//smaller than 0 is left, larger is right
		private float AngleDir(Vector2 A, Vector2 B)
		{
			return -A.x * B.y + A.y * B.x;
		}

        public override string ToString()
        {
            return "(" + n1 + "," + "n2" + n2 + "," + n3 + ")";
        }

        public List<NodeTriple> successors = new List<NodeTriple>();

        public void findSuccessorsFrom(List<NodeTriple> nodeTriples)
        {
            foreach(NodeTriple nt in nodeTriples)
            {
                if(this.n2 == nt.n1)
                {
                    if(this.n3 == nt.n2)
                    {
                        successors.Add(nt);
                    }
                }
            }
            Debug.Log(successors.Count);
        }

        public NodeTriple getBestSuccessor ()
        {
            float bestAngle = float.MaxValue;
            NodeTriple bestSuccessor = null;

            foreach (NodeTriple a in successors)
            {
                // First node triple angle
                Vector2 a1 = a.n2.pos - a.n1.pos;
                Vector2 a2 = a.n3.pos - a.n2.pos;
                a1 = -a1;
                float angleA = Mathf.Atan2(a2.y, a2.x) - Mathf.Atan2(a1.y, a1.x);
                if(angleA < bestAngle)
                {
                    bestAngle = angleA;
                    bestSuccessor = a;
                }
            }
           
            return bestSuccessor;
        }

    	public override bool Equals(object other)
        {
            if (other == null) return false;
            NodeTriple otherNodeTriple = (NodeTriple)other;
			if (this.n1.Equals(otherNodeTriple.n1) && this.n2.Equals(otherNodeTriple.n2) && this.n3.Equals(otherNodeTriple.n3))
            {
                return true;
            }
            return false;
        }

		public override int GetHashCode(){
			return (int)n2.x + (int)n2.y;
		}
        
    }


}
