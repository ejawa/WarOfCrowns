using System.Collections.Generic;
using UnityEngine;

namespace WarOfCrowns.Core
{
    // Если Gender уже определен в другом месте (например, в Unit.cs), удали эту строку,
    // но лучше перенеси Gender в отдельный файл Enums.cs или оставь здесь, если он нигде больше не дублируется.
    public enum Gender { Male, Female }

    [CreateAssetMenu(fileName = "NameDatabase", menuName = "WarOfCrowns/Name Database")]
    public class NameDatabase : ScriptableObject
    {
        [Header("Имена")]
        [TextArea(5, 10)] public string maleNamesRaw;
        [TextArea(5, 10)] public string femaleNamesRaw;

        [Header("Фамилии")]
        [TextArea(5, 10)] public string surnamesRaw;

        // --- ВОТ ТЕ САМЫЕ СПИСКИ, КОТОРЫХ НЕ ХВАТАЛО ---
        [Header("Портреты")]
        public List<Sprite> malePortraits;
        public List<Sprite> femalePortraits;
        // -----------------------------------------------

        // Кэшированные списки (не сохраняются в ассет)
        private List<string> _maleNames;
        private List<string> _femaleNames;
        private List<string> _surnames;

        private void Initialize()
        {
            _maleNames = ParseList(maleNamesRaw);
            _femaleNames = ParseList(femaleNamesRaw);
            _surnames = ParseList(surnamesRaw);
        }

        private List<string> ParseList(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return new List<string>();
            return new List<string>(raw.Split(new char[] { '\n', ',', '\r' }, System.StringSplitOptions.RemoveEmptyEntries));
        }

        public string GetRandomName(Gender gender)
        {
            if (_maleNames == null || _maleNames.Count == 0) Initialize();

            string firstName = "Unknown";
            string lastName = "";

            if (gender == Gender.Male && _maleNames.Count > 0)
                firstName = _maleNames[Random.Range(0, _maleNames.Count)].Trim();
            else if (gender == Gender.Female && _femaleNames.Count > 0)
                firstName = _femaleNames[Random.Range(0, _femaleNames.Count)].Trim();

            if (_surnames != null && _surnames.Count > 0)
                lastName = _surnames[Random.Range(0, _surnames.Count)].Trim();

            return $"{firstName} {lastName}";
        }

        public Sprite GetRandomPortrait(Gender gender)
        {
            if (gender == Gender.Male && malePortraits != null && malePortraits.Count > 0)
            {
                return malePortraits[Random.Range(0, malePortraits.Count)];
            }
            else if (gender == Gender.Female && femalePortraits != null && femalePortraits.Count > 0)
            {
                return femalePortraits[Random.Range(0, femalePortraits.Count)];
            }
            return null;
        }
    }
}