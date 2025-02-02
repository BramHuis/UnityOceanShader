using System.Collections.Generic;
using UnityEngine;

public static class OceanSOValidator {
    private static List<Ocean> cachedOceans = new List<Ocean>();
    private static bool isCached = false;
    private static float[] pointsToReadHeightFrom;

    // Cache all Ocean objects in the scene
    public static void CacheOceansInScene() {
        if (isCached) {
            return;
        }
        // Clear the previous cache
        cachedOceans.Clear();

        // Find all Ocean objects and store references
        Ocean[] oceans = Object.FindObjectsOfType<Ocean>();
        cachedOceans.AddRange(oceans);
        isCached = true;
    }

    // Validate all cached Ocean objects
    public static void ValidateAllCachedOceans() {
        // Ensure we only validate cached objects
        foreach (Ocean ocean in cachedOceans) {
            if (ocean != null) {
                ocean.OnValidate();
            }
        }
    }

    public static void InitializePointHeightArray(int arrayLength) {
        pointsToReadHeightFrom = new float[arrayLength];
    }

    public static void SetPointHeights(Vector2[] heights) {
        for (int i = 0; i < heights.Length; i++) {
            pointsToReadHeightFrom[i] = heights[i].x;
        }
    }

    public static float[] GetPointHeights() {
        return pointsToReadHeightFrom;
    }
}
