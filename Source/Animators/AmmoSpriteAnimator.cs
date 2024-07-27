using AdaptiveArsenal.Utilities;
using System.Collections;

namespace AdaptiveArsenal.Animators;

[RegisterTypeInIl2Cpp(false)]
public class AmmoSpriteAnimator : MonoBehaviour
{
    private GameObject ammoPrefab;
    private readonly List<GameObject> activeAmmoSprites = [];

    internal IEnumerator CasingEjectionAnimation(Vector3 startPosition, Transform parentTransform, GunType gunType)
    {
        var casingObject = Instantiate(ammoPrefab, parentTransform);
        activeAmmoSprites.Add(casingObject);
        casingObject.transform.localPosition = startPosition;
        casingObject.SetActive(true);

        var sprite = casingObject.GetComponent<UISprite>();
        sprite.depth = 30;
        sprite.spriteName = gunType switch
        {
            GunType.Rifle => "ico_ammo_rifle",
            GunType.Revolver => "ico_ammo_revolver",
            _ => ""
        };

        var end = new Vector3(startPosition.x + 100f, startPosition.y - 150f, startPosition.z);
        var controlPoint = new Vector3(
            startPosition.x + 50f,
            startPosition.y + 100f,
            startPosition.z
        );

        var duration = 1f;
        var elapsedTime = 0f;
        var rotationSpeed = 360f;

        while (elapsedTime < duration)
        {
            var t = elapsedTime / duration;
                
            casingObject.transform.localPosition = MathUtils.QuadraticBezier(startPosition, controlPoint, end, t);
            casingObject.transform.Rotate(0, 0, rotationSpeed * Time.deltaTime);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        activeAmmoSprites.Remove(casingObject);
        Destroy(casingObject);
    }

    private void Start() => ammoPrefab = GetComponent<EquipItemPopup>().m_ListAmmoSprites[1].gameObject;

    private void OnDisable()
    {
        foreach (var sprite in activeAmmoSprites)
        {
            Destroy(sprite);
        }
        activeAmmoSprites.Clear();
    }
}