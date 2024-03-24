using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NetBuff;
using NetBuff.Misc;
using NetBuff.UDP;
using UnityEngine;
using UnityEngine.UI;

public class ConnectionMenu : MonoBehaviour
{
    public TMPro.TMP_InputField nickInput;
    public TMPro.TMP_InputField ipInput;
    public TMPro.TMP_InputField serverList;
    public Button hostButton;
    public Button joinButton;
    public Button refreshButton;

    public GameObject connectMenu;
    
    private ServerDiscoverer.GameInfo[] _serverList;

    private void Awake()
    {
        hostButton.onClick.AddListener(() =>
        {
            PlayerPrefs.SetString("Nick", nickInput.text);
            PlayerPrefs.Save();
            
            if (NetworkManager.Instance.transport is UDPNetworkTransport udp)
            {
                udp.Name = nickInput.text;
                udp.address = ipInput.text;
                udp.Ip = ipInput.text;
            }
            
            NetworkManager.Instance.StartHost();
        });
        
        joinButton.onClick.AddListener(() =>
        {
            PlayerPrefs.SetString("Nick", nickInput.text);
            PlayerPrefs.Save();
            
            if (NetworkManager.Instance.transport is UDPNetworkTransport udp)
            {
                udp.address = ipInput.text;
            }
            NetworkManager.Instance.StartClient();
        });
        
        if (NetworkManager.Instance.transport is UDPNetworkTransport udp)
        {
            ipInput.text = udp.address;
        }

        nickInput.text = PlayerPrefs.GetString("Nick", "");

        nickInput.onValueChanged.AddListener((s) => ValidateButtons());
        ipInput.onValueChanged.AddListener((s) => ValidateButtons());
        refreshButton.onClick.AddListener(() =>
        {
            _serverList = null;
        });
        
        ValidateButtons();
        InvokeRepeating(nameof(UpdateServerList), 1, 15);
    }
    
    private void ValidateButtons()
    {
        hostButton.interactable = !string.IsNullOrEmpty(nickInput.text) && !string.IsNullOrEmpty(ipInput.text);
        joinButton.interactable = !string.IsNullOrEmpty(nickInput.text) && !string.IsNullOrEmpty(ipInput.text);
    }

    private void FixedUpdate()
    {
        if(NetworkManager.Instance.EndType != NetworkTransport.EndType.None)
        {
            if (!connectMenu.activeSelf) return;
            connectMenu.SetActive(false);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            DrawServerList();
            connectMenu.SetActive(true);
            connectMenu.transform.parent.gameObject.SetActive(true);
        }
    }

    private void UpdateServerList()
    {
        if(!connectMenu.activeSelf) return;
        _serverList = null;
    }
    
    private void DrawServerList()
    {
        if (_serverList == null)
        {
            _serverList = Array.Empty<ServerDiscoverer.GameInfo>();
            var list = new List<ServerDiscoverer.GameInfo>();
                
            if (NetworkManager.Instance.transport is UDPNetworkTransport udp)
            {
                ServerDiscoverer.FindServers(udp.port, (info) =>
                {
                    list.Add(info);
                    _serverList = list.ToArray();
                }, () => {});
            }
        }

        if (NetworkManager.Instance.transport is UDPNetworkTransport transport)
        {
            var list = "";
            foreach (var info in _serverList)
            {
                if (info is ServerDiscoverer.EthernetGameInfo egi)
                {
                    list += $"{egi.Name}: {egi.Address} - {egi.Platform}";
                }
            }
            serverList.text = list;
        }
    }
}
