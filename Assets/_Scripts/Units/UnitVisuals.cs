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
        [SerializeField] private float leanAngle = 25f;

        // Свойства для UI
        public Sprite BodySprite => bodyRenderer != null ? bodyRenderer.sprite : null;
        public Sprite ClothesSprite => clothesRenderer != null ? clothesRenderer.sprite : null;
        public Sprite HeadSprite => headRenderer != null ? headRenderer.sprite : null;
        public Sprite ArmorSprite => armorRenderer != null ? armorRenderer.sprite : null;
        public Sprite WeaponSprite => weaponToolRenderer != null ? weaponToolRenderer.sprite : null;

        // --- СВОЙСТВА ДЛЯ СОХРАНЕНИЯ (Имена спрайтов) ---
        public string BodySpriteName => _bodySet?.idle != null ? _bodySet.idle.name : "";
        public string ClothesSpriteName => _clothesSet?.idle != null ? _clothesSet.idle.name : "";
        public string HeadSpriteName => _headSet?.idle != null ? _headSet.idle.name : "";

        // Текущие сеты
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

        private void Awake()
        {
            _motor = GetComponentInParent<UnitMotor>();
            _visualRoot = transform.Find("Visuals");
            if (_visualRoot == null) _visualRoot = transform;
            _initialScaleX = Mathf.Abs(_visualRoot.localScale.x);
        }

        private void Start() { _lastPosition = transform.position; }

        private void LateUpdate()
        {
            if (_motor == null) return;

            // Поворот
            float deltaX = transform.position.x - _lastPosition.x;
            _lastPosition = transform.position;
            if (Mathf.Abs(deltaX) > 0.001f) Flip(deltaX > 0);

            // Анимация ходьбы
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

        // --- МЕТОДЫ ЗАГРУЗКИ И ГЕНЕРАЦИИ ---

        public void InitAppearance(Gender gender, AppearanceDatabase db)
        {
            if (db == null) return;
            _bodySet = db.GetRandomBody();
            _headSet = db.GetRandomHead(gender);
            _clothesSet = db.GetRandomPeasantClothes();
            _armorSet = null; _weaponSet = null;
            UpdateAllRenderers(-1);
        }

        // Вызывается при загрузке игры
        public void LoadAppearance(SpriteSet body, SpriteSet head, SpriteSet clothes)
        {
            _bodySet = body;
            _headSet = head;
            _clothesSet = clothes;
            UpdateAllRenderers(-1);
        }

        public void UpdateEquipment(ResourceType tool, ResourceType weapon, ResourceType armor, AppearanceDatabase db)
        {
            if (db == null) return;
            SpriteSet wS = db.GetEquipmentSprites(weapon);
            SpriteSet tS = db.GetEquipmentSprites(tool);
            _weaponSet = (wS != null) ? wS : tS; // Оружие приоритетнее инструмента
            _armorSet = db.GetEquipmentSprites(armor);
            UpdateAllRenderers(_isMoving ? _currentFrame : -1);
        }

        public void UpdateProfession(ProfessionType profession, AppearanceDatabase db)
        {
            if (db == null) return;
            if (profession == ProfessionType.Soldier) _clothesSet = db.GetRandomSoldierClothes();
            else _clothesSet = db.GetRandomPeasantClothes(); // Или оставить текущую
            UpdateAllRenderers(_isMoving ? _currentFrame : -1);
        }

        // --- ВСПОМОГАТЕЛЬНЫЕ ---
        public void FaceTarget(Vector3 targetPosition)
        {
            float diffX = targetPosition.x - transform.position.x;
            if (Mathf.Abs(diffX) > 0.1f) Flip(diffX > 0);
        }

        private void Flip(bool faceRight)
        {
            Vector3 scale = _visualRoot.localScale;
            scale.x = faceRight ? -_initialScaleX : _initialScaleX; // Инверсия для левосторонних спрайтов
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

            // --- ИСПРАВЛЕНИЕ ---
            // Проверяем текущий масштаб контейнера визуалов.
            // Если Scale.x положительный (смотрим влево) -> угол +25
            // Если Scale.x отрицательный (смотрим вправо) -> угол -25

            float currentScaleX = _visualRoot.localScale.x;
            float angle = Mathf.Abs(leanAngle);
            float targetZ = (currentScaleX > 0) ? angle : -angle;
            // -------------------

            float elapsed = 0f;
            Quaternion startRot = Quaternion.identity;
            Quaternion targetRot = Quaternion.Euler(0, 0, targetZ);

            while (elapsed < duration)
            {
                _visualRoot.localRotation = Quaternion.Lerp(startRot, targetRot, elapsed / duration);
                elapsed += Time.deltaTime;
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < returnDuration)
            {
                _visualRoot.localRotation = Quaternion.Lerp(targetRot, startRot, elapsed / returnDuration);
                elapsed += Time.deltaTime;
                yield return null;
            }
            _visualRoot.localRotation = startRot;
        }

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
    }
}