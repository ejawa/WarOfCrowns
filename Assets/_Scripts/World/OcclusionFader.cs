using UnityEngine;
using System.Collections.Generic;

namespace WarOfCrowns.World
{
    public class OcclusionFader : MonoBehaviour
    {
        [Header("Настройки")]
        [Range(0f, 1f)] public float fadedAlpha = 0.4f;
        [SerializeField] private float fadeSpeed = 5f;

        [Header("Что делать прозрачным?")]
        [SerializeField] private SpriteRenderer[] renderersToFade;

        private int _unitsInsideCount = 0;
        private float _targetAlpha = 1f;

        private void Awake()
        {
            if (renderersToFade == null || renderersToFade.Length == 0)
            {
                renderersToFade = GetComponentsInChildren<SpriteRenderer>();
            }
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            // Лог для проверки: видит ли дерево хоть кого-то?
            // Debug.Log($"[Fader] Что-то вошло в триггер: {other.name}");

            if (other.CompareTag("Unit"))
            {
                Debug.Log($"[Fader] Юнит {other.name} зашел за {name}!");
                _unitsInsideCount++;
                UpdateTargetAlpha();
            }
        }

        private void OnTriggerExit2D(Collider2D other)
        {
            if (other.CompareTag("Unit"))
            {
                _unitsInsideCount--;
                if (_unitsInsideCount < 0) _unitsInsideCount = 0;
                UpdateTargetAlpha();
            }
        }

        private void UpdateTargetAlpha()
        {
            _targetAlpha = (_unitsInsideCount > 0) ? fadedAlpha : 1f;
        }

        private void Update()
        {
            if (renderersToFade == null || renderersToFade.Length == 0) return;

            // Если альфа еще не достигла цели
            if (Mathf.Abs(renderersToFade[0].color.a - _targetAlpha) > 0.01f)
            {
                float newAlpha = Mathf.Lerp(renderersToFade[0].color.a, _targetAlpha, fadeSpeed * Time.deltaTime);
                Debug.Log($"Меняю альфу для {renderersToFade[0].name}: {newAlpha}");
                foreach (var r in renderersToFade)
                {
                    if (r != null)
                    {
                        Color c = r.color;
                        c.a = newAlpha;
                        r.color = c;
                    }
                }
            }
        }
    }
}