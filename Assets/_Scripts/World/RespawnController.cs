using System.Collections;
using UnityEngine;

namespace WarOfCrowns.World
{
    public class RespawnController : MonoBehaviour
    {
        // ��������� �����: ������ �� ��������� string, � �� GameObject
        public void StartRespawning(string prefabName, float time)
        {
            StartCoroutine(RespawnRoutine(prefabName, time));
        }

        // � ��������� �����
        private IEnumerator RespawnRoutine(string prefabName, float time)
        {
            yield return new WaitForSeconds(time);

            // ��������� ������ �� ����� Resources �� ��� �����
            GameObject prefabToRespawn = Resources.Load<GameObject>(prefabName);

            if (prefabToRespawn != null)
            {
                // ������� ����� ������������ �������
                Instantiate(prefabToRespawn, transform.position, transform.rotation);
            }
            else
            {
                Debug.LogError($"�� ������� ����� ������ � ������ '{prefabName}' � ����� Resources.");
            }

            // ���������� ���� ������ ����
            Destroy(gameObject);
        }
    }
}