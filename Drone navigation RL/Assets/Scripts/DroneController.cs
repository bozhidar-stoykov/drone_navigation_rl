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

    public event EventHandler OnDistanceCloser;
    public event EventHandler OnDistanceFurther;
    public event EventHandler<AddScoreEventArgs> OnAddScore;

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
    }

    void FixedUpdate()
    {
        CheckGrounded();
        CheckOrientation();
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
            InvokeAddScore(score * (66 - distance));
        else if (score < -0.06f)
            InvokeAddScore(score * distance);

        previousDistance = distance;
    }

    private void CheckOrientation()
    {
        if (Math.Abs(rigidbody.rotation.eulerAngles.x) > 80 || Math.Abs(rigidbody.rotation.eulerAngles.z) > 80)
            InvokeAddScore(-1f);
    }
    
    private void CheckGrounded()
    {
        float distanceY = Math.Abs(reward.position.y - rigidbody.position.y);
        if (rigidbody.position.y < 1.9f)
            InvokeAddScore(-distanceY / 3);
        else
        {
            float rewardValue = reward.position.y - distanceY;
            rewardValue = rewardValue / 3;
            InvokeAddScore(rewardValue);
        }
    }

    private void InvokeAddScore(float score)
    {
        AddScoreEventArgs addScoreArgs = new AddScoreEventArgs();
        addScoreArgs.Score = score;
        OnAddScore?.Invoke(this, addScoreArgs);
    }
}
