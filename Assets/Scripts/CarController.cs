using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CarController : MonoBehaviour
{
    
    public Wheel[] wheels;
    public float motorPower;
    public float brakePower;
    public AnimationCurve steeringCurve;
    public GameObject smokePrefab;
    public float slipAllowance = 0.05f;


    private float _gasInput;
    private float _steerInput;
    private float _brakeInput;
    private float _speed;
    private float _slipAngle;

    private Rigidbody rb;

    // Start is called before the first frame update
    void Awake()
    {
        rb = GetComponent<Rigidbody>();

        foreach (var wheel in wheels)
        {
            wheel.smokeParticles = Instantiate(smokePrefab, wheel.coll.transform.position - wheel.coll.radius / 2 * Vector3.up, 
                Quaternion.identity, wheel.coll.transform).GetComponent<ParticleSystem>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        CheckInput();
        UpdateWheel();
    }

    private void UpdateWheel()
    {
        _speed = rb.velocity.magnitude;
        CheckInput();

        float steerHelp = Vector3.SignedAngle(transform.forward, rb.velocity + transform.forward, Vector3.up);
        if(_gasInput < 0) steerHelp = 0;

        foreach (var wheel in wheels)
        {
            wheel.UpdateWheelMesh();
            wheel.ApplySteering(_steerInput, _speed, steeringCurve, steerHelp);
            wheel.ApplyMotor(_gasInput, motorPower);
            wheel.ApplyBrake(_brakeInput, brakePower);
            wheel.CheckParticles(slipAllowance);
        }
    }

    private void CheckInput()
    {
        _gasInput = Input.GetAxis("Vertical");
        _steerInput = Input.GetAxis("Horizontal");
        _slipAngle = Vector3.Angle(transform.forward, rb.velocity - transform.forward);

        if(_slipAngle < 120)
        {
            if(_gasInput < 0)
            {
                _brakeInput = Mathf.Abs(_gasInput);
                _gasInput = 0;
            }
        }
        else _brakeInput = 0;
    }

}


[System.Serializable]
public class Wheel
{
    public WheelCollider coll;
    public MeshRenderer mesh;
    public bool steerable;
    public bool motorized;
    public bool frontTyre;
    [NonSerialized] public ParticleSystem smokeParticles;

    public void UpdateWheelMesh()
    {
        Vector3 pos;
        Quaternion quat;
        coll.GetWorldPose(out pos, out quat);
        mesh.transform.position = pos;
        mesh.transform.rotation = quat;
    }

    public void ApplyMotor(float gasInput, float motorPower){
        if(!motorized) return;

        coll.motorTorque = motorPower * gasInput;
    }

    public void ApplySteering(float steeringInput, float speed, AnimationCurve steeringCurve, float steerHelp = 0)
    {
        if(!steerable) return;

        float steeringAngle = steeringInput * steeringCurve.Evaluate(speed);

        //Help player?
        steeringAngle += steerHelp;
        steeringAngle = Mathf.Clamp(steeringAngle, -70f, 70f);
        //Help player end here

        coll.steerAngle = steeringAngle;
    }

    public void ApplyBrake(float brakeInput, float brakePower)
    {
        if(frontTyre) coll.brakeTorque = brakeInput * brakePower * 0.7f;
        else coll.brakeTorque = brakeInput * brakePower * 0.3f;
    }

    public void CheckParticles(float slipAllowance)
    {
        coll.GetGroundHit(out WheelHit hit);

        if(Mathf.Abs(hit.sidewaysSlip) + Mathf.Abs(hit.forwardSlip) > slipAllowance)
        {
            smokeParticles.Play();
        }
        else smokeParticles.Stop();
    }
}
