using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CarControllerRealistic : MonoBehaviour
{
    
    public Wheel[] wheels;
    public float motorPower;
    public float brakePower;
    public AnimationCurve steeringCurve;
    public GameObject smokePrefab;
    public float slipAllowance = 0.05f;
    public Vector3 centerOfMassOffset;


    private float _gasInput;
    private float _steerInput;
    private float _brakeInput;
    private bool _handBrakeInput;
    private float _speed;
    private float _slipAngle;

    private Rigidbody rb;

    // Start is called before the first frame update
    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        rb.centerOfMass += centerOfMassOffset;

        foreach (var wheel in wheels)
        {
            wheel.smokeParticles = Instantiate(smokePrefab, wheel.coll.transform.position - wheel.coll.radius / 2 * Vector3.up, 
                Quaternion.identity, wheel.coll.transform).GetComponent<ParticleSystem>();
            wheel.Awake();
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
        steerHelp /= 2;
        if(_gasInput < 0) steerHelp = 0;

        foreach (var wheel in wheels)
        {
            wheel.UpdateWheelMesh();
            wheel.ApplySteering(_steerInput, _speed, steeringCurve, steerHelp);
            wheel.ApplyMotor(_gasInput, motorPower);
            wheel.ApplyBrake(_brakeInput, brakePower);
            wheel.ApplyHandBrake(_handBrakeInput, brakePower);
            wheel.CheckParticles(slipAllowance);
        }
    }

    private void CheckInput()
    {
        _gasInput = Input.GetAxis("Vertical");
        _steerInput = Input.GetAxis("Horizontal");
        _slipAngle = Vector3.Angle(transform.forward, rb.velocity - transform.forward);
        _handBrakeInput = Input.GetButton("Fire1");

        if(_slipAngle < 120)
        {
            if (_gasInput < 0)
            {
                _brakeInput = Mathf.Abs(_gasInput);
                _gasInput = 0;
            }
            else _brakeInput = 0;
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

    private WheelFrictionCurve defaultSidewaysFriction = new WheelFrictionCurve();
    private WheelFrictionCurve handBrakeSidewaysFriction = new WheelFrictionCurve();

    public void Awake()
    {
        defaultSidewaysFriction = coll.sidewaysFriction;

        handBrakeSidewaysFriction.extremumSlip = defaultSidewaysFriction.extremumSlip * 10;
        handBrakeSidewaysFriction.extremumValue = defaultSidewaysFriction.extremumValue / 2;
        handBrakeSidewaysFriction.asymptoteSlip = defaultSidewaysFriction.asymptoteSlip;
        handBrakeSidewaysFriction.asymptoteValue = defaultSidewaysFriction.asymptoteValue;
        handBrakeSidewaysFriction.stiffness = defaultSidewaysFriction.stiffness;
    }

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

    public void ApplyHandBrake(bool handBrakeInput, float brakePower)
    {
        if (frontTyre) return;
        if (!handBrakeInput)
        {
            coll.sidewaysFriction = defaultSidewaysFriction;
            return;
        }

        coll.sidewaysFriction = handBrakeSidewaysFriction;

        coll.brakeTorque = brakePower * 0.5f;
    }

    public void CheckParticles(float slipAllowance)
    {
        coll.GetGroundHit(out WheelHit hit);

        if (Mathf.Abs(hit.sidewaysSlip) * 2 + Mathf.Abs(hit.forwardSlip) / 5 > slipAllowance)
        {
            if (!smokeParticles.isPlaying) smokeParticles.Play();
        }
        else
        {
            if(smokeParticles.isPlaying) smokeParticles.Stop();
        }
    }
}
