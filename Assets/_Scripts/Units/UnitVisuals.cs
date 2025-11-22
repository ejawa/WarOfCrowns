using UnityEngine;
using System.Collections;
using WarOfCrowns.Core;

namespace WarOfCrowns.Units
{
    public class UnitVisuals : MonoBehaviour
    {
        [Header("Рендереры")]
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private SpriteRenderer clothesRenderer;
        [SerializeField] private SpriteRenderer headRenderer;
        [SerializeField] private SpriteRenderer armorRenderer;
        [SerializeField] private SpriteRenderer weaponToolRenderer;

        [Header("Анимация")]
        [SerializeField] private float frameRate = 0.1f;
        [SerializeField] private float leanAngle = 25f; // Ставь положительное число (25)

        // Свойства
        public Sprite BodySprite => bodyRenderer != null ? bodyRenderer.sprite : null;
        public Sprite ClothesSprite => clothesRenderer != null ? clothesRenderer.sprite : null;
        public Sprite HeadSprite => headRenderer != null ? headRenderer.sprite : null;
        public Sprite ArmorSprite => armorRenderer != null ? armorRenderer.sprite : null;
        public Sprite WeaponSprite => weaponToolRenderer != null ? weaponToolRenderer.sprite : null;

        private SpriteSet _bodySet;
        private SpriteSet _clothesSet;
        private SpriteSet _headSet;
        private SpriteSet _armorSet;
        private SpriteSet _weaponSet;

        private UnitMotor _motor;
        private float _timer;
        private int _currentFrame;
        private bool _isMoving;

        private Vector3 _lastPosition;
        private float _initialScaleX;
        private Transform _visualRoot;

        // Важный флаг: куда мы смотрим? (True = Вправо, False = Влево/Оригинал)
        private bool _isFacingRight = false;

        private void Awake()
        {
            _motor = GetComponentInParent<UnitMotor>();
            _visualRoot = transform.Find("Visuals");
            if (_visualRoot == null) _visualRoot = transform;

            _initialScaleX = Mathf.Abs(_visualRoot.localScale.x);
        }

        private void Start()
        {
            _lastPosition = transform.position;
        }

        private void LateUpdate()
        {
            if (_motor == null) return;

            // 1. ПОВОРОТ
            float deltaX = transform.position.x - _lastPosition.x;
            _lastPosition = transform.position;

            if (Mathf.Abs(deltaX) > 0.001f)
            {
                bool movingRight = deltaX > 0;
                Flip(movingRight);
            }

            // 2. АНИМАЦИЯ ХОДЬБЫ
            _isMoving = _motor.IsMoving;

            if (_isMoving)
            {
                _timer += Time.deltaTime;
                if (_timer >= frameRate)
                {
                    _timer = 0f;
                    _currentFrame++;
                    if (_currentFrame > 2) _currentFrame = 0;
                    UpdateAllRenderers(_currentFrame);
                }
            }
            else
            {
                if (_currentFrame != -1)
                {
                    _currentFrame = -1;
                    UpdateAllRenderers(-1);
                }
            }
        }

        public void FaceTarget(Vector3 targetPosition)
        {
            float diffX = targetPosition.x - transform.position.x;
            if (Mathf.Abs(diffX) > 0.1f)
            {
                Flip(diffX > 0);
            }
        }

        private void Flip(bool faceRight)
        {
            _isFacingRight = faceRight;

            Vector3 scale = _visualRoot.localScale;
            // Для спрайтов, смотрящих влево:
            // Вправо (faceRight) -> -Scale
            // Влево (!faceRight) -> +Scale
            scale.x = faceRight ? -_initialScaleX : _initialScaleX;

            _visualRoot.localScale = scale;
        }

        public void TriggerAttackAnimation()
        {
            StopAllCoroutines();
            StartCoroutine(AttackLeanRoutine());
        }

        private IEnumerator AttackLeanRoutine()
        {
            float duration = 0.15f;
            float returnDuration = 0.2f;

            // --- ГЛАВНОЕ ИСПРАВЛЕНИЕ ---
            // Мы вручную вычисляем угол, чтобы он всегда был "вперед" (вниз).
            // Если смотрим влево -> крутим +25
            // Если смотрим вправо -> крутим +25 (НО! из-за Scale -1 это превратится в поворот назад)
            // ПОЭТОМУ:
            // Если смотрим вправо (Scale -1), нам нужно крутить в ОБРАТНУЮ сторону (-25), 
            // чтобы Scale -1 * Rot -25 дало визуальный наклон вперед.

            float angle = Mathf.Abs(leanAngle);
            float targetZ = _isFacingRight ? -angle : angle;
            // ----------------------------

            float elapsed = 0f;
            Quaternion startRot = Quaternion.identity;
            Quaternion targetRot = Quaternion.Euler(0, 0, targetZ);

            // Наклон
            while (elapsed < duration)
            {
                _visualRoot.localRotation = Quaternion.Lerp(startRot, targetRot, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            elapsed = 0f;
            // Возврат
            while (elapsed < returnDuration)
            {
                _visualRoot.localRotation = Quaternion.Lerp(targetRot, startRot, elapsed / returnDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            _visualRoot.localRotation = startRot;
        }

        // ... (Остальные методы: UpdateAllRenderers, SetSprite, InitAppearance... скопируй из прошлого кода)
        private void UpdateAllRenderers(int frameIndex)
        {
            SetSprite(bodyRenderer, _bodySet, frameIndex);
            SetSprite(clothesRenderer, _clothesSet, frameIndex);
            SetSprite(headRenderer, _headSet, frameIndex);
            SetSprite(armorRenderer, _armorSet, frameIndex);
            SetSprite(weaponToolRenderer, _weaponSet, frameIndex);
        }
        private void SetSprite(SpriteRenderer r, SpriteSet s, int f)
        {
            if (r == null) return;
            if (s == null) { r.sprite = null; return; }
            r.sprite = (f == -1 || s.walk == null || s.walk.Length <= f) ? s.idle : s.walk[f];
        }
        // --- ИНИЦИАЛИЗАЦИЯ ---
        public void InitAppearance(Gender gender, AppearanceDatabase db)
        {
            if (db == null) return;

            _bodySet = db.GetRandomBody();
            _headSet = db.GetRandomHead(gender);

            
            _clothesSet = db.GetRandomPeasantClothes();
            // ----------------------------------------------------

            _armorSet = null;
            _weaponSet = null;

            UpdateAllRenderers(-1);
        }

        public void UpdateProfession(ProfessionType profession, AppearanceDatabase db)
        {
            if (db == null) return;
            if (profession == ProfessionType.Soldier)
            {
                _clothesSet = db.GetRandomSoldierClothes();
            }
            else
            {
              
                _clothesSet = db.GetRandomPeasantClothes();
            }
            // ----------------------------------------------

            UpdateAllRenderers(_isMoving ? _currentFrame : -1);
        }
        public void UpdateEquipment(ResourceType t, ResourceType w, ResourceType a, AppearanceDatabase db)
        {
            if (db == null) return;
            SpriteSet wS = db.GetEquipmentSprites(w); SpriteSet tS = db.GetEquipmentSprites(t);
            _weaponSet = (wS != null) ? wS : tS;
            _armorSet = db.GetEquipmentSprites(a);
            UpdateAllRenderers(_isMoving ? _currentFrame : -1);
        }
        
    }
}