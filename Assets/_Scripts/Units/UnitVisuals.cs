using UnityEngine;
using System.Collections;
using WarOfCrowns.Core;
using WarOfCrowns.Units;

namespace WarOfCrowns.Units
{
    public class UnitVisuals : MonoBehaviour
    {
        public static bool ShowStanceIcons = true;

        [Header("Рендереры")]
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private SpriteRenderer clothesRenderer;
        [SerializeField] private SpriteRenderer headRenderer;
        [SerializeField] private SpriteRenderer armorRenderer;
        [SerializeField] private SpriteRenderer weaponToolRenderer;
        [Tooltip("Спрайт 'щеточки', который красится в цвет фракции")]
        [SerializeField] private SpriteRenderer plumeRenderer;

        [Header("Стойки")]
        [SerializeField] private SpriteRenderer stanceIconRenderer;
        [SerializeField] private Sprite aggressiveIcon;
        [SerializeField] private Sprite defensiveIcon;
        [SerializeField] private Sprite holdIcon;
        [SerializeField] private Color stanceColor = new Color(1f, 1f, 1f, 0.9f);
        [Header("X-Ray")]
        [SerializeField] private SpriteRenderer silhouetteRenderer; // <-- Привяжи сюда объект Silhouette
        [Header("Эффекты")]
        [SerializeField] private SpriteRenderer parryEffectRenderer;

        [Header("Анимация")]
        [Tooltip("Время одного кадра в секундах (0.1 = 100мс)")]
        [SerializeField] private float frameDuration = 0.1f;
        [SerializeField] private float leanAngle = 25f;
        [SerializeField] private bool spriteFacesLeft = false;

        private Unit _unit;
        private UnitMotor _motor;
        private SpriteSet _bodySet, _clothesSet, _headSet, _armorSet, _weaponSet, _plumeSet;
        private float _animTimer;
        private int _currentFrameIndex;
        private float _initialScaleX;
        private Transform _visualRoot;

        private Vector3 _lastPosition;
        private Coroutine _parryCoroutine;
        private Coroutine _drowningCoroutine;
        private Vector3 _iconInitialScale;
        private bool _isMovingCached;

        // Геттеры для UI
        public string BodySpriteName => _bodySet?.idle != null ? _bodySet.idle.name : "";
        public string ClothesSpriteName => _clothesSet?.idle != null ? _clothesSet.idle.name : "";
        public string HeadSpriteName => _headSet?.idle != null ? _headSet.idle.name : "";

        public float DrownAnimationLength
        {
            get
            {
                if (_bodySet != null && _bodySet.drown != null)
                    return _bodySet.drown.Length * frameDuration;
                return 1.0f;
            }
        }

        private void Awake()
        {
            _unit = GetComponentInParent<Unit>();
            _motor = GetComponentInParent<UnitMotor>();
            _visualRoot = transform.Find("Visuals");
            if (_visualRoot == null) _visualRoot = transform;

            _initialScaleX = Mathf.Abs(_visualRoot.localScale.x);

            if (parryEffectRenderer) parryEffectRenderer.gameObject.SetActive(false);

            if (stanceIconRenderer)
            {
                stanceIconRenderer.color = stanceColor;
                stanceIconRenderer.sortingOrder = 20;
                _iconInitialScale = stanceIconRenderer.transform.localScale;
                stanceIconRenderer.gameObject.SetActive(false);
            }

            if (plumeRenderer) plumeRenderer.gameObject.SetActive(false);
        }

        private void Start()
        {
            _lastPosition = transform.position;
            if (_unit != null) UpdateStanceVisual(_unit.Stance);
        }

