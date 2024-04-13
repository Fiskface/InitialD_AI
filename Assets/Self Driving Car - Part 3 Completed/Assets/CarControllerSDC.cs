using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarControllerSDC : MonoBehaviour
{
    private Vector3 startPosition, startRotation;
    private NNet network;
    private GeneticManager geneticManager;

    [Range(-1f,1f)]
    public float a,t;

    public float timeSinceStart = 0f;

    [Header("Sensors")] 
    public int raycastAmount;
    public int angle = 180;

    [Header("Fitness")]
    public float overallFitness;
    public float distanceMultipler = 1.4f;
    public float avgSpeedMultiplier = 0.2f;
    public float sensorMultiplier = 0.1f;

    [Header("Network Options")]
    public int LAYERS = 1;
    public int NEURONS = 10;

    private Vector3 lastPosition;
    private float totalDistanceTravelled;
    private float avgSpeed;

    private float aSensor,bSensor,cSensor;
    private float[] inputs;

    private void Awake() {
        startPosition = transform.position;
        startRotation = transform.eulerAngles;
        geneticManager = GetComponent<GeneticManager>();

        inputs = new float[raycastAmount];
        NNet.inputs = inputs.Length;
    }

    public void ResetWithNetwork (NNet net)
    {
        network = net;
        Reset();
    }
    
    public void Reset() {

        timeSinceStart = 0f;
        totalDistanceTravelled = 0f;
        avgSpeed = 0f;
        lastPosition = startPosition;
        overallFitness = 0f;
        transform.position = startPosition;
        transform.eulerAngles = startRotation;
    }

    private void OnCollisionEnter(Collision collision) {
        Death();
    }

    private void FixedUpdate()
    {

        InputSensors();
        lastPosition = transform.position;
        
        (a, t) = network.RunNetwork(inputs);
        
        MoveCar(a, t);

        timeSinceStart += Time.deltaTime;

        CalculateFitness();
    }

    private void Death()
    {
        geneticManager.Death(overallFitness, network);
    }

    private void CalculateFitness() {

        totalDistanceTravelled += Vector3.Distance(transform.position,lastPosition);
        avgSpeed = totalDistanceTravelled/timeSinceStart;

       overallFitness = (totalDistanceTravelled*distanceMultipler)+(avgSpeed*avgSpeedMultiplier)+(((aSensor+bSensor+cSensor)/3)*sensorMultiplier);

        if (timeSinceStart > 20 && overallFitness < 40) {
            Death();
        }

        if (overallFitness >= 1000) {
            Death();
        }

    }

    private void InputSensors()
    {
        var anglePerAmount = angle / (raycastAmount - 1);
        for (int i = 0; i < raycastAmount; i++)
        {
            Vector3 direction = Quaternion.AngleAxis(-angle * 0.5f + anglePerAmount * i, Vector3.up) * Vector3.forward;
            direction = transform.TransformDirection(direction);
            
            Ray r = new Ray(transform.position,direction);
            RaycastHit hit;

            if (Physics.Raycast(r, out hit))
            {
                inputs[i] = hit.distance / 20;
                Debug.DrawLine(r.origin, hit.point, Color.red);
            }
            else
            {
                inputs[i] = 1;
            }
        }
    }

    private Vector3 inp;
    public void MoveCar(float v, float h) {
        inp = Vector3.Lerp(Vector3.zero,new Vector3(0,0,v*11.4f),0.02f);
        inp = transform.TransformDirection(inp);
        transform.position += inp;

        transform.eulerAngles += new Vector3(0, (h*90)*0.02f,0);
    }

}
