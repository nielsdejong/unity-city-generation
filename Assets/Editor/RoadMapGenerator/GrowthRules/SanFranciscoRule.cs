using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;

public class SanFranciscoRule : GrowthRule {

    public SanFranciscoRule(int seed) : base(seed) {
        // No initialization required.
    }

    public override void branchHighwayToHighway(ref List<Edge> branches, Edge oldEdge)
    {
        throw new NotImplementedException();
    }

    public override void branchRoadToStreet(ref List<Edge> branches, Edge oldEdge)
    {
        throw new NotImplementedException();
    }

    public override void branchStreetToStreet(ref List<Edge> branches, Edge oldEdge)
    {
        throw new NotImplementedException();
    }
}
