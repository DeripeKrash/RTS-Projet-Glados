using System;
using UnityEngine;
using System.Collections.Generic;
using System.Collections.ObjectModel;

// FactoryLocationFinder is responsible for searching a valid location to build a factory,
// according to its size and the surrounding buildings. To find a location, we proceed as follow:
// 1. Get the radius of the circle the new factory is circumscribed in
// 2. For each existing factory, do the same and add it to the new factory's radius
// 3. Search this radius with n radialSteps (every 360° / radialSteps degrees), and check
//    whether this position is valid for the factory to build
// 4. If no position was found, expand the radius and search, until the maximum radius is reached
// Whether a position is valid or not is determined by whether its x value is finite or NaN
[Serializable]
public class FactoryLocationFinder
{
    // ========== Inspector fields ==========
    [SerializeField]
    [Tooltip("Multiplier applied to each factory radius, around which a location is searched, to defined the maximum search radius")]
    float maxSearchRadiusMultiplier = 2f;

    [SerializeField]
    [Tooltip("The offset the multiplier is increased by until it reaches maxSearchRadiusMultiplier")]
    float maxSearchRadiusMultiplierStep = 0.2f;

    [SerializeField]
    [Min(6f)]
    [Tooltip("Number of stops on the radius. 10 means position")]
    float radialSteps = 10f;

    [SerializeField]
    [Tooltip("The vertical offset applied to a tested position, in order to prevent collision with the ground")]
    float testPosVerticalOffset = 0.1f;

    // ========== Internal ==========
    float[] cosAngles;
    float[] sinAngles;
    int     maxSteps;


    // ========== Methods ==========
    public void PreComputeValues()
    {
        float offset = 2f * Mathf.PI / radialSteps;
        float angle  = offset;

        maxSteps  = Mathf.FloorToInt(radialSteps);
        cosAngles = new float[maxSteps];
        sinAngles = new float[maxSteps];

        cosAngles[0] = 1f;
        sinAngles[0] = 0f;

        for (int i = 1; i < maxSteps; i++)
        {
            cosAngles[i] = Mathf.Cos(angle);
            sinAngles[i] = Mathf.Sin(angle);
            
            angle += offset;
        }
    }

    Vector3 FindLocationAround(GameObject factory, Vector3 selectedBounds, float selectedFactoryRadius)
    {
        Vector3    bounds       = factory.GetComponent<BoxCollider>().size;
        float      startRadius  = selectedFactoryRadius + Math.FlatDiagonalLength(bounds) * .5f;
        float      endRadius    = startRadius * maxSearchRadiusMultiplier;
        float      radiusOffset = maxSearchRadiusMultiplierStep * startRadius;

        for (float radius = startRadius; radius <= endRadius; radius += radiusOffset)
        {
            for (int i = 0; i < maxSteps; i++)
            {
                float x = factory.transform.position.x + radius * cosAngles[i];
                float z = factory.transform.position.z + radius * sinAngles[i];

                Vector3 halfExtents    = selectedBounds * .5f;
                Vector3 testedPos      = new Vector3(x, factory.transform.position.y, z);
                Vector3 testedPosCheck = new Vector3(testedPos.x,
                                                     testedPos.y + testPosVerticalOffset + halfExtents.y,
                                                     testedPos.z);
                bool    overlapped  = Physics.CheckBox(testedPosCheck, halfExtents);

                if (!overlapped)
                {
                    return testedPos;
                }
            }
        }

        return new Vector3(float.NaN, float.NaN, float.NaN);
    }

    public Vector3 FindLocation(ReadOnlyCollection<Factory> factories, GameObject selectedFactory)
    {
        // Get bounds of selected factory type
        // Compiler raises an error if newLocation is not initialized.
        // Why C#, WHY?
        Vector3 newLocation           = new Vector3(float.NaN, float.NaN, float.NaN);
        Vector3 selectedBounds        = selectedFactory.GetComponent<BoxCollider>().size;
        float   selectedFactoryRadius = Math.FlatDiagonalLength(selectedBounds) * .5f;

        int factoCount = factories.Count;
        for (int i = 0; i < factoCount; i++)
        {
            newLocation = FindLocationAround(factories[i].gameObject,
                                             selectedBounds,
                                             selectedFactoryRadius);

            // A warning in Unity brings to this line. Do not change the comparison,
            // it is a cheap way to check for NaN, not a mistake
            if (!float.IsNaN(newLocation.x))
            {
                return newLocation;
            }
        }

        return newLocation;
    }
}