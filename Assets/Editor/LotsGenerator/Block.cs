using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace Assets.Editor.LotsGenerator
{
    /// <summary>
    /// A Block is defined as a land area encapsulated by roads.
    /// </summary>
    class Block
    {
        public List<Vector2> corners;
        public List<NodeTriple> nodeTriples;

        public bool highwayAdjacent = false;
        private float Area = float.NaN;
        public Block()
        {
            corners = new List<Vector2>();
            nodeTriples = new List<NodeTriple>();
        }

        private Vector2 getCenter()
        {
          
            Vector2 center = Vector2.zero;
            foreach (Vector2 corner in corners)
            {
                center += corner;
            }
            center /= corners.Count;
            return center;
          
        }
        private float AngleDir(Vector2 A, Vector2 B)
        {
            return -A.x * B.y + A.y * B.x;
        }

        public void shrinkBlock()
        {
            
            List<Vector2> newCorners = new List<Vector2>();
            for(int a = 0; a < corners.Count; a++)
            {
                // Edge before this point (n2 - n1)
                Vector2 v1 = Vector2.zero;
                if(a != 0)
                    v1 = corners[a] - corners[a - 1];
                else
                    v1 = corners[a] - corners[corners.Count - 1];

                // Edge after this point (n3 - n2)
                Vector2 v2 = Vector2.zero;
                if (a != corners.Count-1)
                    v2 = corners[a + 1] - corners[a];
                else
                    v2 = corners[0] - corners[a];

                // Compute angle between the vectors, determine the movement vector direction
                bool right = (AngleDir(v1, v2) >= 0);
                float angle = Vector2.Angle((v1).normalized, (v2).normalized);
                Vector2 movement;
                float distance;
                if (right)
                {
                    movement = Vector2.Lerp(- v1.normalized, v2.normalized, 0.5f);
                    if(movement == Vector2.zero)
                        movement = Quaternion.Euler(0, 0, -90) * v1.normalized;
                    distance = 1f / Mathf.Cos(((angle / 2) * (Mathf.PI / 180)));
                    angle = 180 - angle;
                    movement.Normalize();
                    movement *= Mathf.Max(1f, distance);
                }
                else {
                    movement = Vector2.Lerp(- v1.normalized, v2.normalized, 0.5f);
                    if (movement == Vector2.zero)
                        movement = Quaternion.Euler(0, 0, 90) * v1.normalized;
                    distance = 1f / Mathf.Cos(((angle / 2) * (Mathf.PI / 180)));
                    angle = 180 + angle;
                    movement.Normalize();
                    movement *= Mathf.Max(1f, distance);
                    movement *= -1;
                }

               
                if (nodeTriples[a].n2.onHighway == true)
                {
                     movement *= CityGenerator.highwayWidth / 2 + 1;
                }
                else
                {
                    movement *= CityGenerator.streetWidth / 2 + 1;
                }
               
                newCorners.Add(corners[a] + movement);
                /*
                Vector2 distance = center - corners[a];
                float percentage = CityGenerator.blockShrinkPercentage;

                // Correction for streets and highways
                float correction = 2f;
                if (highwayAdjacent)
                {
                    correction += 10f;
                }
                corners[a] = Vector2.MoveTowards(corners[a], center,  percentage * distance.magnitude + correction);
                */
            }
            corners = newCorners;
        }

        // Calculates the area of a polygon using the standard method.
        public float getArea()
        {
            if(float.IsNaN(Area))
            {
                List<Vector2> vertices = new List<Vector2>();
                foreach (Vector2 corner in corners)
                {
                    vertices.Add(corner);
                }

                vertices.Add(vertices[0]);
                Area = Math.Abs(vertices.Take(vertices.Count - 1).Select((p, i) => (p.x * vertices[i + 1].y) - (p.y * vertices[i + 1].x)).Sum() / 2);
            }
            return Area;
        }

		//Compute the smallest angle in the polygon
		public float getMinAngle(){
			float smallestAngle = float.MaxValue;
			for (int i = 0; i < corners.Count; i++) {
				//get points
				int previousIndex = ((i - 1) < 0) ? corners.Count - 1 : i - 1;
				Vector2 previousPoint = corners [previousIndex];
				Vector2 thisPoint = corners [i];
				Vector2 nextPoint = corners [(i + 1) % corners.Count];

				//get edges
				Vector2 previousEdge = previousPoint - thisPoint;
				Vector2 nextEdge = nextPoint - thisPoint;

				//check the angle
				float angle = Vector2.Angle (previousEdge, nextEdge);
				smallestAngle = (angle < smallestAngle) ? angle: smallestAngle;
			}
			return smallestAngle;
		}
    }
}