        private void LateUpdate()
        {
            if (_unit == null) return;

            // 1. Туман
            HandleFogOfWarVisibility();
            if (!_visualRoot.gameObject.activeSelf) return;

            // 2. Цвета (Вызываем каждый кадр, чтобы реагировать на объявление войны)
            // Можно оптимизировать через события, но пока и так сойдет
            UpdateColors();

            // 3. Расчет движения
            float deltaX = 0f;
            if (_unit.IsOwner)
            {
                if (_motor != null)
                {
                    _isMovingCached = _motor.IsMoving;
                    if (_isMovingCached) deltaX = _motor.TargetPosition.x - transform.position.x;
                }
            }
            else
            {
                float distMoved = Vector3.Distance(transform.position, _lastPosition);
                _isMovingCached = distMoved > 0.005f;
                deltaX = transform.position.x - _lastPosition.x;
            }
            _lastPosition = transform.position;

            // 4. Поворот (Flip)
            if (_isMovingCached && Mathf.Abs(deltaX) > 0.001f)
            {
                float directionMult = spriteFacesLeft ? -1f : 1f;
                bool faceRight = deltaX > 0;

                Vector3 scale = _visualRoot.localScale;
                scale.x = faceRight ? (_initialScaleX * directionMult) : (-_initialScaleX * directionMult);
                _visualRoot.localScale = scale;

                if (stanceIconRenderer != null)
                {
                    float parentSign = Mathf.Sign(scale.x);
                    Vector3 newIconScale = _iconInitialScale;
                    newIconScale.x = Mathf.Abs(_iconInitialScale.x) * parentSign;
                    stanceIconRenderer.transform.localScale = newIconScale;
                }
            }

            if (stanceIconRenderer != null)
                stanceIconRenderer.transform.rotation = Quaternion.identity;

            // 5. Аниматор (Таймер)
            _animTimer += Time.deltaTime;
            if (_animTimer >= frameDuration)
            {
                _animTimer -= frameDuration;
                _currentFrameIndex++;
            }

            // 6. Отрисовка
            if (_drowningCoroutine == null)
            {
                UpdateAllRenderers(_currentFrameIndex);
            }
        }

        // --- МЕТОД ДЛЯ UNIT.CS ---
        public void ForceUpdateState()
        {
            UpdateAllRenderers(_currentFrameIndex);
        }
        // ----------------------------------

        private void UpdateColors()
        {
            if (bodyRenderer) bodyRenderer.color = Color.white;
            if (headRenderer) headRenderer.color = Color.white;
            if (armorRenderer) armorRenderer.color = Color.white;

            // Фоллбэк, если королевства еще нет
            if (_unit.OwningKingdom == null)
            {
                if (clothesRenderer) clothesRenderer.color = Color.grey;
                if (plumeRenderer) plumeRenderer.color = Color.grey;
                return;
            }

            Color targetColor = _unit.OwningKingdom.kingdomColor.Value;

            if (Kingdom.PlayerKingdom != null)
            {
                int myID = Kingdom.PlayerKingdom.kingdomID.Value;
                int unitOwnerID = _unit.ownerKingdomID.Value;

                if (unitOwnerID != myID && Kingdom.PlayerKingdom.IsAtWarWith(unitOwnerID))
                {
                    targetColor = Color.red;
                }
            }

            if (clothesRenderer)
            {
                float tint = _unit.visualTint.Value;
                Color clothColor = Color.Lerp(Color.black, targetColor, tint);
                clothColor.a = 1f; // <--- ИЗМЕНЕНИЕ
                clothesRenderer.color = clothColor;
            }

            if (plumeRenderer)
            {
                Color plumeColor = targetColor;
                plumeColor.a = 1f; // <--- ИЗМЕНЕНИЕ
                plumeRenderer.color = plumeColor;
            }
        }

        // ... (Остальные методы LoadAppearance, UpdateEquipment, UpdateAllRenderers и эффекты - без изменений) ...
        // Копируй их из предыдущего файла, так как мы меняли только UpdateColors

        // ДЛЯ УДОБСТВА Я ПРИВОЖУ ИХ НИЖЕ, ЧТОБЫ ТЫ МОГ СКОПИРОВАТЬ ФАЙЛ ЦЕЛИКОМ:

        public void LoadAppearance(SpriteSet b, SpriteSet h, SpriteSet c, SpriteSet p)
        {
            _bodySet = b; _headSet = h; _clothesSet = c; _plumeSet = p;
            UpdateAllRenderers(_currentFrameIndex);
        }

