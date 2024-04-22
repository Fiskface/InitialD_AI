using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Dreamteck.Splines;
using UnityEngine;
using UnityEngine.Serialization;

public class CarControllerSDC : MonoBehaviour
{
    private Vector3 startPosition, startRotation;
    private NNet network;
    private GeneticManager geneticManager;
    private CarControllerRealistic carController;

    [Range(-1f,1f)]
    public float gas, steer;

    public float timeSinceStart = 0f;

    [Header("Sensors")]
    private float speedSensor;
    private float movementAngleSensor;
    public int raycastAmount = 15;
    public int angle = 180;
    public Transform raycastStartPos;
    public LayerMask layerMask;
    public bool loadFileOnStart;
    private SplineProjector splineProjector;

    [Header("Fitness")]
    public float overallFitness;
    public float splinePercentMultiplier = 1.4f;
    public float avgSpeedMultiplier = 0.2f;
    public float reachedGoalFitnessGain = 2000;

    [Header("Network Options")]
    public int LAYERS = 1;
    public int NEURONS = 10;

    private Vector3 lastPosition;
    private float totalDistanceTravelled;
    private float avgSpeed;

    [NonSerialized] public bool reachedGoal = false;

    private Rigidbody rb;

    private float[] inputs;

    private void Awake() {
        startPosition = transform.position;
        startRotation = transform.eulerAngles;
        geneticManager = GetComponent<GeneticManager>();
        rb = GetComponent<Rigidbody>();
        splineProjector = GetComponent<SplineProjector>();

        inputs = new float[raycastAmount + 1];
        NNet.inputs = inputs.Length;
        carController = GetComponent<CarControllerRealistic>();

        if (loadFileOnStart)
        {
            var saved = GeneticManager.DeserializeFromFile<NNet>();
            if (saved != null) network = saved;
        }
    }

    public void ResetWithNetwork (NNet net)
    {
        network = net;
        Reset();
    }
    
    public void Reset() 
    {
        timeSinceStart = 0f;
        totalDistanceTravelled = 0f;
        avgSpeed = 0f;
        lastPosition = startPosition;
        overallFitness = 0f;
        transform.position = startPosition;
        transform.eulerAngles = startRotation;
        reachedGoal = false;

        carController.Reset();
    }

    private void FixedUpdate()
    {
        InputSensors();
        
        (gas, steer) = network.RunNetwork(inputs);

        carController.SetInputs(gas, steer);

        timeSinceStart += Time.deltaTime;

        CalculateFitness();

        lastPosition = transform.position;
    }

    private void Death()
    {
        if (loadFileOnStart) return;
        geneticManager.Death(overallFitness, network);
    }

    private void CalculateFitness() 
    {
        totalDistanceTravelled += Vector3.Distance(transform.position,lastPosition);
        avgSpeed = totalDistanceTravelled/timeSinceStart;
        
        overallFitness = avgSpeed * avgSpeedMultiplier;
        overallFitness += (float)splineProjector.GetPercent() * splinePercentMultiplier;
        
        if (reachedGoal)
        {
            overallFitness += reachedGoalFitnessGain;
            Death();
        }
        
        if (timeSinceStart >= 3 && timeSinceStart > overallFitness / 20) Death();
    }

    private void InputSensors()
    {
        float anglePerAmount = angle / (raycastAmount - 1);
        for (int i = 0; i < raycastAmount; i++)
        {
            Vector3 direction = Quaternion.AngleAxis(-(float)angle * 0.5f + anglePerAmount * (float)i, Vector3.up) * Vector3.forward;
            direction = transform.TransformDirection(direction);
            
            Ray r = new Ray(raycastStartPos.position, direction);
            RaycastHit hit;

            if (Physics.Raycast(r, out hit, float.MaxValue, layerMask))
            {
                inputs[i] = hit.distance / 30;
                Debug.DrawLine(r.origin, hit.point, Color.red);
            }
            else
            {
                inputs[i] = 1;
            }
        }

        speedSensor = rb.velocity.magnitude / 10;
        inputs[^1] = speedSensor;
    }
    
    private void OnCollisionEnter(Collision collision) 
    {
        Death();
    }

}
