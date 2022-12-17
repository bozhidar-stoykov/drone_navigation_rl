using System.Collections;
using System.Collections.Generic;
using Unity.MLAgents.Sensors;
using UnityEngine;

public class ObstacleDetectionSensorSpawner : MonoBehaviour
{
    private RayPerceptionSensorComponent3D rpSensor;
    
    void Awake()
    {
        rpSensor = GetComponent<RayPerceptionSensorComponent3D>();

        for (int i = 1; i < rpSensor.RaysPerDirection * 4; i++)
        {
            GameObject sensorParent = new GameObject();
            sensorParent.transform.parent = this.gameObject.transform;
            RayPerceptionSensorComponent3D rps = sensorParent.AddComponent<RayPerceptionSensorComponent3D>() as RayPerceptionSensorComponent3D;
            rps.DetectableTags = rpSensor.DetectableTags;
            rps.RayLength = rpSensor.RayLength;
            rps.SensorName = rpSensor.SensorName + i.ToString();
            rps.SphereCastRadius = rpSensor.SphereCastRadius;
            rps.RaysPerDirection = rpSensor.RaysPerDirection - 1;
            rps.MaxRayDegrees = rpSensor.MaxRayDegrees - rpSensor.MaxRayDegrees / (rpSensor.RaysPerDirection);
            rps.transform.localPosition = rpSensor.transform.localPosition;
            sensorParent.transform.Rotate(Vector3.left, rpSensor.MaxRayDegrees / (rpSensor.RaysPerDirection) * i);
        }
        
    }
}
