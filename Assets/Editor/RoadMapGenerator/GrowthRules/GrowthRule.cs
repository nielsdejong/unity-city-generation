using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

public abstract class GrowthRule {

    /// <summary>
    /// Initializes a new Growth Rule with a seed for the random number generator.
    /// </summary>
    /// <param name="seed">The seed</param>
    public GrowthRule(int seed)
    {  
        //Random.InitState(seed);
    }

    /// <summary>
    /// Use the growth rule to generate branch(es) from a highway to a new highway.
    /// This can be either:
    /// - A straight highway "branch" (e.g. the highway continuing forward, which always happens)
    /// - A left highway branch (the highway taking a left turn with some degree of angle modificiation)
    /// - A right highway branch (the highway taking a right turn with some degree of angle modificiation)
    /// </summary>
    /// <param name="branches"></param>
    /// <param name="startNode"></param>
    /// <param name="oldDirection"></param>
    public abstract void branchHighwayToHighway(ref List<Edge> branches, Edge oldEdge);

    /// <summary>
    /// Use the growth rule to generate branch(es) from a street to a new street.
    /// This can be only:
    /// - A forward street "branch" (e.g. the street continuing forward)
    /// </summary>
    /// <param name="branches"></param>
    /// <param name="startNode"></param>
    /// <param name="oldDirection"></param>
    public abstract void branchStreetToStreet(ref List<Edge> branches, Edge oldEdge);

    /// <summary>
    /// Use the growth rule to generate branch(es) from a (highway or street) to a new street.
    ///  This can be either:
    /// - A left street branch (the street taking a left turn with some degree of angle modificiation)
    /// - A right street branch (the street taking a right turn with some degree of angle modificiation)
    /// </summary>
    /// <param name="branches"></param>
    /// <param name="startNode"></param>
    /// <param name="oldDirection"></param>
    public abstract void branchRoadToStreet(ref List<Edge> branches, Edge oldEdge);

    /// <summary>
    /// Branches a new Vector2 starting at Vector2 point. The new branch is based on an old direction, which is then changed by a random value
    /// in between minAngle and maxAngle. The length is the length of the new edge that is created.
    /// </summary>
    /// <param name="point"></param>
    /// <param name="directionVector"></param>
    /// <param name="minAngle"></param>
    /// <param name="maxAngle"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    protected virtual Vector2 branchVectorFromPoint(Vector2 point, Vector2 directionVector, float minAngle, float maxAngle, RoadTypes roadType)
    {
        float diff = maxAngle - minAngle;
        float randomAngle = Random.value * diff;
        Vector2 newDirectionVector = Vector2.zero + directionVector;
        newDirectionVector.Normalize();
        newDirectionVector *= (roadType == RoadTypes.HIGHWAY) ?
            CityGenerator.highwayMinLength * CityGenerator.highwayLookAhead:
            CityGenerator.streetMinLength * CityGenerator.streetLookAhead;
        Vector2 vector = point + (Vector2)(Quaternion.Euler(0, 0, minAngle + randomAngle) * newDirectionVector);
        return vector;
    }

    /// <summary>
    /// Casts a number of random rays within a given range, using the branchVectorFromPoint method.
    /// </summary>
    /// <param name="point"></param>
    /// <param name="oldDirection"></param>
    /// <param name="minAngle"></param>
    /// <param name="maxAngle"></param>
    /// <param name="length"></param>
    /// <param name="rayCount"></param>
    /// <returns></returns>
    public List<Vector2> castVectorsFromPoint(Vector2 point, Vector2 oldDirection, float minAngle, float maxAngle, RoadTypes roadType, int rayCount)
    {
        float totalRange = maxAngle - minAngle;
        // If the total angle range is X, the angle range for every ray is (X/rayCount)
        float rayRange = totalRange / (float)rayCount;

        // Create a number of rays
        List<Vector2> rays = new List<Vector2>();
        for(int ray = 0; ray < rayCount; ray++)
        {
            float minAngleForRay = minAngle + rayRange * ray;
            float maxAngleForRay = minAngle + rayRange * (ray + 1);
            rays.Add(this.branchVectorFromPoint(point, oldDirection, minAngleForRay, maxAngleForRay, roadType));
        }
        return rays;
    }


    /// <summary>
    /// Gets the ray that results in the highest population and return it together with the population value.
    /// </summary>
    /// <param name="rayEndPoints">List of Vector2 points representing the ends of rays.</param>
    /// <returns></returns>
    public virtual KeyValuePair<Vector2, float> getBestRay(Vector2 point, List<Vector2> rayEndPoints, RoadTypes roadType)
    {
        float maxPopulation = float.NegativeInfinity;
        Vector2 bestDirection = Vector2.zero;
        
        // Loop through all rays and find the one that results in the highest population density.
        for (int i = 0; i < rayEndPoints.Count; i++)
        {
            float rayLength = (roadType == RoadTypes.HIGHWAY) ? 
                CityGenerator.highwayMinLength * CityGenerator.highwayLookAhead :
                CityGenerator.streetMinLength * CityGenerator.streetLookAhead;
            int raySamples = (int)Mathf.Sqrt(rayLength);
            float rayStepSize = (float)rayLength / (float)raySamples;

            // take samples along the ray.
            Vector2 rayDirection = getRayDirection(point, rayEndPoints[i]);
            float rayPopulation = 0f;

            Vector2 directionPoint = Vector2.zero;
            bool isDirectionSet = false;
            for (int sample = 1; sample <= raySamples; sample++)
            {
				//save the first point over the ray that is at a suitable distance for a road/highway
				Vector2 sampleEndPoint = point + rayDirection * sample * rayStepSize;
                if ((roadType == RoadTypes.HIGHWAY && sample * rayStepSize >= CityGenerator.highwayMinLength ||
                    roadType != RoadTypes.HIGHWAY && sample * rayStepSize >= CityGenerator.streetMinLength) && !isDirectionSet)
                {
                    directionPoint = sampleEndPoint;
                    isDirectionSet = true;
                }   

				if (!CoordinateHelper.validEndPoint (sampleEndPoint)) {
                    raySamples = sample -1;
                    break;
				}

                float popValue = CoordinateHelper.worldToPop(sampleEndPoint.x, sampleEndPoint.y);
                /*popValue = (roadType == RoadTypes.HIGHWAY) ?
                    ((float)sample / (float)raySamples) * popValue :            // more weight to further points
                    (1 - (float)(sample-1) / (float)raySamples) * popValue;     // more weight to closer points*/
                rayPopulation = (popValue > rayPopulation) ? popValue : rayPopulation;
            }

            if (rayPopulation > maxPopulation && isDirectionSet)
            {
                maxPopulation = rayPopulation;
                bestDirection = directionPoint;
            }
        }
        return new KeyValuePair<Vector2, float>(bestDirection, maxPopulation);
    }

    public Vector2 getRayDirection(Vector2 point, Vector2 rayFragment)
    {
        return  (new Vector2(rayFragment.x, rayFragment.y) - new Vector2(point.x, point.y)).normalized;
    }
}
