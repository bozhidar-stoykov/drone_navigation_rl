using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class SpawnPositions : MonoBehaviour
{
    public List<Transform> spawnPostionsDrone;
    public List<Transform> spawnPostionsReward;
    public int numberOfEnvironments;

    void Awake()
    {
        spawnPostionsDrone = GenerateDroneSpawnPositions();
        spawnPostionsReward = GenerateRewardSpawnPositions(spawnPostionsDrone);
    }

    private List<Transform> GenerateDroneSpawnPositions()
    {
        List<Transform> spawnPositions = this.GetComponentsInChildren<Transform>().ToList<Transform>();
        List<Transform> chosenSpawnPositions = new List<Transform>();

        for (int i = 0; i < numberOfEnvironments; i++)
        {
            Transform chosenSpawnPos = GetRandomSpawnPosition(spawnPositions);
            chosenSpawnPositions.Add(chosenSpawnPos);
            spawnPositions.Remove(chosenSpawnPos);
        }

        return chosenSpawnPositions;
    }

    private List<Transform> GenerateRewardSpawnPositions(List<Transform> chosenSpawnPositions)
    {
        List<Transform> spawnPositions = this.GetComponentsInChildren<Transform>().ToList<Transform>();
        List<Transform> possibleGoalSpawnPositions = spawnPositions;
        List<Transform> chosenGoalSpawnPositions = new List<Transform>();

        // Remove drone spawn positions
        foreach (Transform chosenSpawnPos in chosenSpawnPositions)
        {
            possibleGoalSpawnPositions.Remove(chosenSpawnPos);
        }

        foreach (Transform spawnPosition in chosenSpawnPositions.ToList())
        {
            Transform chosenSpawnPos = GetRandomGoalSpawnPosition(spawnPosition, possibleGoalSpawnPositions);
            chosenGoalSpawnPositions.Add(chosenSpawnPos);
            possibleGoalSpawnPositions.Remove(chosenSpawnPos);
        }

        return chosenGoalSpawnPositions;
    }

    private Transform GetRandomSpawnPosition(List<Transform> spawnPositions)
    {
        int index = UnityEngine.Random.Range(0, spawnPositions.Count);
        return spawnPositions[index];
    }

    private Transform GetRandomGoalSpawnPosition(Transform droneSpawnPosition, List<Transform> possibleGoalSpawnPositions)
    {
        List<Transform> spawnPositionsInRange;

        spawnPositionsInRange = FilterPositionsInRange(droneSpawnPosition, possibleGoalSpawnPositions);
        int index = UnityEngine.Random.Range(0, spawnPositionsInRange.Count);


        return spawnPositionsInRange[index];
    }

    private List<Transform> FilterPositionsInRange(Transform droneSpawnPos, List<Transform> possibleGoalSpawnPositions)
    {
        List<Transform> spawnPositionsInRange = new List<Transform>();

        spawnPositionsInRange = possibleGoalSpawnPositions.FindAll(t => IsInRange(t, droneSpawnPos));
        return spawnPositionsInRange;
    }

    static bool IsInRange(Transform spawnPosition, Transform dronePosition)
    {
        float distance = Vector3.Distance(spawnPosition.position, dronePosition.position);
        return (distance < 750);
    }

    public void GenerateNewPositionOnIndex(int index)
    {
        Transform dronePos = GenerateDroneSpawnPositions()[index];
        Transform goalPosition = GenerateRewardSpawnPositions(spawnPostionsDrone)[index];
        spawnPostionsDrone[index] = dronePos;
        spawnPostionsReward[index] = goalPosition;
    }
}
