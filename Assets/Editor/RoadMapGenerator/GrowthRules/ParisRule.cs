using System.Collections.Generic;
using UnityEngine;
using CG = CityGenerator;

public class ParisRule : GrowthRule {

    static float noAccessPopThreshold = 0.98f;
    static float defaultCurveAngle = 5f;
    static float defaultHighwayBranchAngle = 45f;
    static float defaultBranchAngle = 90f;
    static float maxStraightAngle = 15f;
    static float maxBranchAngle = 90f - maxStraightAngle;

    public ParisRule(int seed) : base(seed) {
        // No initialization required.
    }

    public override void branchHighwayToHighway(ref List<Edge> branches, Edge oldEdge)
    {
        Vector2 startVector = new Vector2(oldEdge.n2.x, oldEdge.n2.y);
        Vector2 oldDirection = oldEdge.getDirection();

        float startPop = CoordinateHelper.worldToPop(oldEdge.n2.x, oldEdge.n2.y);
        if (startPop < noAccessPopThreshold)
        {
            List<Vector2> straightRays = this.castVectorsFromPoint(
                            startVector, oldDirection,
                            -maxStraightAngle,
                            maxStraightAngle,
                            RoadTypes.HIGHWAY, CG.rayCount);

            List<Vector2> leftRays = this.castVectorsFromPoint(
                    startVector, oldDirection,
                    -defaultHighwayBranchAngle,
                    -defaultHighwayBranchAngle + defaultCurveAngle,
                    RoadTypes.HIGHWAY, CG.rayCount);

            List<Vector2> rightRays = this.castVectorsFromPoint(
                    startVector, oldDirection,
                    defaultHighwayBranchAngle,
                    defaultHighwayBranchAngle - defaultCurveAngle,
                    RoadTypes.HIGHWAY, CG.rayCount);

            // check for peaks on the left
            List<Vector2> leftCheck = this.castVectorsFromPoint(
                startVector, oldDirection,
                -defaultHighwayBranchAngle - maxBranchAngle,
                -defaultHighwayBranchAngle + maxBranchAngle,
                RoadTypes.HIGHWAY, CG.rayCount);

            // check for peaks on the right
            List<Vector2> rightCheck = this.castVectorsFromPoint(
                startVector, oldDirection,
                defaultHighwayBranchAngle + maxBranchAngle,
                defaultHighwayBranchAngle - maxBranchAngle,
                RoadTypes.HIGHWAY, CG.rayCount);

            List<Vector2> curveRays = null;
            List<Vector2> branchOutRays = null;

            KeyValuePair<Vector2, float> curveRaysAndPopulation = new KeyValuePair<Vector2, float>();
            KeyValuePair<Vector2, float> branchRaysAndPopulation = new KeyValuePair<Vector2, float>();
            KeyValuePair<Vector2, float> oppositeBranchRaysAndPopulation = new KeyValuePair<Vector2, float>();

            KeyValuePair<Vector2, float> straightRaysAndPopulation = this.getBestRay(startVector, straightRays, RoadTypes.HIGHWAY);
            KeyValuePair<Vector2, float> leftRaysAndPopulation = this.getBestRay(startVector, leftRays, RoadTypes.HIGHWAY);
            KeyValuePair<Vector2, float> rightRaysAndPopulation = this.getBestRay(startVector, rightRays, RoadTypes.HIGHWAY);
        
            KeyValuePair<Vector2, float> leftCheckAndPopulation = this.getBestRay(startVector, leftCheck, RoadTypes.HIGHWAY);
            KeyValuePair<Vector2, float> rightCheckAndPopulation = this.getBestRay(startVector, rightCheck, RoadTypes.HIGHWAY);

            // if we have a peak right in front of us we should make the first big branch 
            if (straightRaysAndPopulation.Value > leftCheckAndPopulation.Value && straightRaysAndPopulation.Value > rightRaysAndPopulation.Value)
            {
                // right
                if (rightRaysAndPopulation.Value > CG.highwayPopThreshold && UnityEngine.Random.value < CG.highwayBranchProb)
                    branches.Add(new Edge(
                        oldEdge.n2, new Node(rightRaysAndPopulation.Key.x, rightRaysAndPopulation.Key.y),
                        RoadTypes.HIGHWAY, oldEdge.getTime() + CG.highwayPriority));
            
                // left
                if (leftRaysAndPopulation.Value > CG.highwayPopThreshold && UnityEngine.Random.value < CG.highwayBranchProb)
                    branches.Add(new Edge(
                        oldEdge.n2, new Node(leftRaysAndPopulation.Key.x, leftRaysAndPopulation.Key.y),
                        RoadTypes.HIGHWAY, oldEdge.getTime() + CG.highwayPriority));

                // straight (with a street)
                if (straightRaysAndPopulation.Value > CG.streetPopThreshold)
                    branches.Add(new Edge(
                        oldEdge.n2, new Node(straightRaysAndPopulation.Key.x, straightRaysAndPopulation.Key.y),
                        RoadTypes.HIGHWAY, oldEdge.getTime() + CG.highwayPriority));
            }
            // otherwise it means we already have a peak on one side, around which we should keep curving, so..
            else
            {
                // if we have a paris peak on the left...
                if (leftCheckAndPopulation.Value > rightCheckAndPopulation.Value)
                {
                    // we chose left as our branching direction
                    branchRaysAndPopulation = leftRaysAndPopulation;
                    oppositeBranchRaysAndPopulation = rightRaysAndPopulation;
                    branchOutRays = rightRays;

                    // we check slightly on the left
                    curveRays = this.castVectorsFromPoint(
                        startVector, oldDirection,
                        -defaultCurveAngle - maxStraightAngle,
                        -defaultCurveAngle,
                        RoadTypes.HIGHWAY, CG.rayCount);
                    curveRaysAndPopulation = this.getBestRay(startVector, curveRays, RoadTypes.HIGHWAY);
                }
                else 
                {
                    // we save right as branching direction
                    branchRaysAndPopulation = rightRaysAndPopulation;
                    oppositeBranchRaysAndPopulation = leftRaysAndPopulation;
                    branchOutRays = leftRays;

                    // we check slightly on the right
                    curveRays = this.castVectorsFromPoint(
                        startVector, oldDirection,
                        defaultCurveAngle + maxStraightAngle,
                        defaultCurveAngle,
                        RoadTypes.HIGHWAY, CG.rayCount);
                    curveRaysAndPopulation = this.getBestRay(startVector, curveRays, RoadTypes.HIGHWAY);
                }

                // once we know on which side we should curve...
                // if the value on the side is higher than the curving one... we have the center on that side and..
                if (branchRaysAndPopulation.Value > curveRaysAndPopulation.Value)
                {
                    // ... we curve around the center
                    if (curveRaysAndPopulation.Value > CG.highwayPopThreshold)
                        branches.Add(new Edge(
                            oldEdge.n2, new Node(curveRaysAndPopulation.Key.x, curveRaysAndPopulation.Key.y),
                            RoadTypes.HIGHWAY, oldEdge.getTime() + CG.highwayPriority)); 

                    // ... and we might branch towards it
                    if (branchRaysAndPopulation.Value > CG.streetPopThreshold 
                            && Random.value < CG.streetBranchProb)
                        branches.Add(new Edge(
                            oldEdge.n2, new Node(branchRaysAndPopulation.Key.x, branchRaysAndPopulation.Key.y),
                            RoadTypes.STREET, oldEdge.getTime() + CG.highwayPriority));
                }
                // otherwise we might go straight
                else
                {
                    straightRaysAndPopulation = this.getBestRay(startVector, straightRays, RoadTypes.HIGHWAY);
                    if (straightRaysAndPopulation.Value > CG.highwayPopThreshold)
                        branches.Add(new Edge(
                            oldEdge.n2, new Node(straightRaysAndPopulation.Key.x, straightRaysAndPopulation.Key.y),
                            RoadTypes.HIGHWAY, oldEdge.getTime() + CG.highwayPriority));
                }

                // ... in any case we might branch out of the center
                oppositeBranchRaysAndPopulation = this.getBestRay(startVector, branchOutRays, RoadTypes.HIGHWAY);
                if (oppositeBranchRaysAndPopulation.Value > CG.highwayPopThreshold 
                        && Random.value < CG.highwayBranchProb)
                    branches.Add(new Edge(
                        oldEdge.n2, new Node(oppositeBranchRaysAndPopulation.Key.x, oppositeBranchRaysAndPopulation.Key.y),
                        RoadTypes.HIGHWAY, oldEdge.getTime() + CG.highwayPriority));
            }
        }
    }

