using UnityEngine;
using System.Collections;
using WarOfCrowns.Core;

public class GameLoader : MonoBehaviour
{
    IEnumerator Start()
    {
        // Ждем, пока все менеджеры (Kingdom, GameManager) не проснутся
        while (Kingdom.PlayerKingdom == null || GameManager.Instance == null)
        {
            yield return null;
        }

        // Теперь, когда все на месте, ЗАПУСКАЕМ ИГРУ
        GameManager.Instance.InitializeGame();
    }
}