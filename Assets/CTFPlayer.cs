using System;

using System;
using System.Collections;
using System.Collections.Generic;
using Cinemachine;
using CTF;
using NetBuff;
using NetBuff.Components;
using NetBuff.Interface;
using NetBuff.Misc;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;

public class CTFPlayer : NetworkBehaviour
{
    public static readonly List<CTFPlayer> Players = new();

    [Header("PLAYER")] 
    public Renderer body;
    public NetworkAnimator animator;
    public TextMeshProUGUI nickText;
    public GameObject serverListPrefab;
    public GameObject shotPrefab;
    public GameObject pirateHat;
    private Rigidbody _rb;
    private Camera _camera;
    public GameObject[] lifePoints;
    
    [Header("FLAG")]
    public GameObject flag;
    private Renderer flagRenderer;

    [Header("SETTINGS")]
    public float walkSpeed = 5;
    public float jumpForce = 3;
    public float mouseSensitivity = 2;

    [Header("STATE")] 
    public BoolNetworkValue gameStarted = new(false);
    public StringNetworkValue nick = new("1000ton");
    public BoolNetworkValue isBlue = new(false);
    public BoolNetworkValue carryingFlag = new(false, NetworkValue.ModifierType.Server);
    public ColorNetworkValue color = new(Color.white);
    public IntNetworkValue life = new(3, NetworkValue.ModifierType.Everybody);
    
    private float cameraRot = 0;
    private float _colorHUE = 0;
    public float shotTimeout = 0.5f;
    private void OnEnable()
    {
        Players.Add(this);
        
        TryGetComponent(out _rb);
        flag.transform.GetChild(0).TryGetComponent(out flagRenderer);
        _camera = Camera.main;
        lifePoints = CTFManager.instance.lifePoints;
        life.Value = lifePoints.Length;
        
        //Values
        WithValues(isBlue, carryingFlag, nick, gameStarted, color, life);
        nick.OnValueChanged += OnChangeNick;
        isBlue.OnValueChanged += OnChangeTeam;
        carryingFlag.OnValueChanged += OnChangeCarryingFlag;
        gameStarted.OnValueChanged += GameStart;
        color.OnValueChanged += OnChangeColor;
        life.OnValueChanged += OnChangeLife;
    }

    private void OnChangeLife(int oldvalue, int newvalue)
    {
        for (var i = 0; i < lifePoints.Length; i++)
        {
            lifePoints[i].SetActive(i < newvalue);
        }
        if (newvalue <= 0)
        {
            life.Value = lifePoints.Length;
            transform.position = CTFManager.instance.GetSpawn(isBlue.Value);
        }
    }

    private void OnChangeColor(Color oldvalue, Color newvalue)
    {
        body.material.color = newvalue;
    }