        public void UpdateEquipment(ResourceType t, ResourceType w, ResourceType a, AppearanceDatabase db)
        {
            if (!db) return;
            _weaponSet = db.GetEquipmentSprites(w) ?? db.GetEquipmentSprites(t);
            _armorSet = db.GetEquipmentSprites(a);
            UpdateAllRenderers(_currentFrameIndex);
        }

        public void UpdateProfession(ProfessionType p, AppearanceDatabase db)
        {
            UpdateAllRenderers(_currentFrameIndex);
        }

        private void HandleFogOfWarVisibility()
        {
            if (FogOfWarManager.Instance == null || Kingdom.PlayerKingdom == null ||
                _unit.ownerKingdomID.Value == Kingdom.PlayerKingdom.kingdomID.Value)
            {
                if (!_visualRoot.gameObject.activeSelf) _visualRoot.gameObject.SetActive(true);
                return;
            }

            bool isVisible = FogOfWarManager.Instance.IsVisible(transform.position);
            if (_visualRoot.gameObject.activeSelf != isVisible)
                _visualRoot.gameObject.SetActive(isVisible);
        }

        public void UpdateStanceVisual(UnitStance stance)
        {
            if (!stanceIconRenderer) return;

            if (!ShowStanceIcons || (Kingdom.PlayerKingdom && _unit.ownerKingdomID.Value != Kingdom.PlayerKingdom.kingdomID.Value))
            {
                stanceIconRenderer.gameObject.SetActive(false);
                return;
            }

            stanceIconRenderer.gameObject.SetActive(true);
            stanceIconRenderer.transform.localPosition = Vector3.up * 0.9f;
            stanceIconRenderer.color = stanceColor;

            switch (stance)
            {
                case UnitStance.Aggressive: stanceIconRenderer.sprite = aggressiveIcon; break;
                case UnitStance.Defensive: stanceIconRenderer.sprite = defensiveIcon; break;
                case UnitStance.Hold: stanceIconRenderer.sprite = holdIcon; break;
            }
        }

        public void FaceTarget(Vector3 t)
        {
            float dx = t.x - transform.position.x;
            if (Mathf.Abs(dx) > 0.01f)
            {
                float m = spriteFacesLeft ? -1f : 1f;
                bool faceRight = dx > 0;
                Vector3 s = _visualRoot.localScale;
                s.x = faceRight ? (_initialScaleX * m) : (-_initialScaleX * m);
                _visualRoot.localScale = s;
            }
        }

        public void TriggerAttackAnimation() { StopAllCoroutines(); StartCoroutine(AttackLeanRoutine()); }

        private IEnumerator AttackLeanRoutine()
        {
            float d = 0.15f; float rd = 0.2f;
            float cx = _visualRoot.localScale.x; float a = Mathf.Abs(leanAngle);
            float tz = (cx > 0) ? -a : a; if (spriteFacesLeft) tz = -tz;
            float e = 0; Quaternion sr = Quaternion.identity; Quaternion tr = Quaternion.Euler(0, 0, tz);
            while (e < d) { _visualRoot.localRotation = Quaternion.Lerp(sr, tr, e / d); e += Time.deltaTime; yield return null; }
            e = 0; while (e < rd) { _visualRoot.localRotation = Quaternion.Lerp(tr, sr, e / rd); e += Time.deltaTime; yield return null; }
            _visualRoot.localRotation = sr;
        }

        public void TriggerParryEffect()
        {
            if (parryEffectRenderer)
            {
                if (_parryCoroutine != null) StopCoroutine(_parryCoroutine);
                parryEffectRenderer.gameObject.SetActive(true);
                _parryCoroutine = StartCoroutine(HideParryRoutine());
            }
        }

        private IEnumerator HideParryRoutine()
        {
            parryEffectRenderer.color = new Color(1f, 1f, 1f, 1f);
            parryEffectRenderer.transform.localPosition = Vector3.up * 0.5f;
            Vector3 startPos = parryEffectRenderer.transform.localPosition;
            Vector3 endPos = startPos + Vector3.up * 0.5f;
            float duration = 0.5f; float e = 0f;
            while (e < duration) { float t = e / duration; parryEffectRenderer.transform.localPosition = Vector3.Lerp(startPos, endPos, t); Color c = parryEffectRenderer.color; c.a = 1f - t; parryEffectRenderer.color = c; e += Time.deltaTime; yield return null; }
            parryEffectRenderer.gameObject.SetActive(false);
        }

