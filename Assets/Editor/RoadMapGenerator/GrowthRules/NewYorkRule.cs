using System.Collections.Generic;
using UnityEngine;
using CG = CityGenerator;

public class NewYorkRule : GrowthRule
{
    static float branchAngle = 90f;
    static int rayCount = 1;

    public NewYorkRule(int seed) : base(seed)
    {
        // No initialization required.
    }

    public override void branchHighwayToHighway(ref List<Edge> branches, Edge oldEdge)
    {
        // Generate a spectrum of rays in a generally forward direction, and pick the ray with the highest population.
        Vector2 oldDirection = oldEdge.getDirection().normalized;
        Vector2 startVector = new Vector2(oldEdge.n2.x, oldEdge.n2.y);

        List<Vector2> rays = this.castVectorsFromPoint(
            startVector, oldDirection, 
            0, 0,
            RoadTypes.HIGHWAY, rayCount);

        KeyValuePair<Vector2, float> rayAndPopulation = this.getBestRay(startVector, rays, RoadTypes.HIGHWAY);
        if (rayAndPopulation.Value > CG.highwayPopThreshold)
        {
            branches.Add(new Edge(
                oldEdge.n2, new Node(rayAndPopulation.Key.x, rayAndPopulation.Key.y), 
                RoadTypes.HIGHWAY, oldEdge.getTime() + CG.highwayPriority));
        }

        /// Branching sideways ///
        // Branch to left
        List<Vector2> leftRays = this.castVectorsFromPoint(
            startVector, oldDirection, 
            -branchAngle,
            -branchAngle,
            RoadTypes.HIGHWAY, rayCount);

        KeyValuePair<Vector2, float> leftRayAndPopulation = this.getBestRay(startVector, leftRays, RoadTypes.HIGHWAY);
        if (leftRayAndPopulation.Value > CG.highwayPopThreshold && Random.value < CG.highwayBranchProb)
        {
            branches.Add(new Edge(
                oldEdge.n2, new Node(leftRayAndPopulation.Key.x, leftRayAndPopulation.Key.y), 
                RoadTypes.HIGHWAY, oldEdge.getTime() + CG.highwayPriority));
        }
        
        // Branch to right
        List<Vector2> rightRays = this.castVectorsFromPoint(
            startVector, oldDirection, 
            -branchAngle + 180, 
            -branchAngle + 180,
            RoadTypes.HIGHWAY, rayCount);

        KeyValuePair<Vector2, float> rightRayAndPopulation = this.getBestRay(startVector, rightRays, RoadTypes.HIGHWAY);
        if (rightRayAndPopulation.Value > CG.highwayPopThreshold && Random.value < CG.highwayBranchProb)
        {
            branches.Add(new Edge(
                oldEdge.n2, new Node(rightRayAndPopulation.Key.x, rightRayAndPopulation.Key.y), 
                RoadTypes.HIGHWAY, oldEdge.getTime() + CG.highwayPriority));
        }
        
    }

    public override void branchRoadToStreet(ref List<Edge> branches, Edge oldEdge)
    {
        Vector2 startVector = new Vector2(oldEdge.n2.x, oldEdge.n2.y);
        Vector2 oldDirection = oldEdge.getDirection();
        float startPop = CoordinateHelper.worldToPop(oldEdge.n2.x, oldEdge.n2.y);
        float streetBranchProb = (float)(Mathf.Exp(startPop) - 1) / (float)(Mathf.Exp(1) - 1);
        if (CityGeneratorUI.DebugMode) Debug.Log("Street at (" + oldEdge.n2.x + ", " + oldEdge.n2.y + "). Branch prob: " + streetBranchProb);

        /// Branching sideways ///
        // Branch to left
        List<Vector2> leftRays = this.castVectorsFromPoint(
            startVector, oldDirection, 
            -branchAngle, 
            -branchAngle,
            RoadTypes.STREET, rayCount);

        KeyValuePair<Vector2, float> leftRayAndPopulation = this.getBestRay(startVector, leftRays, RoadTypes.STREET);
        if (leftRayAndPopulation.Value > CG.streetPopThreshold
                && Random.value < streetBranchProb)
        {
            if (CityGeneratorUI.DebugMode) Debug.Log("branch left!");
            branches.Add(new Edge(
                oldEdge.n2, new Node(leftRayAndPopulation.Key.x, leftRayAndPopulation.Key.y),
                RoadTypes.STREET, oldEdge.getTime() + CG.streetPriority));
        }
        
        
        // Branch to right
        List<Vector2> rightRays = this.castVectorsFromPoint(
            startVector, oldDirection, 
            -branchAngle + 180, 
            -branchAngle + 180,
            RoadTypes.STREET, rayCount);

        KeyValuePair<Vector2, float> rightRayAndPopulation = this.getBestRay(startVector, rightRays, RoadTypes.STREET);
        if (rightRayAndPopulation.Value > CG.streetPopThreshold
                && Random.value < streetBranchProb)
        {
            if(CityGeneratorUI.DebugMode)  Debug.Log("branch right!");
            branches.Add(new Edge(
                oldEdge.n2, new Node(rightRayAndPopulation.Key.x, rightRayAndPopulation.Key.y),
                RoadTypes.STREET, oldEdge.getTime() + CG.streetPriority));
        }
    }

    public override void branchStreetToStreet(ref List<Edge> branches, Edge oldEdge)
    {
        Vector2 startVector = new Vector2(oldEdge.n2.x, oldEdge.n2.y);
        Vector2 oldDirection = oldEdge.getDirection();

        List<Vector2> rays = this.castVectorsFromPoint(
            startVector, oldDirection, 
            0, 0,
            RoadTypes.STREET, rayCount);

        KeyValuePair<Vector2, float> rayAndPopulation = this.getBestRay(startVector, rays, RoadTypes.STREET);
        if (rayAndPopulation.Value > CG.streetPopThreshold)
        {
            branches.Add(new Edge(
                oldEdge.n2, new Node(rayAndPopulation.Key.x, rayAndPopulation.Key.y), 
                RoadTypes.STREET, oldEdge.getTime() + CG.streetPriority));
        }
    }
}
