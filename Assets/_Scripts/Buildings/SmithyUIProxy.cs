using UnityEngine;
using WarOfCrowns.Buildings;
namespace WarOfCrowns.UI
{
    public class SmithyUIProxy : MonoBehaviour
    {
        public void Initialize(Smithy smithy)
        {
            GetComponent<SmithyUI>().Initialize(smithy);
        }
    }
}