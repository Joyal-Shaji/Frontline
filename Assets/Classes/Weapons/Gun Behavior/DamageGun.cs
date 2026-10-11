using UnityEngine;

public class DamageGun : MonoBehaviour
{
    public float Damage;
    public float BulletRange;
    [SerializeField] private Camera playerCamera;

    private void Awake()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }

        if (playerCamera == null)
        {
            Debug.LogError("DamageGun needs an assigned player camera or an enabled camera tagged MainCamera.", this);
        }
    }

    public void Shoot()
    {
        if (playerCamera == null)
        {
            return;
        }

        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        if (Physics.Raycast(ray, out RaycastHit hit, BulletRange))
        {
            // Enemy colliders are often on child objects rather than the Enemy root.
            Enemy enemy = hit.collider.GetComponentInParent<Enemy>();
            if (enemy != null)
            {
                enemy.Health -= Damage;
            }
        }
    }
}
