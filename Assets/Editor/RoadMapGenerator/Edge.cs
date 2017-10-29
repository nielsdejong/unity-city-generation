using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public class Edge
{
	public Node n1 { get; set;}
	public Node n2 { get; set;}
    RoadTypes type;
    int priority;
	private Edge pred;
	private Edge succ;
	private Vector2 direction;
    public int connectedBlocks = 0;

    public Edge(Node n1, Node n2, RoadTypes type)
    {
        this.n1 = n1;
        this.n2 = n2;
        this.type = type;
        this.priority = 0;
		computeDirection ();
    }

    public Edge(Node n1, Node n2, RoadTypes type, int t)
    {
        this.n1 = n1;
        this.n2 = n2;
        this.type = type;
        this.priority = t;
		computeDirection ();
    }

	public Edge(Node n1, Node n2, RoadTypes type, int t, Edge pred, Edge succ)
	{
		this.n1 = n1;
		this.n2 = n2;
		this.type = type;
		this.priority = t;
		this.pred = pred;
		this.succ = succ;
		computeDirection ();
	}

	private void computeDirection(){
		this.direction = (new Vector2(this.n2.x, this.n2.y) - new Vector2(this.n1.x, this.n1.y));
	}

	public Vector2 getDirection(){
		return this.direction;
	}

    public Node[] getNodes()
    {
        return new Node[] { this.n1, this.n2 };
    }

    public RoadTypes getRoadType()
    {
        return this.type;
    }

    public int getTime()
    {
        return this.priority;
    }

	public void setPred(Edge e){
		this.pred = e;	
	}

	public void setSucc(Edge e){
		this.succ = e;
	}

	public Edge getPred(){
		return this.pred;	
	}

	public Edge getSucc(){
		return this.succ;	
	}

    public Node getOpposite(Node n)
    {
		if(n.Equals(n1))
        {
            return n2;
        }
        else
        {
            return n1;
        }
    }

	public override bool Equals(object obj)
	{
		var item = obj as Edge;

		if (item == null)
		{
			return false;
		}

		return ((this.n1 == item.n1 && this.n2 == item.n2) || (this.n1 == item.n2 && this.n2 == item.n1));
	}
    public override string ToString()
    {
        return "(" + n1 + ", " + n2 + ")";
    }

    // Added to remove an annoying error!
    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}