    private void GameStart(bool oldvalue, bool newvalue)
    {
        if (newvalue)
        {
            if (HasAuthority)
            {
                transform.position = CTFManager.instance.GetSpawn(isBlue.Value);
                
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            color.Value = isBlue.Value ? Color.blue : new Color(1, 0.5f, 0);
        }
    }

    private void OnChangeNick(string oldvalue, string newvalue)
    {
        nickText.text = newvalue;
        pirateHat.SetActive(newvalue.Contains("Jhon Let It Fall", StringComparison.InvariantCultureIgnoreCase));
        
        if(newvalue.Contains("_jeb", StringComparison.InvariantCultureIgnoreCase))
        {
            InvokeRepeating(nameof(RainbowName), 1, 1);
            nickText.color = isBlue.Value ? Color.blue : new Color(1, 0.5f, 0);
        }
        else
        {
            CancelInvoke(nameof(RainbowName));
            _colorHUE = 0;
            nickText.color = Color.white;
        }
    }

    private void RainbowName()
    {
        _colorHUE += 0.1f;
        if (_colorHUE > 1) _colorHUE = 0;
        color.Value = Color.HSVToRGB(_colorHUE, 1, 1);
    }

    private void OnDisable()
    {
        Players.Remove(this);
    }

    private void Update()
    {
        nickText.transform.parent.LookAt(Camera.main!.transform);
        if (!HasAuthority || !gameStarted.Value)
            return;

        var deltaTime = Time.deltaTime;
        var isGrounded = IsGrounded();
        var vel = _rb.velocity;
        var wSpeed = walkSpeed;
        shotTimeout -= deltaTime;
        
        if(Input.GetKey(KeyCode.Space) && isGrounded)
        {
            vel.y = jumpForce;
            _rb.angularVelocity /= 2;
        }
        
        if(Input.GetKey(KeyCode.LeftShift))
        {
            wSpeed *= 1.5f;
        }

        //Camera view
        transform.localEulerAngles += new Vector3(0, Input.GetAxis("Mouse X") * mouseSensitivity, 0);
        cameraRot -= Input.GetAxis("Mouse Y") * mouseSensitivity;
            
        cameraRot = Mathf.Clamp(cameraRot, -90, 90);
        _camera.transform.localEulerAngles = new Vector3(cameraRot, 0, 0);
            
        //Moving direction
        var move = new Vector3(Input.GetAxis("Horizontal"), 0, Input.GetAxis("Vertical"));
        move = Quaternion.Euler(0, _camera.transform.eulerAngles.y, 0) * move;
        vel.x = move.x * wSpeed;
        vel.z = move.z * wSpeed;
            
        //Apply velocity and equilibrium
        _rb.velocity = vel;
        
        //Shot
        if(Input.GetMouseButtonDown(0) && shotTimeout <= 0)
        {
            shotTimeout = 1f;
            var cameraTransform = _camera.transform;
            Spawn(shotPrefab, cameraTransform.position + cameraTransform.forward * 1, cameraTransform.rotation, Vector3.one * 0.2f, true, OwnerId);
        }

        animator.SetBool("walking", move.magnitude > 0.1f);
        animator.SetBool("carrying", carryingFlag.Value);
    }
    
    public override void OnSpawned(bool isRetroactive)
    {
        if (!HasAuthority) return;
        
        isBlue.Value = Players.FindAll(p => p.isBlue.Value).Count < Players.FindAll(p => !p.isBlue.Value).Count;

        var cam = _camera.transform;
        cam.SetParent(transform);
        cam.localPosition = Vector3.up;
        body.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        pirateHat.GetComponentInChildren<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.ShadowsOnly;
        nickText.transform.parent.gameObject.SetActive(false);
        
        Spawn(serverListPrefab, transform.position, Quaternion.identity, OwnerId);
        nick.Value = NetworkManager.Instance.IsServerRunning
            ? PlayerPrefs.GetString("Nick", $"Host ({OwnerId})") + " (Host)"
            : PlayerPrefs.GetString("Nick", $"Player ({OwnerId})");
    }
    
    private void OnTriggerEnter(Collider other)
    {
        if (!NetworkManager.Instance.IsServerRunning)
            return;
            
        if (other.CompareTag("Flag"))
            CTFManager.instance.CaptureFlag(this);
            
        if (other.CompareTag("Spawn") && carryingFlag.Value)
            CTFManager.instance.FinishFlagCapture(this);
    }

    private bool IsGrounded()
    {
        if (Physics.Raycast(transform.position+new Vector3(0,0.1f,0), Vector3.down, out var hit, 2.2f))
        {
            Debug.DrawRay(transform.position+new Vector3(0,0.1f,0), Vector3.down*hit.distance, hit.distance < 0.2f ? Color.green : Color.yellow);
            return hit.distance < .2f;
        }
        Debug.DrawRay(transform.position+new Vector3(0,0.1f,0), Vector3.down * 2.2f, Color.red);
        return false;
    }

    private void OnChangeCarryingFlag(bool oldvalue, bool newvalue)
    {
        flag.SetActive(newvalue);
        if (!newvalue) return;
        //blue or orange
        flagRenderer.material.color = !isBlue.Value ? Color.blue : new Color(1, 0.5f, 0);
        if(HasAuthority && _colorHUE <= 0)
            color.Value = isBlue.Value ? Color.blue : new Color(1, 0.5f, 0);
    }

    private void OnChangeTeam(bool oldvalue, bool newvalue)
    {
        transform.position = CTFManager.instance.GetSpawn(newvalue);
        if(nick.Value.Contains("_jeb", StringComparison.InvariantCultureIgnoreCase))
        {
            nickText.color = isBlue.Value ? Color.blue : new Color(1, 0.5f, 0);
        }else color.Value = newvalue ? Color.blue : new Color(1, 0.5f, 0);
    }
    
    public override void OnClientReceivePacket(IOwnedPacket packet)
    {
        if (!HasAuthority)
            return;
            
        if (packet is PacketPlayerRespawn packetPlayerRespawn)
        {
            transform.position = CTFManager.instance.GetSpawn(isBlue.Value);
            _rb.velocity = Vector3.zero;
            transform.forward = new Vector3(-transform.position.x, 0, 0);
        }
    }
}
