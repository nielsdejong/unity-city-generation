using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public enum NodeTypes {
	STRAIGHT, INTERSECTION, ROADEND
}

public class Node {
	private float _x;
	public float x { 
		get{ 
			return _x;
		} 

		set { 
			this._x = value;
			this._pos.x = value;
		}
	}

	private float _y;
	public float y { 
		get{ 
			return _y;
		} 

		set{ 
			this._y = value;
			this._pos.y = value;
		}
	}

	private Vector2 _pos;
	public Vector2 pos { 
		get{
			return _pos;
		}
		set{ 
			this._pos = value;
			this._x = value.x;
			this._y = value.y;
		}
	}

	public NodeTypes nodeType { get; set; }
	public List<Edge> edges;
    public bool onHighway = false;
	public Node(float x, float y){
		this._x = x;
		this._y = y;
		this._pos = new Vector2 (x, y);
		this.edges = new List<Edge>();
	}

	public Node(Vector2 v){
		this._x = v.x;
		this._y = v.y;
		this._pos = new Vector2 (v.x, v.y);
		this.edges = new List<Edge> ();
	}
		
	public override bool Equals(object obj)
	{
		var item = obj as Node;

		if (item == null || GetType() != obj.GetType())
		{
			return false;
		}

		return Mathf.Approximately(this._x, item.x) && Mathf.Approximately(this._y, item.y);
	}

    // Added to remove an annoying error!
    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    public override string ToString()
    {
        return "(" + _x + ", " + _y + ")";
    }

}
