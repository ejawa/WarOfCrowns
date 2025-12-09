using UnityEngine;
using TMPro;
using System.Collections;

namespace WarOfCrowns.UI
{
    public class NotificationUI : MonoBehaviour
    {
        public static NotificationUI Instance { get; private set; }

        [SerializeField] private TextMeshProUGUI notificationText;
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private float displayTime = 3f;

        private Coroutine _routine;

        private void Awake()
        {
            Instance = this;
            if (canvasGroup != null) canvasGroup.alpha = 0;
        }

        public void ShowNotification(string message, Color color)
        {
            if (notificationText == null || canvasGroup == null) return;

            notificationText.text = message;
            notificationText.color = color;

            if (_routine != null) StopCoroutine(_routine);
            _routine = StartCoroutine(AnimateNotification());
        }

        private IEnumerator AnimateNotification()
        {
            canvasGroup.alpha = 1;
            yield return new WaitForSeconds(displayTime);

            float t = 1f;
            while (t > 0)
            {
                t -= Time.deltaTime;
                canvasGroup.alpha = t;
                yield return null;
            }
            canvasGroup.alpha = 0;
        }
    }
}