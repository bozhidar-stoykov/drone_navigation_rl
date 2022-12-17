using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class DroneController : MonoBehaviour
{
    private Quaternion startRotation;
    private float frontLeftPropThrotle;
    private float frontRightPropThrotle;
    private float rearLeftPropThrotle;
    private float rearRightPropThrotle;
    private float previousDistance;

    public event EventHandler OnOutOfMap;
    public event EventHandler<AddScoreEventArgs> OnAddScore;
    private FlyToGoalAgent agent;


    [Header("Physics")]
    public Transform frontLeftPropTransform;
    public Transform frontRightPropTransform;
    public Transform rearLeftPropTransform;
    public Transform rearRightPropTransform;
    public Transform fLPPTransform;
    public Transform fRPPTransform;
    public Transform rLPPTransform;
    public Transform rRPPTransform;
    public Transform reward;
    public Rigidbody rigidbody;
    public Vector3 startPosition;
    public int speed=6;

    enum Propeller
    {
        FrontLeft,
        FrontRight,
        RearLeft,
        RearRight
    }

    void Start()
    {
        previousDistance = 0;
        startRotation = transform.rotation;
        startPosition = transform.position;
        speed = 13;
        agent = gameObject.GetComponent<FlyToGoalAgent>();
    }

    void FixedUpdate()
    {
        CheckGrounded();
        CheckOrientation();
        CheckMapBounderies();
        CheckDistanceToReward();
        ThrottlePropeller(Propeller.FrontLeft, frontLeftPropThrotle, speed);
        ThrottlePropeller(Propeller.FrontRight, frontRightPropThrotle, speed);
        ThrottlePropeller(Propeller.RearLeft, rearLeftPropThrotle, speed);
        ThrottlePropeller(Propeller.RearRight, rearRightPropThrotle, speed);
    }

    public void GetInput()
    {
        frontLeftPropThrotle = Input.GetKey(KeyCode.A) ? 1 : (Input.GetKey(KeyCode.Z) ? -1 : 0);
        frontRightPropThrotle = Input.GetKey(KeyCode.S) ? 1 : (Input.GetKey(KeyCode.X) ? -1 : 0);
        rearLeftPropThrotle = Input.GetKey(KeyCode.D) ? 1 : (Input.GetKey(KeyCode.C) ? -1 : 0);
        rearRightPropThrotle = Input.GetKey(KeyCode.F) ? 1 : (Input.GetKey(KeyCode.V) ? -1 : 0);
    }

    public void SetInput(float flTorque, float frTorque, float rlTorque, float rrTorque)
    {
        frontLeftPropThrotle = flTorque;
        frontRightPropThrotle = frTorque;
        rearLeftPropThrotle = rlTorque;
        rearRightPropThrotle = rrTorque;
    }

    public void ResetDrone()
    {
        transform.position = startPosition;
        transform.rotation = startRotation;
        rigidbody.velocity = new Vector3(0, 0, 0);
        rigidbody.angularVelocity = new Vector3(0, 0, 0);
    }

    private void ThrottlePropeller(Propeller propeller, float throttle, int speed = 6, int rotationSpeed = 33)
    {
        Transform propTrans;
        Transform propCenterPoint;

        (Transform, Transform) propInfo = GetPropellerInfo(propeller);
        propTrans = propInfo.Item1;
        propCenterPoint = propInfo.Item2;
        
        // Throttle the propeler if the key is pressed, applying a force to the drone depending on which propeler is being throttled and the orientation of the drone
        if (throttle > 0)
        {
            propTrans.Rotate(Vector3.up * rotationSpeed);
            rigidbody.AddForceAtPosition(transform.up * speed * throttle, propCenterPoint.position);
        }
        else if (throttle < 0)
        {
            propTrans.Rotate(Vector3.up * -rotationSpeed);
            rigidbody.AddForceAtPosition(-transform.up * speed * Math.Abs(throttle), propCenterPoint.position);
        }
    }

    private (Transform, Transform) GetPropellerInfo(Propeller propeller)
    {
        Transform propTrans;
        Transform propCenterPoint;

        switch (propeller)
        {
            case Propeller.FrontLeft:
                propTrans = frontLeftPropTransform;
                propCenterPoint = fLPPTransform;
                break;
            case Propeller.FrontRight:
                propTrans = frontRightPropTransform;
                propCenterPoint = fRPPTransform;
                break;
            case Propeller.RearLeft:
                propTrans = rearLeftPropTransform;
                propCenterPoint = rLPPTransform;
                break;
            case Propeller.RearRight:
                propTrans = rearRightPropTransform;
                propCenterPoint = rRPPTransform;
                break;
            default:
                Debug.Log("Invalid propeller value provided!");
                propTrans = frontLeftPropTransform;
                propCenterPoint = fLPPTransform;
                break;
        }
        return (propTrans, propCenterPoint);
    }

    private void CheckDistanceToReward()
    {
        float distance = Vector3.Distance(rigidbody.position, reward.position);
        float score = previousDistance - distance;
   
        if (score > 0.06f)
            InvokeAddScore(score * (60 - distance));
        else if (score < -0.06f)
            InvokeAddScore(score * distance);

        previousDistance = distance;
    }

    private void CheckOrientation()
    {
        float maxReward = agent.GetCumulativeReward();
        float thresholdReward = 1f;
        if (maxReward > 2000)
            thresholdReward = maxReward / 2000;

        if (Math.Abs(rigidbody.rotation.eulerAngles.x) > 110 || Math.Abs(rigidbody.rotation.eulerAngles.z) > 110)
            InvokeAddScore(-thresholdReward);

        if (Math.Abs(rigidbody.rotation.eulerAngles.x) < 30 || Math.Abs(rigidbody.rotation.eulerAngles.z) < 30)
            if (maxReward > 3000)
                InvokeAddScore(thresholdReward / 2);
    }
    
    private void CheckGrounded()
    {
        if (rigidbody.position.y < 1.9f)
            InvokeAddScore(-0.5f);
        else
        {
            float rewardValue = reward.position.y - Math.Abs(reward.position.y - rigidbody.position.y - 1.2f);
            rewardValue = rewardValue / 2f;
            InvokeAddScore(rewardValue);
        }
    }

    private void CheckMapBounderies()
    {
        float posX = rigidbody.transform.localPosition.x;
        float posY = rigidbody.transform.localPosition.y;
        float posZ = rigidbody.transform.localPosition.z;

        if (Math.Abs(posX) > 60 || posY < -2 || Math.Abs(posZ) > 60)
            OnOutOfMap?.Invoke(this, EventArgs.Empty);
    }
    
    private void InvokeAddScore(float score)
    {
        AddScoreEventArgs addScoreArgs = new AddScoreEventArgs();
        addScoreArgs.Score = score;
        OnAddScore?.Invoke(this, addScoreArgs);
    }
}
