using System.Collections;
using System.Collections.Generic;
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
    private int highestReward;

    private void Awake()
    {
        controller = GetComponent<DroneController>();
        episodeLenght = 2000;
        highestReward = 0;
    }

    private void Start()
    {
        controller.OnDistanceCloser += OnDistanceCloser;
        controller.OnDistanceFurther += OnDistanceFurther;
        controller.OnAddScore += OnScoreEvent;
    }

    public override void OnEpisodeBegin()
    {
        float x = Random.Range(-13f, 13);
        float y = Random.Range(2, 7f);
        float z = Random.Range(-13f, 13);
        goalTransform.localPosition = new Vector3(x, y, z);
        controller.ResetDrone();
    }

    public override void CollectObservations(VectorSensor sensor)
    {
        Vector3 direction = goalTransform.forward - transform.forward ;
        float distance = Vector3.Distance(transform.position, goalTransform.position);
        sensor.AddObservation(drone.velocity.magnitude);
        sensor.AddObservation(drone.angularVelocity.magnitude);
        sensor.AddObservation(drone.transform.rotation.eulerAngles);
        sensor.AddObservation(direction);
        sensor.AddObservation(distance);
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

        if (StepCount == MaxStep - 1)
            Debug.Log("Reward: " + GetCumulativeReward());
    }

    private void OnTriggerEnter(Collider collider)
    {
        if (collider.gameObject.tag == "Wall")
        {
            if (!hasCollided)
            {
                hasCollided = true;
                AddReward(-400f);
            }

            StartCoroutine(waitBeforeEnd());
        }

        if (collider.gameObject.tag == "Reward")
        {
            if (!reachedReward)
            {
                reachedReward = true;
                AddReward(1600f);
            }

            StartCoroutine(waitBeforeEnd());
        }
    }

    private void OnDistanceCloser(object sender, System.EventArgs e)
    {
        AddReward(1f);
    }

    private void OnDistanceFurther(object sender, System.EventArgs e)
    {
        AddReward(-1f);
    }

    private void OnScoreEvent(object sender, AddScoreEventArgs e)
    {
        AddReward(e.Score);
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

    IEnumerator waitBeforeEnd()
    {
        yield return new WaitForSeconds(0.001f);
        hasCollided = false;
        EndEpisode();
    }
}