        public void TriggerDrowningEffect()
        {
            if (_drowningCoroutine != null) StopCoroutine(_drowningCoroutine);
            _drowningCoroutine = StartCoroutine(DrowningAnimRoutine());
        }

        private IEnumerator DrowningAnimRoutine()
        {
            if (weaponToolRenderer) weaponToolRenderer.sprite = null;
            int frames = (_bodySet != null && _bodySet.drown != null) ? _bodySet.drown.Length : 5;
            for (int i = 0; i < frames; i++)
            {
                SetDrownFrame(bodyRenderer, _bodySet, i);
                SetDrownFrame(clothesRenderer, _clothesSet, i);
                SetDrownFrame(headRenderer, _headSet, i);
                SetDrownFrame(armorRenderer, _armorSet, i);
                SetDrownFrame(plumeRenderer, _plumeSet, i);
                yield return new WaitForSeconds(frameDuration);
            }
            _visualRoot.gameObject.SetActive(false);
        }

        private void SetDrownFrame(SpriteRenderer r, SpriteSet s, int frameIndex)
        {
            if (!r) return;
            if (s == null || s.drown == null || s.drown.Length == 0) return;
            int i = Mathf.Min(frameIndex, s.drown.Length - 1);
            r.sprite = s.drown[i];
        }

        private void UpdateAllRenderers(int globalFrameIndex)
        {
            bool inWater = _unit.IsInWater;
            int animType = 0;
            if (inWater) animType = 2;
            else if (_isMovingCached) animType = 1;
            else animType = 0;
            if (silhouetteRenderer)
            {
                silhouetteRenderer.sprite = bodyRenderer.sprite;
                // Или weaponRenderer.sprite, если хочешь подсвечивать оружие тоже.
                // Обычно подсвечивают только тело (bodyRenderer).
                silhouetteRenderer.flipX = bodyRenderer.flipX; // Синхрон поворота
            }
            SetSprite(bodyRenderer, _bodySet, globalFrameIndex, animType);
            SetSprite(clothesRenderer, _clothesSet, globalFrameIndex, animType);
            SetSprite(headRenderer, _headSet, globalFrameIndex, animType);
            SetSprite(armorRenderer, _armorSet, globalFrameIndex, animType);

            if (inWater) { if (weaponToolRenderer) weaponToolRenderer.sprite = null; }
            else { SetSprite(weaponToolRenderer, _weaponSet, globalFrameIndex, animType); }

            if (plumeRenderer)
            {
                bool showPlume = (_unit.Profession == ProfessionType.Soldier);
                if (plumeRenderer.gameObject.activeSelf != showPlume) plumeRenderer.gameObject.SetActive(showPlume);
                if (showPlume) SetSprite(plumeRenderer, _plumeSet, globalFrameIndex, animType);
            }
        }

        private void SetSprite(SpriteRenderer r, SpriteSet s, int frameIndex, int animType)
        {
            if (!r) return;
            if (r != clothesRenderer && r != plumeRenderer) r.color = Color.white;
            if (s == null) { r.sprite = null; return; }
            Sprite spriteToSet = null;
            if (animType == 2) // SWIM
            {
                if (s.swim != null && s.swim.Length > 0) { int i = frameIndex % s.swim.Length; spriteToSet = s.swim[i]; }
                else { if (r == bodyRenderer || r == clothesRenderer || r == armorRenderer) spriteToSet = null; else spriteToSet = s.idle; }
            }
            else if (animType == 1) // WALK
            {
                if (s.walk != null && s.walk.Length > 0) { int i = frameIndex % s.walk.Length; spriteToSet = s.walk[i]; }
                else spriteToSet = s.idle;
            }
            else spriteToSet = s.idle; // IDLE
            r.sprite = spriteToSet;
        }
    }
}