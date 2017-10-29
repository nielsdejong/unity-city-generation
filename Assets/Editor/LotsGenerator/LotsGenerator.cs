
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
namespace Assets.Editor.LotsGenerator
{
    class LotsGenerator
    {
        public List<Block> blocks = new List<Block>();
        public List<Lot> lots = new List<Lot>();
        public void generate()
        {
			//get the nodes
            List<Node> nodes = RoadMapGenerator.nodes;
            if (CityGeneratorUI.DebugMode)
                Debug.Log("NODES:" + nodes.Count);
            
			//generate node triples
            List<NodeTriple> nodeTriples = buildNodeTriples(nodes);
            if (CityGeneratorUI.DebugMode)
                Debug.Log("NODE TRIPLES:" + nodeTriples.Count);

			//sort the node triples by angle
            sortNodeTriplesByAngle(ref nodeTriples);

            /*foreach (NodeTriple nt in nodeTriples)
            {
				Debug.Log(nt + "  angle: " + nt.angle);
            }*/
            if (CityGeneratorUI.DebugMode)
                Debug.Log("NODE TRIPLES SORTED:" + nodeTriples.Count);

			//generate the blocks
            blocks = this.getPolygon2Ds(nodeTriples);
            if (CityGeneratorUI.DebugMode)
                Debug.Log("BLOCKS:" + blocks.Count);

            //generate the lots from the blocks
            generateLots();
        }

        // Generates lots from the blocks
       private void generateLots()
        {
            lots = new List<Lot>();
            foreach(Block block in blocks)
            {
                Lot lot = new Lot(block);
                lot.corners = block.corners;
                lots.Add(lot);
            }
        }

        private List<NodeTriple> buildNodeTriples(List<Node> nodes)
        {
            // Build triples for connected nodes such that:
            // the nodes are connected as follows: (n1)----(n2)----(n3)
            List<NodeTriple> nodeTriples = new List<NodeTriple>();
            foreach (Node n in nodes)
            {
                foreach(Edge e1 in n.edges)
                {
                    foreach(Edge e2 in n.edges)
                    {
                        if(e1.Equals(e2))
                        {
                            continue;
                        }

						Node other1 = e1.getOpposite (n);
						Node other2 = e2.getOpposite (n);
                        NodeTriple nt1 = new NodeTriple(other1, n, other2);
						NodeTriple nt2 = new NodeTriple(other2, n, other1);
                                
						if (!nodeTriples.Contains (nt1)) {
							nodeTriples.Add (nt1); 
						}
						if (!nodeTriples.Contains (nt2)) {
							nodeTriples.Add (nt2);
						}
                    }               
                }
            }
            return nodeTriples;
        }

        // Finds all blocks enclosed by roads in the roadmap.
        private List<Block> getPolygon2Ds(List<NodeTriple> nodeTriples)
        {
            List<Block> polygons = new List<Block>();
            // Repeat until we have considered all triples 
            while(nodeTriples.Count > 0)
            {                            
                // Initialize new polygon object
                Block currentPolygon = new Block();
				bool validPolygon = true;

                // Reference to first triple
                NodeTriple old = nodeTriples[0];
				Node startNode = old.n2;

				//we store the nodeTriples which are used for this polygon
				List<NodeTriple> consideredTriples = new List<NodeTriple> ();
				consideredTriples.Add (old);

				NodeTriple next = null;

                //Debug.Log("New polygon being created with start nodetriple: " + old);
				while (!startNode.Equals((next == null) ? null : next.n2))
                {
                    //find the next node triple
					next = findNext(nodeTriples, old);                    
                    
					if (next != null) {
						//Debug.Log ("Next nodetriple: " + next);
						//the node was already there, and it is not the start node.. invalid polygon
						if(currentPolygon.corners.Contains(next.n2.pos) && !next.n2.Equals(startNode)){
							validPolygon = false;
							break;
						}
                        addCornerToBlock(ref currentPolygon, next);
						consideredTriples.Add (next);
					} else {
						//road ends here
						validPolygon = false;
						break;
					}                  

					old = next;
                }

				//remove the triples that have been considered already
				foreach (NodeTriple nt in consideredTriples) {
					nodeTriples.Remove (nt);
				}

				//if the polygon is valid
				if (validPolygon) {
					//make sure the polygon is in clockwise order
					if (isClockwise (currentPolygon)) {
						//when the smallest angle is large enough, continue
						if(currentPolygon.getMinAngle() > 30){
							currentPolygon.shrinkBlock ();
							if (currentPolygon.getArea () > CityGenerator.minBlockArea && currentPolygon.getArea () < CityGenerator.maxBlockArea) {
								polygons.Add (currentPolygon);
							}
						}
					}
				}

            }
            return polygons;
        }

		/// <summary>
		/// Checks if the corners of a block are in clockwise order
		/// </summary>
		/// <returns><c>true</c>, if clockwise was used, <c>false</c> otherwise.</returns>
		/// <param name="b">The block to check</param>
		/// Using method here described in: http://stackoverflow.com/questions/1165647/how-to-determine-if-a-list-of-polygon-points-are-in-clockwise-order
		private bool isClockwise(Block b){
			float sum = 0;
			for (int i = 0; i < b.corners.Count; i++) {
				Vector2 thisPoint = b.corners [i];
				Vector2 nextPoint = b.corners [(i + 1) % b.corners.Count];
				float formula = (nextPoint.x - thisPoint.x) * (nextPoint.y + thisPoint.y);
				sum += formula;
			}
			return sum >= 0;
		}

        private void addCornerToBlock(ref Block block, NodeTriple old)
        {
            if (old.n2.onHighway)
            {
                block.highwayAdjacent = true;
            }
            block.corners.Add(old.n2.pos);
            block.nodeTriples.Add(old);
        
        }
        // Find next nodetriple in sorted list
        private NodeTriple findNext(List<NodeTriple> triples, NodeTriple old)
        {
			for( int i = 0; i < triples.Count; i++)
            {
				if(old.n2.Equals(triples[i].n1) && old.n3.Equals(triples[i].n2))
                {
					return triples[i];
                }
            }
            return null;
        }

        private void sortNodeTriplesByAngle(ref List<NodeTriple> nodeTriples)
        {
            // Sort triples by angle with x-axis.
            nodeTriples.Sort(
                delegate (NodeTriple a, NodeTriple b)
                {                  
					return (a.angle).CompareTo(b.angle);
                }
            );
        }
			
    }
   
   
}
