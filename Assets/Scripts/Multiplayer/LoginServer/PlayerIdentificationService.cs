using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerIdentificationService
{
    PlayerIdentificationService instance;

    List<KeyValuePair<string, int>> PlayerScores;

    List<KeyValuePair<string, string>> UsersAndPasswords;

    public PlayerIdentificationService ()
    {
        instance = this;
    }

    public PlayerIdentificationService (string ip, int port)
    {
        instance = this;
    }
}
