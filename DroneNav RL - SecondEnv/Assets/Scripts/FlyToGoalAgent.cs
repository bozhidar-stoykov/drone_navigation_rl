using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.MLAgents;
using Unity.MLAgents.Actuators;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class AddScoreEventArgs : System.EventArgs
{
    public float Score { get; set; }
}

public class FlyToGoalAgent : Agent
{
    [Header("Sensors")]
    public Rigidbody drone;
    public Transform goalTransform;

    private DroneController controller;
    private int episodeLenght;
    private bool hasCollided = false;
    private bool reachedReward = false;
    private bool crashed = false;

    private void Awake()
    {
        controller = GetComponent<DroneController>();
        episodeLenght = 3600;
    }

    private void Start()
    {
        controller.OnAddScore += OnScoreEvent;
        controller.OnOutOfMap += OnOutOfMapEvent;
    }

    public override void OnEpisodeBegin()
    {
        SetRewardRandomPosition(true);
        controller.ResetDrone();
        hasCollided = false;
        reachedReward = false;
        crashed = false;
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        sensor.AddObservation(drone.velocity);
        sensor.AddObservation(drone.angularVelocity);
        sensor.AddObservation(drone.transform.localPosition);
        sensor.AddObservation(drone.transform.rotation.eulerAngles);
        sensor.AddObservation(goalTransform.localPosition);
    }
    
    public override void OnActionReceived(ActionBuffers actions)
    {
        float flTorque = 0;
        float frTorque = 0;
        float rlTorque = 0;
        float rrTorque = 0;

        flTorque = actions.ContinuousActions[0];
        frTorque = actions.ContinuousActions[1];
        rlTorque = actions.ContinuousActions[2];
        rrTorque = actions.ContinuousActions[3];

        controller.SetInput(flTorque, frTorque, rlTorque, rrTorque);

        //if (StepCount == MaxStep - 1)
        //    Debug.Log("Reward: " + GetCumulativeReward());
    }

    private void OnTriggerEnter(Collider collider)
    {
        if (collider.gameObject.tag == "Wall")
        {
            if (!hasCollided)
            {
                hasCollided = true;
                AddReward(-1100f);
            }

            StartCoroutine(waitBeforeEnd());
        }

        if (collider.gameObject.tag == "Reward")
        {
            if (!reachedReward)
            {
                reachedReward = true;
                AddReward(20000f);
                IncreaseEpisodeLenght();
            }

            StartCoroutine(waitBeforeRewardReset());
        } 
    }

    private void OnCollisionEnter(Collision collision)
    {
        float threshold = 32f;
        if (GetCumulativeReward() > 10000)
            threshold = 14f;
        else if (GetCumulativeReward() > 20000)
            threshold = 8.5f;
        else if (GetCumulativeReward() > 30000)
            threshold = 3.5f;
        //else if (GetCumulativeReward() > 30000)
        //    threshold = 2.5f;

        if (!crashed && collision.relativeVelocity.magnitude > threshold &&
            collision.collider.gameObject.tag == "Ground")
        {
            crashed = true;
            AddReward(-1000f);
            StartCoroutine(waitBeforeEnd());
        }
    }

    private void OnScoreEvent(object sender, AddScoreEventArgs e)
    {
        AddReward(e.Score);
    }

    private void OnOutOfMapEvent(object sender, System.EventArgs e)
    {
        AddReward(-600f);
        StartCoroutine(waitBeforeEnd());
    }

    public override void Heuristic(in ActionBuffers actionsOut)
    {
        float frontLeftPropThrotle = 0;
        float frontRightPropThrotle = 0;
        float rearLeftPropThrotle = 0;
        float rearRightPropThrotle = 0;

        if (Input.GetKey(KeyCode.A)) frontLeftPropThrotle = 1;
        if (Input.GetKey(KeyCode.S)) frontRightPropThrotle = 1;
        if (Input.GetKey(KeyCode.D)) rearLeftPropThrotle = 1;
        if (Input.GetKey(KeyCode.F)) rearRightPropThrotle = 1;

        if (Input.GetKey(KeyCode.Z)) frontLeftPropThrotle = -1;
        if (Input.GetKey(KeyCode.X)) frontRightPropThrotle = -1;
        if (Input.GetKey(KeyCode.C)) rearLeftPropThrotle = -1;
        if (Input.GetKey(KeyCode.V)) rearRightPropThrotle = -1;

        ActionSegment<float> continuousActions = actionsOut.ContinuousActions;
        continuousActions[0] = frontLeftPropThrotle;
        continuousActions[1] = frontRightPropThrotle;
        continuousActions[2] = rearLeftPropThrotle;
        continuousActions[3] = rearRightPropThrotle;
    }

    private void SetRewardRandomPosition(bool initial)
    {
        float x;
        float y = Random.Range(5, 14f);
        float z;

        if (initial)
        {
            (float, float) xz = GenerateRandomZandX();
            x = xz.Item1;
            z = xz.Item2;
        }

        else
        {
            float absX = Mathf.Abs(goalTransform.localPosition.x);
            float absZ = Mathf.Abs(goalTransform.localPosition.z);

            if (absX > 6 && absZ > 6)
            {
                x = Random.Range(-5f, 5f);
                z = Random.Range(-5f, 5f);
            }

            else
            {
                (float, float) xz = GenerateRandomZandX();
                x = xz.Item1;
                z = xz.Item2;
            }
        }

        // Increase y position with Reward
        if (GetCumulativeReward() > 20000)
            y = Random.Range(9, 20f);
        else if (GetCumulativeReward() > 8000)
            y = Random.Range(7, 17f);

        
        goalTransform.localPosition = new Vector3(x, y, z);
    }

    private (float, float) GenerateRandomZandX()
    {
        float x = 0;
        float z = 0;
        float min = 145f;
        float max = 170f;
        int side = Random.Range(0, 4);

        switch (side)
        {
            case 0:
                x = Random.Range(-max, max);
                z = Random.Range(min, max);
                break;
            case 1:
                x = Random.Range(-max, max);
                z = Random.Range(-max, -min);
                break;
            case 2:
                x = Random.Range(max, min);
                z = Random.Range(-max, max);
                break;
            case 3:
                x = Random.Range(-max, -min);
                z = Random.Range(-max, max);
                break;
        }

        return (x, z);
    }

    private void IncreaseEpisodeLenght()
    {
        if (episodeLenght < 9000)   // 3 min 00 sec
        {
            episodeLenght = episodeLenght + 1500; // + 30 sec
        }

        else
            episodeLenght = 9000;

        MaxStep = episodeLenght;
    }

    IEnumerator waitBeforeEnd()
    {
        yield return new WaitForSeconds(0.001f);
        hasCollided = false;
        EndEpisode();
    }

    IEnumerator waitBeforeRewardReset()
    {
        yield return new WaitForSeconds(0.001f);
        SetRewardRandomPosition(false);
        reachedReward = false;
    }
}