    public override void branchRoadToStreet(ref List<Edge> branches, Edge oldEdge)
    {
        Vector2 startVector = new Vector2(oldEdge.n2.x, oldEdge.n2.y);
        Vector2 oldDirection = oldEdge.getDirection();
        float startPop = CoordinateHelper.worldToPop(oldEdge.n2.x, oldEdge.n2.y);
        float streetBranchProb = (float)(Mathf.Exp(startPop) - 1) / (float)(Mathf.Exp(1) - 1);

        List<Vector2> leftRays = this.castVectorsFromPoint(
                startVector, oldDirection,
                -defaultBranchAngle,
                -defaultBranchAngle + defaultCurveAngle,
                RoadTypes.STREET, CG.rayCount);

        List<Vector2> rightRays = this.castVectorsFromPoint(
                startVector, oldDirection,
                defaultBranchAngle,
                defaultBranchAngle - defaultCurveAngle,
                RoadTypes.STREET, CG.rayCount);

        
            List<Vector2> straightRays = this.castVectorsFromPoint(
            startVector, oldDirection,
            -maxStraightAngle,
            maxStraightAngle,
            RoadTypes.STREET, CG.rayCount);

            List<Vector2> leftCheck = this.castVectorsFromPoint(
                    startVector, oldDirection,
                    -defaultBranchAngle - maxBranchAngle,
                    -defaultBranchAngle + maxBranchAngle,
                    RoadTypes.STREET, CG.rayCount);

            List<Vector2> rightCheck = this.castVectorsFromPoint(
                startVector, oldDirection,
                defaultBranchAngle + maxBranchAngle,
                defaultBranchAngle - maxBranchAngle,
                RoadTypes.STREET, CG.rayCount);


            List<Vector2> curveRays = null;
            List<Vector2> branchOutRays = null;

            KeyValuePair<Vector2, float> curveRaysAndPopulation = new KeyValuePair<Vector2, float>();
            KeyValuePair<Vector2, float> branchRaysAndPopulation = new KeyValuePair<Vector2, float>();
            KeyValuePair<Vector2, float> oppositeBranchRaysAndPopulation = new KeyValuePair<Vector2, float>();

            KeyValuePair<Vector2, float> leftRaysAndPopulation = this.getBestRay(startVector, leftRays, RoadTypes.STREET);
            KeyValuePair<Vector2, float> rightRaysAndPopulation = this.getBestRay(startVector, rightRays, RoadTypes.STREET);
            KeyValuePair<Vector2, float> straightRaysAndPopulation = this.getBestRay(startVector, straightRays, RoadTypes.STREET);
            KeyValuePair<Vector2, float> leftCheckAndPopulation = this.getBestRay(startVector, leftCheck, RoadTypes.STREET);
            KeyValuePair<Vector2, float> rightCheckAndPopulation = this.getBestRay(startVector, rightCheck, RoadTypes.STREET);

            // if we have a peak in front of us
            if (straightRaysAndPopulation.Value > leftCheckAndPopulation.Value 
                    && straightRaysAndPopulation.Value > rightCheckAndPopulation.Value)
            {
                // we might go right
                if (rightRaysAndPopulation.Value > CG.streetPopThreshold 
                        && Random.value < streetBranchProb)
                    branches.Add(new Edge(
                        oldEdge.n2, new Node(rightRaysAndPopulation.Key.x, rightRaysAndPopulation.Key.y),
                        RoadTypes.STREET, oldEdge.getTime() + CG.streetPriority));

                // we might go left
                if (leftRaysAndPopulation.Value > CG.streetPopThreshold
                        && Random.value < streetBranchProb)
                    branches.Add(new Edge(
                        oldEdge.n2, new Node(leftRaysAndPopulation.Key.x, leftRaysAndPopulation.Key.y),
                        RoadTypes.STREET, oldEdge.getTime() + CG.streetPriority));

                // we might go straight towards the center
                if (straightRaysAndPopulation.Value > CG.streetPopThreshold)
                    branches.Add(new Edge(
                        oldEdge.n2, new Node(straightRaysAndPopulation.Key.x, straightRaysAndPopulation.Key.y),
                        RoadTypes.STREET, oldEdge.getTime() + CG.streetPriority));
            }
            // otherwise we have a peak on one side and we should curve around it
            else
            {
                // we check first on the left: if we have a paris peak on the left...
                if (leftCheckAndPopulation.Value > rightCheckAndPopulation.Value)
                {
                    // ...we chose left as our branching direction
                    branchRaysAndPopulation = leftRaysAndPopulation;
                    oppositeBranchRaysAndPopulation = rightRaysAndPopulation;
                    branchOutRays = rightRays;

                    // and we check slightly on the left
                    curveRays = this.castVectorsFromPoint(
                        startVector, oldDirection,
                        -defaultCurveAngle - maxStraightAngle,
                        -defaultCurveAngle,
                        RoadTypes.STREET, CG.rayCount);
                    curveRaysAndPopulation = this.getBestRay(startVector, curveRays, RoadTypes.STREET);
                }
                // otherwise we check on the right: if we have a paris peak on the right...
                else
                {
                    // ....we save right as branching direction
                    branchRaysAndPopulation = rightRaysAndPopulation;
                    oppositeBranchRaysAndPopulation = leftRaysAndPopulation;
                    branchOutRays = leftRays;

                    // and we check slightly on the right
                    curveRays = this.castVectorsFromPoint(
                        startVector, oldDirection,
                        defaultCurveAngle + maxStraightAngle,
                        defaultCurveAngle,
                        RoadTypes.STREET, CG.rayCount);
                    curveRaysAndPopulation = this.getBestRay(startVector, curveRays, RoadTypes.STREET);
                }

                // at this point if the branching direction value is higher than the curving one... we really have a peak on the side
                if (branchRaysAndPopulation.Value > curveRaysAndPopulation.Value)
                {
                    // ..we curve around the center
                    if (curveRaysAndPopulation.Value > CG.streetPopThreshold)
                        branches.Add(new Edge(
                            oldEdge.n2, new Node(curveRaysAndPopulation.Key.x, curveRaysAndPopulation.Key.y),
                            RoadTypes.STREET, oldEdge.getTime() + CG.streetPriority));

                    if (branchRaysAndPopulation.Value > CG.streetPopThreshold 
                            && UnityEngine.Random.value < streetBranchProb)
                        branches.Add(new Edge(
                            oldEdge.n2, new Node(branchRaysAndPopulation.Key.x, branchRaysAndPopulation.Key.y),
                            RoadTypes.STREET, oldEdge.getTime() + CG.streetPriority));
                }

                // in any case we might branch out of the center (use a basic rule to allow exit from a paris zone)
                oppositeBranchRaysAndPopulation = this.getBestRay(startVector, branchOutRays, RoadTypes.STREET);
                if (oppositeBranchRaysAndPopulation.Value > CG.streetPopThreshold 
                        && UnityEngine.Random.value < streetBranchProb)
                    branches.Add(new Edge(
                        oldEdge.n2, new Node(oppositeBranchRaysAndPopulation.Key.x, oppositeBranchRaysAndPopulation.Key.y),
                        RoadTypes.STREET, oldEdge.getTime() + CG.streetPriority));
            }
    }

    public override void branchStreetToStreet(ref List<Edge> branches, Edge oldEdge)
    {
        //
    }
}
