using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using NetBuff;
using NetBuff.Components;
using NetBuff.Interface;
using NetBuff.Misc;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class CTFManager : NetworkBehaviour
{
    public static CTFManager instance;

    public GameObject flagOrange;
    public GameObject flagBlue;

    public Transform blueSpawn;
    public Transform orangeSpawn;
    [Range(0,10)] public float respawnRadius = 5;
    
    public BoolNetworkValue gameStarted = new(false);
    
    public IntNetworkValue flagOrangeCarrier = new(-1);
    public IntNetworkValue flagBlueCarrier = new(-1);
        
    public IntNetworkValue orangeScore = new(0);
    public IntNetworkValue blueScore = new(0);
    public IntNetworkValue time = new(0);

    [Header("Canvas")]
    public GameObject ServerList;
    public GameObject HUD;
    public TextMeshProUGUI timeText, orangeScoreText, blueScoreText;
    public TextMeshProUGUI orangeMessageText, blueMessageText;
    public Transform serverListSideBlue, serverListSideOrange;
    public Button startButton;
    
    public GameObject[] lifePoints;

    private void Awake()
    {
        if (instance == null) instance = this;
        else if (instance != this) Destroy(gameObject);
    }

    private void OnEnable()
    {
        WithValues(time, gameStarted, flagOrangeCarrier, flagBlueCarrier, orangeScore, blueScore);
        orangeScore.OnValueChanged += OnChangeOrangeScore;
        blueScore.OnValueChanged += OnChangeBlueScore;
        time.OnValueChanged += OnChangeTime;
        orangeMessageText.text = "";
        blueMessageText.text = "";
    }

    private void OnChangeTime(int oldvalue, int newvalue)
    {
        var minutes = newvalue / 60;
        var seconds = newvalue % 60;
        timeText.text = $"{minutes}:{seconds:00}";
    }

    private void OnChangeBlueScore(int oldvalue, int newvalue)
    {
        blueScoreText.text = newvalue.ToString();
    }

    private void OnChangeOrangeScore(int oldvalue, int newvalue)
    {
        orangeScoreText.text = newvalue.ToString();
    }

    private void Start()
    {
        gameStarted.OnValueChanged += OnChangeGameStarted;
    }

    private void Update()
    {
        if(!NetworkManager.Instance.IsServerRunning) return;
        
        foreach (var player in CTFPlayer.Players)
        {
            if (player.transform.position.y < 2)
            {
                SendPacket(new PacketPlayerRespawn {Id = player.Id});
                    
                if (flagOrangeCarrier.Value == player.OwnerId)
                {
                    flagOrangeCarrier.Value = -1;
                    flagOrange.SetActive(true);
                    player.carryingFlag.Value = false;
                }
                else if (flagBlueCarrier.Value == player.OwnerId)
                {
                    flagBlueCarrier.Value = -1;
                    flagBlue.SetActive(true);
                    player.carryingFlag.Value = false;
                }
            }
        }
    }
    
    public override void OnSpawned(bool isRetroactive)
    {
        ServerList.SetActive(true);
        if(NetworkManager.Instance.IsServerRunning)
        {
            startButton.onClick.AddListener(() => gameStarted.Value = true);
            startButton.interactable = true;
        }else
        {
            startButton.interactable = false;
        }
    }

    private void OnChangeGameStarted(bool oldvalue, bool newvalue)
    {
        if (newvalue)
        {
            CTFPlayer.Players.ForEach(x =>
            {
                x.gameStarted.Value = true;
            });
            ServerList.SetActive(false);
            ServerList.transform.parent.gameObject.SetActive(false);
            if (HasAuthority)
            {
                InvokeRepeating(nameof(UpdateTime), 1, 1);
            }
        }
    }

    public void UpdateTime()
    {
        if(NetworkManager.Instance.IsServerRunning) 
            time.Value++;
    }

    public Vector3 GetSpawn(bool isBlue)
    {
        var spawn = isBlue ? blueSpawn.position : orangeSpawn.position; 
        float angle = UnityEngine.Random.Range(0, 2 * Mathf.PI);
        float x = respawnRadius * Mathf.Cos(angle);
        float z = respawnRadius * Mathf.Sin(angle);
        return new Vector3(spawn.x + x, spawn.y, spawn.z + z);
    }

    public void CaptureFlag(CTFPlayer player)
    {
        var pos = player.transform.position;
        var isBlueValue = player.isBlue.Value;
        if(Vector3.Distance(pos, isBlueValue ? flagOrange.transform.position : flagBlue.transform.position) < 5f)
        {
            if (isBlueValue && flagOrange.activeSelf)
            {
                flagOrangeCarrier.Value = player.OwnerId;
                flagOrange.SetActive(false);
            }
            else if ( flagBlue.activeSelf)
            {
                flagBlueCarrier.Value = player.OwnerId;
                flagBlue.SetActive(false);
            }

            var flagIsBlue = !GetServerListSide(player.OwnerId);
            var text = flagIsBlue ? blueMessageText : orangeMessageText;
            if(player.OwnerId != OwnerId)
            {
                text.text = (!flagIsBlue ? "<color=blue>" : "<color=#FF8F00>") + $"{player.nick.Value.Replace(" (Host)", "")}</color> esta com a bandeira " +
                            (flagIsBlue ? "<color=blue>Azul</color>" : "<color=#FF8F00>Laranja</color>");
            }
            else
            {
                text.text = $"Você esta com a bandeira " +
                                         (flagIsBlue ? "<color=blue>Azul</color>" : "<color=#FF8F00>Laranja</color>");
            }
            player.carryingFlag.Value = true;
        }
    }
    
    public void FinishFlagCapture(CTFPlayer player)
    {
        var pos = player.transform.position;
        var isBlueValue = player.isBlue.Value;
        if(Vector3.Distance(pos, !isBlueValue ? flagOrange.transform.position : flagBlue.transform.position) < 5f)
        {
            if (isBlueValue)
            {
                flagOrangeCarrier.Value = -1;
                flagOrange.SetActive(true);
                blueScore.Value++;
            }
            else
            {
                flagBlueCarrier.Value = -1;
                flagBlue.SetActive(true);
                orangeScore.Value++;
            }
            player.carryingFlag.Value = false;

            var flagIsBlue = !GetServerListSide(player.OwnerId);
            var text = flagIsBlue ? blueMessageText : orangeMessageText;
            text.text = "O time " + (!flagIsBlue ? "<color=blue>Azul</color>" : "<color=#FF8F00>Laranja</color>") + " ganhou 1 ponto";
            text.gameObject.SetActive(true);
            Invoke(!flagIsBlue ? nameof(FinishMessageBlue) : nameof(FinishMessageOrange), 2);
        }
    }

    private void FinishMessageOrange()
    {
        orangeMessageText.text = "";
    }

    private void FinishMessageBlue()
    {
        blueMessageText.text = "";
    }

    public Transform GetServerListSide(bool isBlue)
    {
        return isBlue ? serverListSideBlue : serverListSideOrange;
    }
    
    public bool GetServerListSide(int ownerId)
    {
        return CTFPlayer.Players.Find(x => x.OwnerId == ownerId).isBlue.Value;
    }
}

public class PacketPlayerRespawn : IOwnedPacket
{
    public NetworkId Id { get; set; }
        
    public void Serialize(BinaryWriter writer)
    {
        Id.Serialize(writer);
    }

    public void Deserialize(BinaryReader reader)
    {
        Id = NetworkId.Read(reader);
    }
}