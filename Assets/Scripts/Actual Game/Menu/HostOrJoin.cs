using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class HostOrJoin : MonoBehaviour
{
    static string IPG;

    static int PortG;

    static string NowScene;

    public InputField IP;
    public InputField Port;

    public string SelectedMap = "Default";


    public void ChangeSelectedMap (string map)
    {
        SelectedMap = map;
    }

    public void HostServer ()
    {
        ProgramInfo.isServer = true;
        IPG = IP.text;

        var ptext= Port.text;
        if(ptext == "default") ptext = "25542";

        PortG = int.Parse(ptext);

        ServerTCP.OnServerNodeAvailable = InitializeServer;

        NowScene = SceneManager.GetActiveScene().name;

        WSceneManager.SwitchScene(SelectedMap);
    }

    static void InitializeServer ()
    {
        ServerTCP.instance.SetupServer(IPG, PortG);
        ServerTCP.OnServerClosed.Add(BackToThis);
    }

    public void JoinServer ()
    {
        ProgramInfo.isServer = false;
        IPG = IP.text;

        var ptext= Port.text;
        if(ptext == "default") ptext = "25542";

        PortG = int.Parse(ptext);

        ClientTCP.OnClientNodeAvailable = InitializeClient;
        NowScene = SceneManager.GetActiveScene().name;
        WSceneManager.SwitchScene("ClientTCP");
    }

    static void InitializeClient ()
    {
        ClientTCP.instance.ConnectToServer(IPG, PortG);
        ClientTCP.OnDisconnect.Add(BackToThis);
    }

    static void BackToThis ()
    {
        WSceneManager.SwitchScene(NowScene);
    }
}
